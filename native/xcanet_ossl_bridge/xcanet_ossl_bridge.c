#include "xcanet_ossl_bridge.h"

#include <openssl/asn1.h>
#include <openssl/bn.h>
#include <openssl/crypto.h>
#include <openssl/err.h>
#include <openssl/evp.h>
#include <openssl/obj_mac.h>
#include <openssl/objects.h>
#include <openssl/pem.h>
#include <openssl/rand.h>
#include <openssl/x509.h>
#include <openssl/x509v3.h>

#include <stdio.h>
#include <string.h>

static void xcanet_clear_error(xcanet_error* err)
{
    if (err == NULL)
    {
        return;
    }

    memset(err, 0, sizeof(*err));
}

static void xcanet_set_error(xcanet_error* err, int code, const char* message, const char* detail)
{
    if (err == NULL)
    {
        return;
    }

    xcanet_clear_error(err);
    err->code = code;

    if (message != NULL)
    {
        snprintf(err->message, sizeof(err->message), "%s", message);
    }

    if (detail != NULL)
    {
        snprintf(err->detail, sizeof(err->detail), "%s", detail);
    }
}

static void xcanet_set_last_openssl_error(xcanet_error* err, const char* message)
{
    unsigned long error_code = ERR_peek_last_error();
    if (error_code == 0)
    {
        xcanet_set_error(err, XCANET_OSSL_ERROR_OPENSSL, message, "OpenSSL reported an unspecified error.");
        return;
    }

    char detail[XCANET_OSSL_ERROR_DETAIL_LENGTH];
    ERR_error_string_n(error_code, detail, sizeof(detail));
    xcanet_set_error(err, XCANET_OSSL_ERROR_OPENSSL, message, detail);
}

static int xcanet_copy_version(char* buffer, int buffer_len)
{
    const char* version = OpenSSL_version(OPENSSL_VERSION);
    int required_length;

    if (buffer == NULL || buffer_len <= 0)
    {
        return XCANET_OSSL_ERROR_INVALID_ARGUMENT;
    }

    required_length = (int)strlen(version) + 1;
    if (buffer_len < required_length)
    {
        return XCANET_OSSL_ERROR_INVALID_ARGUMENT;
    }

    snprintf(buffer, (size_t)buffer_len, "%s", version);
    return XCANET_OSSL_OK;
}

static EVP_PKEY* xcanet_load_private_key_from_pkcs8(const unsigned char* der, int der_len, xcanet_error* err)
{
    const unsigned char* cursor;
    PKCS8_PRIV_KEY_INFO* pkcs8;
    EVP_PKEY* pkey;

    cursor = der;
    pkcs8 = d2i_PKCS8_PRIV_KEY_INFO(NULL, &cursor, der_len);
    if (pkcs8 == NULL)
    {
        xcanet_set_last_openssl_error(err, "Failed to decode PKCS#8 private key.");
        return NULL;
    }

    pkey = EVP_PKCS82PKEY(pkcs8);
    PKCS8_PRIV_KEY_INFO_free(pkcs8);

    if (pkey == NULL)
    {
        xcanet_set_last_openssl_error(err, "Failed to convert PKCS#8 private key.");
    }

    return pkey;
}

static int xcanet_set_random_serial(X509* certificate, xcanet_error* err)
{
    unsigned char serial_bytes[16];
    BIGNUM* serial_number;
    ASN1_INTEGER* asn1_serial;
    int status;

    if (RAND_bytes(serial_bytes, sizeof(serial_bytes)) != 1)
    {
        xcanet_set_last_openssl_error(err, "Failed to generate certificate serial number.");
        return XCANET_OSSL_ERROR_OPENSSL;
    }

    serial_bytes[0] &= 0x7F;
    serial_number = BN_bin2bn(serial_bytes, (int)sizeof(serial_bytes), NULL);
    if (serial_number == NULL)
    {
        xcanet_set_last_openssl_error(err, "Failed to allocate serial number.");
        return XCANET_OSSL_ERROR_INTERNAL;
    }

    asn1_serial = BN_to_ASN1_INTEGER(serial_number, NULL);
    BN_free(serial_number);

    if (asn1_serial == NULL)
    {
        xcanet_set_last_openssl_error(err, "Failed to encode serial number.");
        return XCANET_OSSL_ERROR_INTERNAL;
    }

    status = X509_set_serialNumber(certificate, asn1_serial);
    ASN1_INTEGER_free(asn1_serial);

    if (status != 1)
    {
        xcanet_set_last_openssl_error(err, "Failed to set certificate serial number.");
        return XCANET_OSSL_ERROR_OPENSSL;
    }

    return XCANET_OSSL_OK;
}

