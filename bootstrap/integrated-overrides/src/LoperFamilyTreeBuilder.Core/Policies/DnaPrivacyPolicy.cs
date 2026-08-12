using LoperFamilyTreeBuilder.Core.Entities;
using LoperFamilyTreeBuilder.Core.Models;

namespace LoperFamilyTreeBuilder.Core.Policies;

public static class DnaPrivacyPolicy
{
    public static bool CanView(DnaMatchVisibility visibility, DnaAccessScope accessScope)
    {
        return accessScope switch
        {
            DnaAccessScope.OwnerAdmin => true,
            DnaAccessScope.DnaAuthorized => visibility == DnaMatchVisibility.DnaAuthorized,
            _ => false
        };
    }

    public static bool CanEdit(DnaAccessScope accessScope) =>
        accessScope == DnaAccessScope.OwnerAdmin;
}
