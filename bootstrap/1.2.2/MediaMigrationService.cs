using System.Security.Cryptography;
using System.Text.RegularExpressions;
using LoperFamilyTreeBuilder.Core.Entities;
using LoperFamilyTreeBuilder.Core.Models;
using LoperFamilyTreeBuilder.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace LoperFamilyTreeBuilder.Data.Services;

public sealed class MediaMigrationService(IDbContextFactory<FamilyTreeDbContext> contextFactory, ArchiveLibraryService archiveLibrary)
{
    private static readonly Dictionary<string,(ArchiveMediaType Type,string Mime)> Supported = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"]=(ArchiveMediaType.Photo,"image/jpeg"),[".jpeg"]=(ArchiveMediaType.Photo,"image/jpeg"),[".png"]=(ArchiveMediaType.Photo,"image/png"),[".gif"]=(ArchiveMediaType.Photo,"image/gif"),[".webp"]=(ArchiveMediaType.Photo,"image/webp"),[".tif"]=(ArchiveMediaType.Photo,"image/tiff"),[".tiff"]=(ArchiveMediaType.Photo,"image/tiff"),[".heic"]=(ArchiveMediaType.Photo,"image/heic"),
        [".pdf"]=(ArchiveMediaType.Document,"application/pdf"),[".doc"]=(ArchiveMediaType.Document,"application/msword"),[".docx"]=(ArchiveMediaType.Document,"application/vnd.openxmlformats-officedocument.wordprocessingml.document"),[".txt"]=(ArchiveMediaType.Document,"text/plain"),[".rtf"]=(ArchiveMediaType.Document,"application/rtf"),
        [".mp3"]=(ArchiveMediaType.Audio,"audio/mpeg"),[".m4a"]=(ArchiveMediaType.Audio,"audio/mp4"),[".wav"]=(ArchiveMediaType.Audio,"audio/wav"),[".mp4"]=(ArchiveMediaType.Video,"video/mp4"),[".mov"]=(ArchiveMediaType.Video,"video/quicktime")
    };

    public async Task<MediaMigrationAnalysisResult> AnalyzeFolderAsync(MediaMigrationSourceType sourceType,string sourceRootPath,string actor,CancellationToken cancellationToken=default)
    {
        if(string.IsNullOrWhiteSpace(sourceRootPath)) throw new InvalidOperationException("Choose or enter the Ancestry / Family Tree Maker media folder first.");
        var root=Path.GetFullPath(Environment.ExpandEnvironmentVariables(sourceRootPath.Trim().Trim('"')));
        if(!Directory.Exists(root)) throw new DirectoryNotFoundException($"The media folder was not found: {root}");

        await using var db=await contextFactory.CreateDbContextAsync(cancellationToken);
        var session=new MediaMigrationSession(sourceType,root,actor); db.MediaMigrationSessions.Add(session); await db.SaveChangesAsync(cancellationToken);
        try
        {
            var existingRows=await db.ArchiveMediaFiles.AsNoTracking().Select(x=>new{x.Sha256,x.Id}).ToListAsync(cancellationToken);
            var existingHashes=existingRows.GroupBy(x=>x.Sha256,StringComparer.OrdinalIgnoreCase).ToDictionary(x=>x.Key,x=>x.First().Id,StringComparer.OrdinalIgnoreCase);
            var people=await db.People.AsNoTracking().Select(x=>new PersonCandidate(x.Id,(x.GivenName+" "+x.MiddleName+" "+x.Surname+" "+x.Suffix).Trim(),x.GivenName,x.Surname,x.Identifiers.Where(i=>i.IdentifierType==PersonIdentifierType.LegacyNumber).Select(i=>i.Value).FirstOrDefault())).ToListAsync(cancellationToken);
            var files=Directory.EnumerateFiles(root,"*",SearchOption.AllDirectories).ToList();
            var ready=0;var duplicates=0;var review=0;
            foreach(var fullPath in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var relative=Path.GetRelativePath(root,fullPath).Replace('\\','/');var fileName=Path.GetFileName(fullPath);var extension=Path.GetExtension(fileName);
                if(!Supported.TryGetValue(extension,out var fileInfo))
                {
                    var unsupported=new MediaMigrationItem(session.Id,relative,fileName,new FileInfo(fullPath).Length,string.Empty,ArchiveMediaType.Other,"application/octet-stream",string.Empty,MediaMigrationItemStatus.Unsupported);
                    unsupported.RequireReview("Unsupported file type. The source file was not changed.");db.MediaMigrationItems.Add(unsupported);review++;continue;
                }
                try
                {
                    await using var stream=new FileStream(fullPath,FileMode.Open,FileAccess.Read,FileShare.Read,81920,useAsync:true);
                    var hashBytes=await SHA256.HashDataAsync(stream,cancellationToken);var sha=Convert.ToHexString(hashBytes).ToLowerInvariant();
                    var metadataJson=await ReadMetadataAsync(fullPath,fileInfo.Mime,cancellationToken);
                    var item=new MediaMigrationItem(session.Id,relative,fileName,stream.Length,sha,fileInfo.Type,fileInfo.Mime,metadataJson,MediaMigrationItemStatus.ReadyToImport);
                    if(existingHashes.TryGetValue(sha,out var existingId)){item.MarkDuplicate(existingId);duplicates++;}
                    else
                    {
                        var match=FindPersonMatch(fileName,people);
                        if(match.PersonId.HasValue)
                        {
                            item.SuggestPerson(match.PersonId.Value,match.Reason);
                            if(!match.IsLegacyNumberMatch){item.RequireReview("Possible filename-to-person match requires confirmation. Original media is preserved and no genealogy is changed.");review++;}
                            else ready++;
                        }
                        else ready++;
                    }
                    db.MediaMigrationItems.Add(item);
                }
                catch(Exception ex)
                {
                    var failed=new MediaMigrationItem(session.Id,relative,fileName,new FileInfo(fullPath).Length,string.Empty,fileInfo.Type,fileInfo.Mime,string.Empty,MediaMigrationItemStatus.NeedsReview);
                    failed.RequireReview($"Could not analyze this file: {ex.Message}");db.MediaMigrationItems.Add(failed);review++;
                }
            }
            session.CompleteAnalysis(files.Count,ready,duplicates,review);
            db.AuditEvents.Add(new AuditEvent("Analyze family media migration",nameof(MediaMigrationSession),session.Id.ToString(),actor,$"Analyzed {files.Count} files from a {sourceType} media folder. Source files were read only and were not changed.",source:"Media Migration"));
            await db.SaveChangesAsync(cancellationToken);
            return await GetAnalysisAsync(session.Id,cancellationToken) ?? throw new InvalidOperationException("The media migration analysis could not be reloaded.");
        }
        catch{session.MarkFailed();await db.SaveChangesAsync(cancellationToken);throw;}
    }

    public async Task<MediaMigrationAnalysisResult?> GetAnalysisAsync(Guid sessionId,CancellationToken cancellationToken=default)
    {
        await using var db=await contextFactory.CreateDbContextAsync(cancellationToken);
        var session=await db.MediaMigrationSessions.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==sessionId,cancellationToken);if(session is null)return null;
        var items=await db.MediaMigrationItems.AsNoTracking().Where(x=>x.SessionId==sessionId).OrderBy(x=>x.Status).ThenBy(x=>x.OriginalFileName).Take(1000)
            .Select(x=>new MediaMigrationItemView(x.Id,x.SourceRelativePath,x.OriginalFileName,x.FileSizeBytes,x.MediaType,x.Sha256,x.Status,x.ExistingMediaFileId,x.SuggestedPersonId,
                x.SuggestedPersonId.HasValue?db.People.Where(p=>p.Id==x.SuggestedPersonId.Value).Select(p=>(p.GivenName+" "+p.MiddleName+" "+p.Surname+" "+p.Suffix).Trim()).FirstOrDefault()??string.Empty:string.Empty,
                x.SuggestedPersonId.HasValue?db.PersonIdentifiers.Where(i=>i.PersonId==x.SuggestedPersonId.Value&&i.IdentifierType==PersonIdentifierType.LegacyNumber).Select(i=>i.Value).FirstOrDefault()??string.Empty:string.Empty,
                x.SuggestedMatchReason,x.ImportedMediaFileId,x.ReviewNote)).ToListAsync(cancellationToken);
        return new MediaMigrationAnalysisResult(ToSummary(session),items);
    }

    public async Task<IReadOnlyList<MediaMigrationSessionSummary>> GetSessionsAsync(CancellationToken cancellationToken=default)
    {
        await using var db=await contextFactory.CreateDbContextAsync(cancellationToken);
        var sessions=await db.MediaMigrationSessions.AsNoTracking().OrderByDescending(x=>x.CreatedUtc).Take(30).ToListAsync(cancellationToken);return sessions.Select(ToSummary).ToList();
    }

    public async Task<MediaMigrationAnalysisResult> ImportSafeOriginalsAsync(Guid sessionId,string actor,CancellationToken cancellationToken=default)
    {
        await using var db=await contextFactory.CreateDbContextAsync(cancellationToken);
        var session=await db.MediaMigrationSessions.SingleOrDefaultAsync(x=>x.Id==sessionId,cancellationToken)??throw new InvalidOperationException("Media migration session not found.");session.BeginImport();await db.SaveChangesAsync(cancellationToken);
        var items=await db.MediaMigrationItems.Where(x=>x.SessionId==sessionId&&x.Status==MediaMigrationItemStatus.ReadyToImport).OrderBy(x=>x.SourceRelativePath).ToListAsync(cancellationToken);
        var imported=0;var failed=0;
        foreach(var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fullPath=Path.GetFullPath(Path.Combine(session.SourceRootPath,item.SourceRelativePath.Replace('/',Path.DirectorySeparatorChar)));var root=Path.GetFullPath(session.SourceRootPath).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
            if(!fullPath.StartsWith(root,StringComparison.OrdinalIgnoreCase)||!File.Exists(fullPath)){item.MarkFailed("Source file is missing or no longer inside the analyzed migration folder.");failed++;continue;}
            try
            {
                await using var stream=new FileStream(fullPath,FileMode.Open,FileAccess.Read,FileShare.Read,81920,useAsync:true);
                var result=await archiveLibrary.ImportAsync(new ArchiveImportRequest{MediaType=item.MediaType,OriginalFileName=item.OriginalFileName,MimeType=item.MimeType,Title=Path.GetFileNameWithoutExtension(item.OriginalFileName),Description=$"Migrated from {session.SourceType}: {item.SourceRelativePath}",CapturedMetadataJson=item.CapturedMetadataJson,PersonId=item.SuggestedMatchReason.StartsWith("Legacy Number",StringComparison.OrdinalIgnoreCase)?item.SuggestedPersonId:null,Role=item.MediaType==ArchiveMediaType.Photo?PersonMediaRole.Photo:item.MediaType==ArchiveMediaType.Document?PersonMediaRole.Document:PersonMediaRole.Other},stream,actor,cancellationToken);
                item.MarkImported(result.MediaFileId);imported++;
            }
            catch(Exception ex){item.MarkFailed(ex.Message);failed++;}
        }
        var remainingReview=await db.MediaMigrationItems.CountAsync(x=>x.SessionId==sessionId&&(x.Status==MediaMigrationItemStatus.NeedsReview||x.Status==MediaMigrationItemStatus.Unsupported||x.Status==MediaMigrationItemStatus.Failed),cancellationToken);
        session.CompleteImport(imported,failed,remainingReview);
        db.AuditEvents.Add(new AuditEvent("Import family media migration",nameof(MediaMigrationSession),session.Id.ToString(),actor,$"Imported {imported} safe originals from migration session {session.SessionCode}; {remainingReview} item(s) remain for review.",source:"Media Migration"));
        await db.SaveChangesAsync(cancellationToken);
        return await GetAnalysisAsync(sessionId,cancellationToken)??throw new InvalidOperationException("The migration result could not be reloaded.");
    }

    private static async Task<string> ReadMetadataAsync(string fullPath,string mimeType,CancellationToken cancellationToken)
    {
        const int max=4*1024*1024;await using var input=new FileStream(fullPath,FileMode.Open,FileAccess.Read,FileShare.Read,81920,useAsync:true);var length=(int)Math.Min(input.Length,max);var bytes=new byte[length];var total=0;
        while(total<length){var read=await input.ReadAsync(bytes.AsMemory(total,length-total),cancellationToken);if(read==0)break;total+=read;}
        return SubmissionMetadataExtractor.Extract(Path.GetFileName(fullPath),mimeType,bytes.AsSpan(0,total));
    }

    private static PersonMatch FindPersonMatch(string fileName,IReadOnlyList<PersonCandidate> people)
    {
        var stem=Path.GetFileNameWithoutExtension(fileName);var compactStem=NormalizeCompact(stem);
        foreach(var person in people.Where(x=>!string.IsNullOrWhiteSpace(x.LegacyNumber))){var legacy=person.LegacyNumber!;if(stem.Contains(legacy,StringComparison.OrdinalIgnoreCase)||compactStem.Contains(NormalizeCompact(legacy),StringComparison.OrdinalIgnoreCase))return new PersonMatch(person.Id,$"Legacy Number {legacy} appears in filename",true);}
        var normalized=" "+NormalizeWords(stem)+" ";var matches=people.Where(p=>!string.IsNullOrWhiteSpace(p.GivenName)&&!string.IsNullOrWhiteSpace(p.Surname)&&normalized.Contains(" "+NormalizeWords(p.GivenName)+" ",StringComparison.OrdinalIgnoreCase)&&normalized.Contains(" "+NormalizeWords(p.Surname)+" ",StringComparison.OrdinalIgnoreCase)).Take(2).ToList();
        return matches.Count==1?new PersonMatch(matches[0].Id,$"Unique name match in filename: {matches[0].DisplayName}",false):new PersonMatch(null,string.Empty,false);
    }
    private static string NormalizeWords(string value)=>Regex.Replace(value.ToLowerInvariant(),"[^a-z0-9]+"," ").Trim();
    private static string NormalizeCompact(string value)=>Regex.Replace(value.ToLowerInvariant(),"[^a-z0-9]+",string.Empty);
    private static MediaMigrationSessionSummary ToSummary(MediaMigrationSession x)=>new(x.Id,x.SessionCode,x.SourceType,x.SourceRootPath,x.Status,x.FilesScanned,x.ReadyToImportCount,x.ExactDuplicateCount,x.NeedsReviewCount,x.ImportedCount,x.FailedCount,x.CreatedUtc,x.CompletedUtc);
    private sealed record PersonCandidate(Guid Id,string DisplayName,string GivenName,string Surname,string? LegacyNumber);
    private sealed record PersonMatch(Guid? PersonId,string Reason,bool IsLegacyNumberMatch);
}
