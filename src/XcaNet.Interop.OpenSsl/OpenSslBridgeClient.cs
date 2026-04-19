using System.Runtime.InteropServices;
using System.Text;
using XcaNet.Contracts.Results;

namespace XcaNet.Interop.OpenSsl;

public sealed class OpenSslBridgeClient : IOpenSslBridgeClient, IDisposable
{
    private readonly OpenSslNativeLibrary? _nativeLibrary;
    private OpenSslDiagnosticsSnapshot _diagnostics;

    public OpenSslBridgeClient(OpenSslBridgeOptions? options = null)
    {
        try
        {
            _nativeLibrary = OpenSslNativeLibrary.Load(options?.LibraryPath);
            _diagnostics = ProbeInternal();
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            _diagnostics = new OpenSslDiagnosticsSnapshot(false, null, OpenSslBridgeCapabilities.None, ex.Message, null);
        }
    }

    public OpenSslDiagnosticsSnapshot Diagnostics => _diagnostics;

    public OperationResult<OpenSslDiagnosticsSnapshot> Probe()
    {
        if (_nativeLibrary is null)
        {
            return OperationResult<OpenSslDiagnosticsSnapshot>.Failure(OperationErrorCode.ValidationFailed, _diagnostics.LastLoadError ?? "OpenSSL bridge is unavailable.");
        }

        try
        {
            _diagnostics = ProbeInternal();
            return OperationResult<OpenSslDiagnosticsSnapshot>.Success(_diagnostics, "OpenSSL bridge probed.");
        }
        catch (InvalidOperationException ex)
        {
            _diagnostics = new OpenSslDiagnosticsSnapshot(false, null, OpenSslBridgeCapabilities.None, ex.Message, null);
            return OperationResult<OpenSslDiagnosticsSnapshot>.Failure(OperationErrorCode.ValidationFailed, ex.Message);
        }
    }

    public OperationResult SelfTest()
    {
        if (_nativeLibrary is null)
        {
            return OperationResult.Failure(OperationErrorCode.ValidationFailed, _diagnostics.LastLoadError ?? "OpenSSL bridge is unavailable.");
        }

        var nativeError = OpenSslNativeError.Create();
        var result = _nativeLibrary.SelfTest(ref nativeError);
        return result == 0
            ? OperationResult.Success("OpenSSL bridge self-test passed.")
            : OperationResult.Failure(OperationErrorCode.ValidationFailed, nativeError.ToMessage("OpenSSL bridge self-test failed."));
    }

    public OperationResult<byte[]> SignCertificateSigningRequest(OpenSslSignCertificateSigningRequestRequest request)
    {
        if (_nativeLibrary is null)
        {
            return OperationResult<byte[]>.Failure(OperationErrorCode.ValidationFailed, _diagnostics.LastLoadError ?? "OpenSSL bridge is unavailable.");
        }

        var nativeError = OpenSslNativeError.Create();
        var output = OpenSslNativeBuffer.Empty;
        try
        {
            var result = _nativeLibrary.SignCertificateSigningRequest(
                request.CertificateSigningRequestDer,
                request.CertificateSigningRequestDer.Length,
                request.IssuerCertificateDer,
                request.IssuerCertificateDer.Length,
                request.IssuerPrivateKeyPkcs8,
                request.IssuerPrivateKeyPkcs8.Length,
                request.ValidityDays,
                ref output,
                ref nativeError);

            if (result != 0)
            {
                return OperationResult<byte[]>.Failure(OperationErrorCode.ValidationFailed, nativeError.ToMessage("OpenSSL CSR signing failed."));
            }

            return OperationResult<byte[]>.Success(output.ToManagedArray(), "OpenSSL CSR signing succeeded.");
        }
        finally
        {
            if (_nativeLibrary is not null)
            {
                _nativeLibrary.FreeBuffer(ref output);
            }
        }
    }

    public void Dispose()
    {
        _nativeLibrary?.Dispose();
    }

