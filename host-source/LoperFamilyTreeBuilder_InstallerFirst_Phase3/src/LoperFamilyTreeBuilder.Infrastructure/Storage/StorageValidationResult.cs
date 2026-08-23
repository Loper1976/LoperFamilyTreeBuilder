namespace LoperFamilyTreeBuilder.Infrastructure.Storage;

public sealed record StorageValidationResult(
    bool IsValid,
    string Path,
    string Message);
