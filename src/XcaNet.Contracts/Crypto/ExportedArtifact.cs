namespace XcaNet.Contracts.Crypto;

public sealed record ExportedArtifact(
    CryptoDataFormat Format,
    byte[] Data,
    string? TextRepresentation,
    string ContentType,
    string FileName);