int xcanet_ossl_is_available(void)
{
    return 1;
}

int xcanet_ossl_get_version(char* buffer, int buffer_len)
{
    return xcanet_copy_version(buffer, buffer_len);
}

int xcanet_ossl_get_capabilities(xcanet_capabilities* out_caps)
{
    if (out_caps == NULL)
    {
        return XCANET_OSSL_ERROR_INVALID_ARGUMENT;
    }

    memset(out_caps, 0, sizeof(*out_caps));
    out_caps->flags = XCANET_OSSL_CAP_SIGN_CSR;
    out_caps->supports_sign_csr = 1;
    return XCANET_OSSL_OK;
}

int xcanet_ossl_self_test(xcanet_error* err)
{
    char version_buffer[128];
    int result;

    xcanet_clear_error(err);
    result = xcanet_copy_version(version_buffer, (int)sizeof(version_buffer));
    if (result != XCANET_OSSL_OK)
    {
        xcanet_set_error(err, result, "Failed to probe OpenSSL version.", NULL);
        return result;
    }

    if (RAND_status() != 1)
    {
        xcanet_set_error(err, XCANET_OSSL_ERROR_OPENSSL, "OpenSSL random subsystem is not ready.", NULL);
        return XCANET_OSSL_ERROR_OPENSSL;
    }

    return XCANET_OSSL_OK;
}

