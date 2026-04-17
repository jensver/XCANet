namespace XcaNet.Core.Entities;

public sealed record AuthorityRecord(
    Guid Id,
    string Name,
    Guid CertificateId,
    Guid? PrivateKeyId,
    Guid? ParentAuthorityId);
