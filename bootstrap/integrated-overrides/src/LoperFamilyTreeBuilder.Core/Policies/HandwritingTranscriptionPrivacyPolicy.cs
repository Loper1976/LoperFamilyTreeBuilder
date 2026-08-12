using LoperFamilyTreeBuilder.Core.Entities;
using LoperFamilyTreeBuilder.Core.Models;

namespace LoperFamilyTreeBuilder.Core.Policies;

public static class HandwritingTranscriptionPrivacyPolicy
{
    public static bool CanView(
        HandwritingTranscriptionVisibility visibility,
        HandwritingTranscriptionAccessScope accessScope)
    {
        return accessScope switch
        {
            HandwritingTranscriptionAccessScope.OwnerAdmin => true,
            HandwritingTranscriptionAccessScope.FamilyArchive =>
                visibility == HandwritingTranscriptionVisibility.FamilyArchive,
            _ => false
        };
    }

    public static bool CanEdit(HandwritingTranscriptionAccessScope accessScope) =>
        accessScope == HandwritingTranscriptionAccessScope.OwnerAdmin;
}