int xcanet_ossl_sign_csr(
    const unsigned char* csr_der,
    int csr_der_len,
    const unsigned char* issuer_cert_der,
    int issuer_cert_der_len,
    const unsigned char* issuer_key_pkcs8_der,
    int issuer_key_pkcs8_der_len,
    int validity_days,
    xcanet_buffer* out_cert_der,
    xcanet_error* err)
{
    const unsigned char* cursor;
    X509_REQ* csr;
    X509* issuer_cert;
    EVP_PKEY* issuer_key;
    EVP_PKEY* subject_key;
    STACK_OF(X509_EXTENSION)* extensions;
    X509* certificate;
    unsigned char* der_data;
    unsigned char* der_cursor;
    int der_length;
    int index;

    xcanet_clear_error(err);

    if (csr_der == NULL || csr_der_len <= 0 ||
        issuer_cert_der == NULL || issuer_cert_der_len <= 0 ||
        issuer_key_pkcs8_der == NULL || issuer_key_pkcs8_der_len <= 0 ||
        validity_days <= 0 || out_cert_der == NULL)
    {
        xcanet_set_error(err, XCANET_OSSL_ERROR_INVALID_ARGUMENT, "Invalid signing arguments.", NULL);
        return XCANET_OSSL_ERROR_INVALID_ARGUMENT;
    }

    out_cert_der->data = NULL;
    out_cert_der->len = 0;

    cursor = csr_der;
    csr = d2i_X509_REQ(NULL, &cursor, csr_der_len);
    if (csr == NULL)
    {
        xcanet_set_last_openssl_error(err, "Failed to decode CSR.");
        return XCANET_OSSL_ERROR_OPENSSL;
    }

    cursor = issuer_cert_der;
    issuer_cert = d2i_X509(NULL, &cursor, issuer_cert_der_len);
    if (issuer_cert == NULL)
    {
        X509_REQ_free(csr);
        xcanet_set_last_openssl_error(err, "Failed to decode issuer certificate.");
        return XCANET_OSSL_ERROR_OPENSSL;
    }

    issuer_key = xcanet_load_private_key_from_pkcs8(issuer_key_pkcs8_der, issuer_key_pkcs8_der_len, err);
    if (issuer_key == NULL)
    {
        X509_free(issuer_cert);
        X509_REQ_free(csr);
        return err != NULL && err->code != 0 ? err->code : XCANET_OSSL_ERROR_OPENSSL;
    }

    certificate = X509_new();
    if (certificate == NULL)
    {
        EVP_PKEY_free(issuer_key);
        X509_free(issuer_cert);
        X509_REQ_free(csr);
        xcanet_set_last_openssl_error(err, "Failed to allocate certificate.");
        return XCANET_OSSL_ERROR_INTERNAL;
    }

    if (X509_set_version(certificate, 2) != 1 ||
        xcanet_set_random_serial(certificate, err) != XCANET_OSSL_OK ||
        X509_set_issuer_name(certificate, X509_get_subject_name(issuer_cert)) != 1 ||
        X509_set_subject_name(certificate, X509_REQ_get_subject_name(csr)) != 1 ||
        X509_gmtime_adj(X509_getm_notBefore(certificate), -300) == NULL ||
        X509_gmtime_adj(X509_getm_notAfter(certificate), (long)validity_days * 24L * 60L * 60L) == NULL)
    {
        if (err != NULL && err->code == 0)
        {
            xcanet_set_last_openssl_error(err, "Failed to initialize issued certificate.");
        }

        X509_free(certificate);
        EVP_PKEY_free(issuer_key);
        X509_free(issuer_cert);
        X509_REQ_free(csr);
        return err != NULL && err->code != 0 ? err->code : XCANET_OSSL_ERROR_OPENSSL;
    }

    subject_key = X509_REQ_get_pubkey(csr);
    if (subject_key == NULL || X509_set_pubkey(certificate, subject_key) != 1)
    {
        EVP_PKEY_free(subject_key);
        X509_free(certificate);
        EVP_PKEY_free(issuer_key);
        X509_free(issuer_cert);
        X509_REQ_free(csr);
        xcanet_set_last_openssl_error(err, "Failed to copy CSR public key.");
        return XCANET_OSSL_ERROR_OPENSSL;
    }

    EVP_PKEY_free(subject_key);

    extensions = X509_REQ_get_extensions(csr);
    if (extensions != NULL)
    {
        for (index = 0; index < sk_X509_EXTENSION_num(extensions); ++index)
        {
            X509_EXTENSION* extension = sk_X509_EXTENSION_value(extensions, index);
            if (X509_add_ext(certificate, extension, -1) != 1)
            {
                sk_X509_EXTENSION_pop_free(extensions, X509_EXTENSION_free);
                X509_free(certificate);
                EVP_PKEY_free(issuer_key);
                X509_free(issuer_cert);
                X509_REQ_free(csr);
                xcanet_set_last_openssl_error(err, "Failed to copy CSR extensions.");
                return XCANET_OSSL_ERROR_OPENSSL;
            }
        }

        sk_X509_EXTENSION_pop_free(extensions, X509_EXTENSION_free);
    }

    if (X509_sign(certificate, issuer_key, EVP_sha256()) <= 0)
    {
        X509_free(certificate);
        EVP_PKEY_free(issuer_key);
        X509_free(issuer_cert);
        X509_REQ_free(csr);
        xcanet_set_last_openssl_error(err, "Failed to sign issued certificate.");
        return XCANET_OSSL_ERROR_OPENSSL;
    }

    der_length = i2d_X509(certificate, NULL);
    if (der_length <= 0)
    {
        X509_free(certificate);
        EVP_PKEY_free(issuer_key);
        X509_free(issuer_cert);
        X509_REQ_free(csr);
        xcanet_set_last_openssl_error(err, "Failed to compute certificate size.");
        return XCANET_OSSL_ERROR_OPENSSL;
    }

    der_data = OPENSSL_malloc((size_t)der_length);
    if (der_data == NULL)
    {
        X509_free(certificate);
        EVP_PKEY_free(issuer_key);
        X509_free(issuer_cert);
        X509_REQ_free(csr);
        xcanet_set_error(err, XCANET_OSSL_ERROR_INTERNAL, "Failed to allocate certificate output buffer.", NULL);
        return XCANET_OSSL_ERROR_INTERNAL;
    }

    der_cursor = der_data;
    if (i2d_X509(certificate, &der_cursor) != der_length)
    {
        OPENSSL_free(der_data);
        X509_free(certificate);
        EVP_PKEY_free(issuer_key);
        X509_free(issuer_cert);
        X509_REQ_free(csr);
        xcanet_set_last_openssl_error(err, "Failed to encode issued certificate.");
        return XCANET_OSSL_ERROR_OPENSSL;
    }

    out_cert_der->data = der_data;
    out_cert_der->len = der_length;

    X509_free(certificate);
    EVP_PKEY_free(issuer_key);
    X509_free(issuer_cert);
    X509_REQ_free(csr);
    return XCANET_OSSL_OK;
}

void xcanet_ossl_free_buffer(xcanet_buffer* buffer)
{
    if (buffer == NULL || buffer->data == NULL)
    {
        return;
    }

    OPENSSL_clear_free(buffer->data, (size_t)buffer->len);
    buffer->data = NULL;
    buffer->len = 0;
}
