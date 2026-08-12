using LoperFamilyTreeBuilder.Core.Entities;
using LoperFamilyTreeBuilder.Core.Models;

namespace LoperFamilyTreeBuilder.Core.Policies;

public static class MedicalPrivacyPolicy
{
    public static bool CanView(
        MedicalRecordVisibility visibility,
        MedicalAccessScope accessScope)
    {
        return visibility switch
        {
            MedicalRecordVisibility.FamilySummary =>
                accessScope >= MedicalAccessScope.FamilySummary,

            MedicalRecordVisibility.MedicalAuthorized =>
                accessScope >= MedicalAccessScope.MedicalAuthorized,

            MedicalRecordVisibility.OwnerOnly =>
                accessScope >= MedicalAccessScope.OwnerAdmin,

            _ => false
        };
    }
}
