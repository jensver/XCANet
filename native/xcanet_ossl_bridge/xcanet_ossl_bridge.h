#ifndef XCANET_OSSL_BRIDGE_H
#define XCANET_OSSL_BRIDGE_H

#ifdef __cplusplus
extern "C" {
#endif

/*
 * Ownership rule:
 * - Any xcanet_buffer returned from native code is owned by native code until
 *   xcanet_ossl_free_buffer is called.
 * - Callers must zero-initialize output structs before passing them in.
 */

#define XCANET_OSSL_ERROR_MESSAGE_LENGTH 256
#define XCANET_OSSL_ERROR_DETAIL_LENGTH 512

#define XCANET_OSSL_OK 0
#define XCANET_OSSL_ERROR_INVALID_ARGUMENT 1
#define XCANET_OSSL_ERROR_OPENSSL 2
#define XCANET_OSSL_ERROR_UNSUPPORTED 3
#define XCANET_OSSL_ERROR_INTERNAL 4

#define XCANET_OSSL_CAP_SIGN_CSR 0x00000001u

typedef struct xcanet_error
{
    int code;
    char message[XCANET_OSSL_ERROR_MESSAGE_LENGTH];
    char detail[XCANET_OSSL_ERROR_DETAIL_LENGTH];
} xcanet_error;

typedef struct xcanet_capabilities
{
    unsigned int flags;
    int supports_sign_csr;
} xcanet_capabilities;

typedef struct xcanet_buffer
{
    unsigned char* data;
    int len;
} xcanet_buffer;

int xcanet_ossl_is_available(void);
int xcanet_ossl_get_version(char* buffer, int buffer_len);
int xcanet_ossl_get_capabilities(xcanet_capabilities* out_caps);
int xcanet_ossl_self_test(xcanet_error* err);
int xcanet_ossl_sign_csr(
    const unsigned char* csr_der,
    int csr_der_len,
    const unsigned char* issuer_cert_der,
    int issuer_cert_der_len,
    const unsigned char* issuer_key_pkcs8_der,
    int issuer_key_pkcs8_der_len,
    int validity_days,
    xcanet_buffer* out_cert_der,
    xcanet_error* err);
void xcanet_ossl_free_buffer(xcanet_buffer* buffer);

#ifdef __cplusplus
}
#endif

#endif
