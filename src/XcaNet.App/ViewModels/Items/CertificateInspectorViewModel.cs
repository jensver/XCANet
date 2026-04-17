using System.Collections.ObjectModel;
using XcaNet.Contracts.Browser;
using XcaNet.Contracts.Crypto;

namespace XcaNet.App.ViewModels.Items;

public sealed class CertificateInspectorViewModel : ViewModelBase
{
    private Guid _certificateId;
    private string _displayName = "No certificate selected.";
    private string _subject = string.Empty;
    private string _issuer = string.Empty;
    private string _issuerDisplayName = string.Empty;
    private string _serialNumber = string.Empty;
    private string _notBefore = string.Empty;
    private string _notAfter = string.Empty;
    private string _sha1Thumbprint = string.Empty;
    private string _sha256Thumbprint = string.Empty;
    private string _keyAlgorithm = string.Empty;
    private string _certificateAuthorityStatus = string.Empty;
    private string _subjectAlternativeNames = string.Empty;
    private string _keyUsage = string.Empty;
    private string _enhancedKeyUsage = string.Empty;
    private string _revocationStatus = string.Empty;
    private Guid? _issuerCertificateId;
    private Guid? _privateKeyId;
    private string _privateKeyDisplayName = string.Empty;
    private RelatedCertificateSummary? _selectedChildCertificate;

    public ObservableCollection<RelatedCertificateSummary> ChildCertificates { get; } = [];

    public Guid CertificateId
    {
        get => _certificateId;
        private set => SetProperty(ref _certificateId, value);
    }

    public string DisplayName
    {
        get => _displayName;
        private set => SetProperty(ref _displayName, value);
    }

    public string Subject
    {
        get => _subject;
        private set => SetProperty(ref _subject, value);
    }

    public string Issuer
    {
        get => _issuer;
        private set => SetProperty(ref _issuer, value);
    }

    public string IssuerDisplayName
    {
        get => _issuerDisplayName;
        private set => SetProperty(ref _issuerDisplayName, value);
    }

    public string SerialNumber
    {
        get => _serialNumber;
        private set => SetProperty(ref _serialNumber, value);
    }

    public string NotBefore
    {
        get => _notBefore;
        private set => SetProperty(ref _notBefore, value);
    }

    public string NotAfter
    {
        get => _notAfter;
        private set => SetProperty(ref _notAfter, value);
    }

    public string Sha1Thumbprint
    {
        get => _sha1Thumbprint;
        private set => SetProperty(ref _sha1Thumbprint, value);
    }

    public string Sha256Thumbprint
    {
        get => _sha256Thumbprint;
        private set => SetProperty(ref _sha256Thumbprint, value);
    }

    public string KeyAlgorithm
    {
        get => _keyAlgorithm;
        private set => SetProperty(ref _keyAlgorithm, value);
    }

    public string CertificateAuthorityStatus
    {
        get => _certificateAuthorityStatus;
        private set => SetProperty(ref _certificateAuthorityStatus, value);
    }

    public string SubjectAlternativeNames
    {
        get => _subjectAlternativeNames;
        private set => SetProperty(ref _subjectAlternativeNames, value);
    }

    public string KeyUsage
    {
        get => _keyUsage;
        private set => SetProperty(ref _keyUsage, value);
    }

    public string EnhancedKeyUsage
    {
        get => _enhancedKeyUsage;
        private set => SetProperty(ref _enhancedKeyUsage, value);
    }

    public string RevocationStatus
    {
        get => _revocationStatus;
        private set => SetProperty(ref _revocationStatus, value);
    }

    public Guid? IssuerCertificateId
    {
        get => _issuerCertificateId;
        private set
        {
            if (SetProperty(ref _issuerCertificateId, value))
            {
                OnPropertyChanged(nameof(CanOpenIssuer));
            }
        }
    }

    public Guid? PrivateKeyId
    {
        get => _privateKeyId;
        private set
        {
            if (SetProperty(ref _privateKeyId, value))
            {
                OnPropertyChanged(nameof(CanOpenPrivateKey));
            }
        }
    }

    public string PrivateKeyDisplayName
    {
        get => _privateKeyDisplayName;
        private set => SetProperty(ref _privateKeyDisplayName, value);
    }

    public RelatedCertificateSummary? SelectedChildCertificate
    {
        get => _selectedChildCertificate;
        set
        {
            if (SetProperty(ref _selectedChildCertificate, value))
            {
                OnPropertyChanged(nameof(CanOpenSelectedChild));
            }
        }
    }

    public bool HasCertificate => CertificateId != Guid.Empty;

    public bool CanOpenIssuer => IssuerCertificateId.HasValue;

    public bool CanOpenPrivateKey => PrivateKeyId.HasValue;

    public bool CanOpenSelectedChild => SelectedChildCertificate is not null;

    public void Clear()
    {
        CertificateId = Guid.Empty;
        DisplayName = "No certificate selected.";
        Subject = string.Empty;
        Issuer = string.Empty;
        IssuerDisplayName = string.Empty;
        SerialNumber = string.Empty;
        NotBefore = string.Empty;
        NotAfter = string.Empty;
        Sha1Thumbprint = string.Empty;
        Sha256Thumbprint = string.Empty;
        KeyAlgorithm = string.Empty;
        CertificateAuthorityStatus = string.Empty;
        SubjectAlternativeNames = string.Empty;
        KeyUsage = string.Empty;
        EnhancedKeyUsage = string.Empty;
        RevocationStatus = string.Empty;
        IssuerCertificateId = null;
        PrivateKeyId = null;
        PrivateKeyDisplayName = string.Empty;
        ChildCertificates.Clear();
        SelectedChildCertificate = null;
        OnPropertyChanged(nameof(HasCertificate));
    }

    public void Apply(CertificateInspector inspector)
    {
        CertificateId = inspector.CertificateId;
        DisplayName = inspector.DisplayName;
        ApplyDetails(inspector.Details);
        RevocationStatus = inspector.RevocationStatus;
        IssuerCertificateId = inspector.IssuerCertificateId;
        IssuerDisplayName = inspector.IssuerDisplayName ?? string.Empty;
        PrivateKeyId = inspector.PrivateKeyId;
        PrivateKeyDisplayName = inspector.PrivateKeyDisplayName ?? string.Empty;
        ChildCertificates.Clear();
        foreach (var child in inspector.ChildCertificates)
        {
            ChildCertificates.Add(child);
        }

        SelectedChildCertificate = ChildCertificates.FirstOrDefault();
        OnPropertyChanged(nameof(HasCertificate));
    }

    private void ApplyDetails(CertificateDetails details)
    {
        Subject = details.Subject;
        Issuer = details.Issuer;
        SerialNumber = details.SerialNumber;
        NotBefore = details.NotBefore.ToString("u");
        NotAfter = details.NotAfter.ToString("u");
        Sha1Thumbprint = details.Sha1Thumbprint;
        Sha256Thumbprint = details.Sha256Thumbprint;
        KeyAlgorithm = details.KeyAlgorithm;
        CertificateAuthorityStatus = details.IsCertificateAuthority ? "Certificate Authority" : "Leaf Certificate";
        SubjectAlternativeNames = details.SubjectAlternativeNames.Count == 0 ? "None" : string.Join(", ", details.SubjectAlternativeNames);
        KeyUsage = details.KeyUsages.Count == 0 ? "None" : string.Join(", ", details.KeyUsages);
        EnhancedKeyUsage = details.EnhancedKeyUsages.Count == 0 ? "None" : string.Join(", ", details.EnhancedKeyUsages);
    }
}
