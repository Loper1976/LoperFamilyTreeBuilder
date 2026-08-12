using LoperFamilyTreeBuilder.Core.Entities;
using LoperFamilyTreeBuilder.Core.Models;
using LoperFamilyTreeBuilder.Core.Policies;

namespace LoperFamilyTreeBuilder.Tests;

public sealed class HandwritingTranscriptionWorkflowTests
{
    [Fact]
    public void Transcription_defaults_to_owner_only_and_queued()
    {
        var record = new HandwritingTranscription(
            "1880 census note",
            "documents/census/1880-note.jpg",
            "Family archive test citation");

        Assert.Equal(HandwritingTranscriptionStatus.Queued, record.Status);
        Assert.Equal(HandwritingTranscriptionVisibility.OwnerOnly, record.Visibility);
        Assert.Equal("documents/census/1880-note.jpg", record.ArchiveRelativePath);
        Assert.Empty(record.MachineDraft);
        Assert.Empty(record.ApprovedTranscript);
    }

    [Fact]
    public void Transcription_workflow_does_not_modify_legacy_number()
    {
        var person = new Person("Test", "Ancestor");
        person.AddLegacyNumber(" 21314.00 ");
        var exactLegacy = person.Identifiers.Single().Value;

        var record = new HandwritingTranscription(
            "Family letter",
            "documents/letters/family-letter.jpg",
            personId: person.Id);
        record.RecordMachineDraft("Machine draft", "Test provider", "vision-test", 0.73m);
        record.SaveReviewedTranscript("Corrected family transcription");
        record.Approve();

        Assert.Equal(exactLegacy, person.Identifiers.Single().Value);
        Assert.Equal(" 21314.00 ", exactLegacy);
        Assert.Equal(person.Id, record.PersonId);
    }

    [Fact]
    public void Human_review_preserves_machine_draft_and_approves_correction()
    {
        var record = new HandwritingTranscription("Letter", "documents/letter.jpg");
        record.RecordMachineDraft("Ths is teh draft", "Vision provider", "model-a", 0.61m);
        var machineDraft = record.MachineDraft;

        record.SaveReviewedTranscript("This is the corrected transcript.");
        record.Approve();

        Assert.Equal(machineDraft, record.MachineDraft);
        Assert.Equal("This is the corrected transcript.", record.ApprovedTranscript);
        Assert.Equal(HandwritingTranscriptionStatus.Approved, record.Status);
        Assert.NotNull(record.ApprovedUtc);
    }

    [Fact]
    public void Approval_without_human_edits_preserves_machine_draft_as_final_text()
    {
        var record = new HandwritingTranscription("Card", "documents/card.jpg");
        record.RecordMachineDraft("Accurate draft", "Vision provider", "model-a", 0.98m);

        record.Approve();

        Assert.Equal("Accurate draft", record.MachineDraft);
        Assert.Equal("Accurate draft", record.ApprovedTranscript);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void Confidence_outside_zero_to_one_is_rejected(double value)
    {
        var record = new HandwritingTranscription("Card", "documents/card.jpg");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            record.RecordMachineDraft("Draft", "Provider", "Model", (decimal)value));
    }

    [Fact]
    public void Original_image_hash_requires_sha256_shape()
    {
        var record = new HandwritingTranscription("Card", "documents/card.jpg");

        Assert.Throws<ArgumentException>(() => record.SetOriginalIntegrityHash("1234"));
        record.SetOriginalIntegrityHash(new string('a', 64));

        Assert.Equal(new string('A', 64), record.OriginalImageHashSha256);
    }

    [Theory]
    [InlineData(HandwritingTranscriptionVisibility.FamilyArchive, HandwritingTranscriptionAccessScope.FamilyArchive, true)]
    [InlineData(HandwritingTranscriptionVisibility.OwnerOnly, HandwritingTranscriptionAccessScope.FamilyArchive, false)]
    [InlineData(HandwritingTranscriptionVisibility.OwnerOnly, HandwritingTranscriptionAccessScope.OwnerAdmin, true)]
    [InlineData(HandwritingTranscriptionVisibility.FamilyArchive, HandwritingTranscriptionAccessScope.None, false)]
    public void Transcription_privacy_policy_requires_scope(
        HandwritingTranscriptionVisibility visibility,
        HandwritingTranscriptionAccessScope scope,
        bool expected)
    {
        Assert.Equal(expected, HandwritingTranscriptionPrivacyPolicy.CanView(visibility, scope));
    }

    [Fact]
    public void Only_owner_admin_can_edit_transcription_records()
    {
        Assert.True(HandwritingTranscriptionPrivacyPolicy.CanEdit(HandwritingTranscriptionAccessScope.OwnerAdmin));
        Assert.False(HandwritingTranscriptionPrivacyPolicy.CanEdit(HandwritingTranscriptionAccessScope.FamilyArchive));
        Assert.False(HandwritingTranscriptionPrivacyPolicy.CanEdit(HandwritingTranscriptionAccessScope.None));
    }
}
