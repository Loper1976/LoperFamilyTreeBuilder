using LoperFamilyTreeBuilder.Core.Entities;
using LoperFamilyTreeBuilder.Core.Models;
using LoperFamilyTreeBuilder.Core.Policies;

namespace LoperFamilyTreeBuilder.Tests;

public sealed class MedicalHealthFoundationTests
{
    [Fact]
    public void Medical_condition_does_not_modify_person_or_legacy_number()
    {
        var person = new Person("Test", "Ancestor");
        person.AddLegacyNumber("21314.00");
        var originalLegacy = person.Identifiers.Single().Value;

        var record = new MedicalCondition(person.Id, "Hypertension");
        record.UpdateClinicalSummary(
            MedicalConditionStatus.Confirmed,
            MedicalConditionSeverity.Moderate,
            true,
            new DateOnly(2020, 1, 2),
            44);

        Assert.Equal(person.Id, record.PersonId);
        Assert.Equal(originalLegacy, person.Identifiers.Single().Value);
        Assert.Equal("21314.00", originalLegacy);
    }

    [Fact]
    public void Medical_condition_defaults_to_protected_visibility()
    {
        var personId = Guid.NewGuid();
        var record = new MedicalCondition(personId, "Stroke");

        Assert.Equal(MedicalRecordVisibility.MedicalAuthorized, record.Visibility);
        Assert.Equal(MedicalConditionStatus.FamilyReported, record.Status);
    }

    [Theory]
    [InlineData(MedicalRecordVisibility.FamilySummary, MedicalAccessScope.FamilySummary, true)]
    [InlineData(MedicalRecordVisibility.MedicalAuthorized, MedicalAccessScope.FamilySummary, false)]
    [InlineData(MedicalRecordVisibility.MedicalAuthorized, MedicalAccessScope.MedicalAuthorized, true)]
    [InlineData(MedicalRecordVisibility.OwnerOnly, MedicalAccessScope.MedicalAuthorized, false)]
    [InlineData(MedicalRecordVisibility.OwnerOnly, MedicalAccessScope.OwnerAdmin, true)]
    [InlineData(MedicalRecordVisibility.FamilySummary, MedicalAccessScope.None, false)]
    public void Medical_privacy_policy_requires_sufficient_scope(
        MedicalRecordVisibility visibility,
        MedicalAccessScope scope,
        bool expected)
    {
        Assert.Equal(expected, MedicalPrivacyPolicy.CanView(visibility, scope));
    }

    [Fact]
    public void Onset_age_rejects_impossible_values()
    {
        var record = new MedicalCondition(Guid.NewGuid(), "Example condition");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            record.UpdateClinicalSummary(
                MedicalConditionStatus.Active,
                MedicalConditionSeverity.Mild,
                false,
                null,
                126));
    }
}
