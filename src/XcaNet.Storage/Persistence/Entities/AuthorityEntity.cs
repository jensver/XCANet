namespace XcaNet.Storage.Persistence.Entities;

public sealed class AuthorityEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid CertificateId { get; set; }
    public Guid? PrivateKeyId { get; set; }
    public Guid? ParentAuthorityId { get; set; }
}