    private OpenSslDiagnosticsSnapshot ProbeInternal()
    {
        if (_nativeLibrary is null)
        {
            return new OpenSslDiagnosticsSnapshot(false, null, OpenSslBridgeCapabilities.None, "OpenSSL bridge is unavailable.", null);
        }

        var versionBuffer = new byte[512];
        var versionResult = _nativeLibrary.GetVersion(versionBuffer, versionBuffer.Length);
        if (versionResult != 0)
        {
            throw new InvalidOperationException("Failed to query the OpenSSL bridge version.");
        }

        var capabilities = OpenSslNativeCapabilities.Empty;
        var capabilitiesResult = _nativeLibrary.GetCapabilities(ref capabilities);
        if (capabilitiesResult != 0)
        {
            throw new InvalidOperationException("Failed to query OpenSSL bridge capabilities.");
        }

        return new OpenSslDiagnosticsSnapshot(
            true,
            Encoding.UTF8.GetString(versionBuffer.AsSpan(0, Array.IndexOf(versionBuffer, (byte)0) is var idx && idx >= 0 ? idx : versionBuffer.Length)),
            capabilities.ToManaged(),
            null,
            _nativeLibrary.LoadedPath);
    }

    private sealed class OpenSslNativeLibrary : IDisposable
    {
        private readonly nint _handle;

        private OpenSslNativeLibrary(nint handle, string loadedPath)
        {
            _handle = handle;
            LoadedPath = loadedPath;
            GetVersion = GetDelegate<GetVersionDelegate>("xcanet_ossl_get_version");
            GetCapabilities = GetDelegate<GetCapabilitiesDelegate>("xcanet_ossl_get_capabilities");
            SelfTest = GetDelegate<SelfTestDelegate>("xcanet_ossl_self_test");
            SignCertificateSigningRequest = GetDelegate<SignCertificateSigningRequestDelegate>("xcanet_ossl_sign_csr");
            FreeBuffer = GetDelegate<FreeBufferDelegate>("xcanet_ossl_free_buffer");
        }

        public GetVersionDelegate GetVersion { get; }
        public string LoadedPath { get; }

        public GetCapabilitiesDelegate GetCapabilities { get; }

        public SelfTestDelegate SelfTest { get; }

        public SignCertificateSigningRequestDelegate SignCertificateSigningRequest { get; }

        public FreeBufferDelegate FreeBuffer { get; }

        public static OpenSslNativeLibrary Load(string? explicitPath)
        {
            var candidates = ResolveCandidates(explicitPath);
            var failures = new List<string>();

            foreach (var candidate in candidates)
            {
                if (string.IsNullOrWhiteSpace(candidate))
                {
                    continue;
                }

                if (!File.Exists(candidate))
                {
                    failures.Add($"not found: {candidate}");
                    continue;
                }

                try
                {
                    var handle = NativeLibrary.Load(candidate);
                    return new OpenSslNativeLibrary(handle, candidate);
                }
                catch (BadImageFormatException ex)
                {
                    failures.Add($"architecture mismatch or invalid binary at {candidate}: {ex.Message}");
                }
                catch (DllNotFoundException ex)
                {
                    failures.Add($"missing dependency while loading {candidate}: {ex.Message}");
                }
                catch (EntryPointNotFoundException ex)
                {
                    failures.Add($"bridge ABI mismatch in {candidate}: {ex.Message}");
                }
            }

            var message = new StringBuilder()
                .AppendLine("Unable to load the optional XcaNet OpenSSL bridge.")
                .AppendLine($"Platform: {RuntimeInformation.OSDescription}")
                .AppendLine($"Architecture: {RuntimeInformation.ProcessArchitecture}")
                .AppendLine("Managed fallback remains available.")
                .AppendLine("Candidates:")
                .AppendLine(string.Join(Environment.NewLine, candidates.Select(x => $" - {x}")))
                .AppendLine("Failures:")
                .AppendLine(failures.Count == 0 ? " - no candidate files were found" : string.Join(Environment.NewLine, failures.Select(x => $" - {x}")))
                .AppendLine("Hints:")
                .AppendLine(" - Set XCANET_OPENSSL_BRIDGE_PATH or Crypto:OpenSslBridgePath to a specific bridge artifact.")
                .AppendLine(" - Ensure the bridge architecture matches the current process.")
                .AppendLine(" - Ensure libssl/libcrypto are installed and loadable on the target system.")
                .ToString()
                .TrimEnd();

            throw new DllNotFoundException(message);
        }

        public void Dispose()
        {
            if (_handle != 0)
            {
                NativeLibrary.Free(_handle);
            }
        }

        private TDelegate GetDelegate<TDelegate>(string exportName)
            where TDelegate : Delegate
        {
            var pointer = NativeLibrary.GetExport(_handle, exportName);
            return Marshal.GetDelegateForFunctionPointer<TDelegate>(pointer);
        }

