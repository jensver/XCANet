namespace XcaNet.Core.Entities;

public sealed record DatabaseProfileRecord(
    Guid Id,
    string DisplayName,
    string KdfAlgorithm,
    int KdfIterations,
    DateTime CreatedUtc);
