using LoperFamilyTreeBuilder.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace LoperFamilyTreeBuilder.Data;

public sealed class FamilyTreeDbContext : DbContext
{
    public FamilyTreeDbContext(DbContextOptions<FamilyTreeDbContext> options)
        : base(options)
    {
    }

    public DbSet<Person> People => Set<Person>();
    public DbSet<PersonIdentifier> PersonIdentifiers => Set<PersonIdentifier>();
    public DbSet<FamilyBranch> FamilyBranches => Set<FamilyBranch>();
    public DbSet<BranchMembership> BranchMemberships => Set<BranchMembership>();
    public DbSet<ParentChildRelationship> ParentChildRelationships => Set<ParentChildRelationship>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<MedicalCondition> MedicalConditions => Set<MedicalCondition>();
    public DbSet<HandwritingTranscription> HandwritingTranscriptions => Set<HandwritingTranscription>();
    public DbSet<DnaMatch> DnaMatches => Set<DnaMatch>();
    public DbSet<DnaSharedMatch> DnaSharedMatches => Set<DnaSharedMatch>();
    public DbSet<FamilyUnion> FamilyUnions => Set<FamilyUnion>();
    public DbSet<LifeEvent> LifeEvents => Set<LifeEvent>();
    public DbSet<ArchiveItem> ArchiveItems => Set<ArchiveItem>();
    public DbSet<SourceRecord> SourceRecords => Set<SourceRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FamilyTreeDbContext).Assembly);
    }
}