        private static IReadOnlyList<string> ResolveCandidates(string? explicitPath)
        {
            if (!string.IsNullOrWhiteSpace(explicitPath))
            {
                return [explicitPath];
            }

            var libraryName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "xcanet_ossl_bridge.dll"
                : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                    ? "libxcanet_ossl_bridge.dylib"
                    : "libxcanet_ossl_bridge.so";

            var baseDirectory = AppContext.BaseDirectory;
            var currentDirectory = Environment.CurrentDirectory;
            var repositoryRoot = FindRepositoryRoot();
            var candidates = new List<string>
            {
                Path.Combine(baseDirectory, libraryName),
                Path.Combine(baseDirectory, "native", libraryName),
                Path.Combine(baseDirectory, "bridges", libraryName),
                Path.Combine(baseDirectory, "runtimes", GetRuntimeIdSegment(), "native", libraryName),
                Path.Combine(currentDirectory, libraryName)
            };

            if (repositoryRoot is not null)
            {
                candidates.Add(Path.Combine(repositoryRoot, "native", "xcanet_ossl_bridge", "build", libraryName));
            }

            var envPath = Environment.GetEnvironmentVariable("XCANET_OPENSSL_BRIDGE_PATH");
            if (!string.IsNullOrWhiteSpace(envPath))
            {
                candidates.Insert(0, envPath);
            }

            return candidates;
        }

        private static string? FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "XcaNet.sln")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            return null;
        }

        private static string GetRuntimeIdSegment()
        {
            var os = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "win"
                : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                    ? "osx"
                    : "linux";

            var arch = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.Arm64 => "arm64",
                Architecture.X64 => "x64",
                Architecture.X86 => "x86",
                _ => RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant()
            };

            return $"{os}-{arch}";
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int GetVersionDelegate(byte[] buffer, int bufferLength);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int GetCapabilitiesDelegate(ref OpenSslNativeCapabilities capabilities);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int SelfTestDelegate(ref OpenSslNativeError error);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int SignCertificateSigningRequestDelegate(
        byte[] csrDer,
        int csrDerLength,
        byte[] issuerCertificateDer,
        int issuerCertificateDerLength,
        byte[] issuerPrivateKeyPkcs8,
        int issuerPrivateKeyPkcs8Length,
        int validityDays,
        ref OpenSslNativeBuffer output,
        ref OpenSslNativeError error);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void FreeBufferDelegate(ref OpenSslNativeBuffer output);

    [StructLayout(LayoutKind.Sequential)]
    private struct OpenSslNativeCapabilities
    {
        public uint Flags;
        public int SupportsSignCertificateSigningRequest;

        public static OpenSslNativeCapabilities Empty => default;

        public OpenSslBridgeCapabilities ToManaged()
        {
            var capabilities = OpenSslBridgeCapabilities.None;
            if ((Flags & 0x00000001u) != 0 || SupportsSignCertificateSigningRequest == 1)
            {
                capabilities |= OpenSslBridgeCapabilities.SupportsCertificateSigningRequestSigning;
            }

            return capabilities;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct OpenSslNativeBuffer
    {
        public nint Data;
        public int Length;

        public static OpenSslNativeBuffer Empty => default;

        public byte[] ToManagedArray()
        {
            if (Data == 0 || Length <= 0)
            {
                return [];
            }

            var data = new byte[Length];
            Marshal.Copy(Data, data, 0, Length);
            return data;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct OpenSslNativeError
    {
        public int Code;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] Message;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
        public byte[] Detail;

        public static OpenSslNativeError Create()
        {
            return new OpenSslNativeError
            {
                Message = new byte[256],
                Detail = new byte[512]
            };
        }

        public string ToMessage(string fallback)
        {
            var message = Decode(Message);
            var detail = Decode(Detail);

            return string.IsNullOrWhiteSpace(message)
                ? fallback
                : string.IsNullOrWhiteSpace(detail)
                    ? message
                    : $"{message} ({detail})";
        }

        private static string Decode(byte[] bytes)
        {
            var nullIndex = Array.IndexOf(bytes, (byte)0);
            var length = nullIndex >= 0 ? nullIndex : bytes.Length;
            return Encoding.UTF8.GetString(bytes, 0, length).Trim();
        }
    }
}
