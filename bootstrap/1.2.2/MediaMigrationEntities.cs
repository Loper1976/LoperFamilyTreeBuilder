namespace LoperFamilyTreeBuilder.Core.Entities;

public enum MediaMigrationSourceType { Ancestry = 1, FamilyTreeMaker = 2, Folder = 3 }
public enum MediaMigrationStatus { Analyzing = 1, ReadyForReview = 2, Importing = 3, Completed = 4, CompletedWithReviewItems = 5, Failed = 6 }
public enum MediaMigrationItemStatus { ReadyToImport = 1, ExactDuplicate = 2, NeedsReview = 3, Imported = 4, Failed = 5, Unsupported = 6 }

public sealed class MediaMigrationSession
{
    private MediaMigrationSession() { }
    public MediaMigrationSession(MediaMigrationSourceType sourceType, string sourceRootPath, string actor)
    {
        if (string.IsNullOrWhiteSpace(sourceRootPath)) throw new ArgumentException("A source folder is required.", nameof(sourceRootPath));
        Id = Guid.NewGuid();
        SessionCode = $"MEDIA-{DateTime.UtcNow:yyyyMMdd-HHmm}-{Id.ToString("N")[..6].ToUpperInvariant()}";
        SourceType = sourceType; SourceRootPath = sourceRootPath.Trim(); StartedBy = actor ?? string.Empty;
        Status = MediaMigrationStatus.Analyzing; CreatedUtc = DateTimeOffset.UtcNow; ModifiedUtc = CreatedUtc;
    }
    public Guid Id { get; private set; }
    public string SessionCode { get; private set; } = string.Empty;
    public MediaMigrationSourceType SourceType { get; private set; }
    public string SourceRootPath { get; private set; } = string.Empty;
    public string StartedBy { get; private set; } = string.Empty;
    public MediaMigrationStatus Status { get; private set; }
    public int FilesScanned { get; private set; }
    public int ReadyToImportCount { get; private set; }
    public int ExactDuplicateCount { get; private set; }
    public int NeedsReviewCount { get; private set; }
    public int ImportedCount { get; private set; }
    public int FailedCount { get; private set; }
    public DateTimeOffset CreatedUtc { get; private set; }
    public DateTimeOffset ModifiedUtc { get; private set; }
    public DateTimeOffset? CompletedUtc { get; private set; }
    public void CompleteAnalysis(int filesScanned,int ready,int duplicates,int needsReview){FilesScanned=filesScanned;ReadyToImportCount=ready;ExactDuplicateCount=duplicates;NeedsReviewCount=needsReview;Status=MediaMigrationStatus.ReadyForReview;ModifiedUtc=DateTimeOffset.UtcNow;}
    public void BeginImport(){Status=MediaMigrationStatus.Importing;ModifiedUtc=DateTimeOffset.UtcNow;}
    public void CompleteImport(int imported,int failed,int remainingReview){ImportedCount=imported;FailedCount=failed;NeedsReviewCount=remainingReview;Status=failed>0||remainingReview>0?MediaMigrationStatus.CompletedWithReviewItems:MediaMigrationStatus.Completed;CompletedUtc=DateTimeOffset.UtcNow;ModifiedUtc=CompletedUtc.Value;}
    public void MarkFailed(){Status=MediaMigrationStatus.Failed;ModifiedUtc=DateTimeOffset.UtcNow;}
}

public sealed class MediaMigrationItem
{
    private MediaMigrationItem() { }
    public MediaMigrationItem(Guid sessionId,string sourceRelativePath,string originalFileName,long fileSizeBytes,string sha256,ArchiveMediaType mediaType,string mimeType,string capturedMetadataJson,MediaMigrationItemStatus status)
    {
        Id=Guid.NewGuid();SessionId=sessionId;SourceRelativePath=sourceRelativePath??string.Empty;OriginalFileName=originalFileName??string.Empty;FileSizeBytes=fileSizeBytes;Sha256=sha256??string.Empty;MediaType=mediaType;MimeType=mimeType??"application/octet-stream";CapturedMetadataJson=capturedMetadataJson??string.Empty;Status=status;CreatedUtc=DateTimeOffset.UtcNow;ModifiedUtc=CreatedUtc;
    }
    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }
    public string SourceRelativePath { get; private set; } = string.Empty;
    public string OriginalFileName { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; }
    public string Sha256 { get; private set; } = string.Empty;
    public ArchiveMediaType MediaType { get; private set; }
    public string MimeType { get; private set; } = string.Empty;
    public string CapturedMetadataJson { get; private set; } = string.Empty;
    public MediaMigrationItemStatus Status { get; private set; }
    public Guid? ExistingMediaFileId { get; private set; }
    public Guid? SuggestedPersonId { get; private set; }
    public string SuggestedMatchReason { get; private set; } = string.Empty;
    public Guid? ImportedMediaFileId { get; private set; }
    public string ReviewNote { get; private set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; private set; }
    public DateTimeOffset ModifiedUtc { get; private set; }
    public MediaMigrationSession Session { get; private set; } = null!;
    public void MarkDuplicate(Guid id){ExistingMediaFileId=id;Status=MediaMigrationItemStatus.ExactDuplicate;ModifiedUtc=DateTimeOffset.UtcNow;}
    public void SuggestPerson(Guid personId,string reason){SuggestedPersonId=personId;SuggestedMatchReason=reason??string.Empty;ModifiedUtc=DateTimeOffset.UtcNow;}
    public void RequireReview(string note){ReviewNote=note??string.Empty;Status=MediaMigrationItemStatus.NeedsReview;ModifiedUtc=DateTimeOffset.UtcNow;}
    public void MarkImported(Guid id){ImportedMediaFileId=id;Status=MediaMigrationItemStatus.Imported;ReviewNote=string.Empty;ModifiedUtc=DateTimeOffset.UtcNow;}
    public void MarkFailed(string note){ReviewNote=note??string.Empty;Status=MediaMigrationItemStatus.Failed;ModifiedUtc=DateTimeOffset.UtcNow;}
}
