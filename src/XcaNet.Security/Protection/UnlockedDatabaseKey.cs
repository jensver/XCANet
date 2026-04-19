using System.Security.Cryptography;

namespace XcaNet.Security.Protection;

public sealed class UnlockedDatabaseKey : IDisposable
{
    private bool _disposed;

    public UnlockedDatabaseKey(byte[] keyBytes, int keyVersion, string kdfAlgorithm, int kdfIterations)
    {
        KeyBytes = keyBytes;
        KeyVersion = keyVersion;
        KdfAlgorithm = kdfAlgorithm;
        KdfIterations = kdfIterations;
    }

    public byte[] KeyBytes { get; private set; }

    public int KeyVersion { get; }

    public string KdfAlgorithm { get; }

    public int KdfIterations { get; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        CryptographicOperations.ZeroMemory(KeyBytes);
        KeyBytes = [];
        _disposed = true;
    }
}
