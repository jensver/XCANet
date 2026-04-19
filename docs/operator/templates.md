# Template Basics

Templates are workflow helpers, not a full policy engine.

They are designed to reduce repetitive entry when creating:

- self-signed CA certificates
- CSRs
- certificates issued from CSRs

## Template fields

Templates can define defaults for:

- name and description
- enabled / disabled state
- favorite flag
- intended usage
- subject defaults
- SAN defaults
- key algorithm defaults
- RSA key size or EC curve defaults
- signature algorithm defaults
- validity defaults
- CA/basic constraints
- key usage and enhanced key usage

## Template lifecycle

From the `Templates` page you can:

- create
- edit
- clone
- favorite / unfavorite
- enable / disable
- delete

Disabled templates remain visible but cannot be applied to workflows.

## Using templates in workflows

- `Private Keys`:
  - apply a template before `Create Self-Signed CA`
  - apply a template before `Create CSR`
- `CSRs`:
  - apply a template before `Sign CSR`

Templates pre-populate the current workflow. They do not create a separate issuance engine.

## Validation behavior

XcaNet blocks obvious template problems such as:

- CA template without CA/basic constraints
- end-entity or CSR template incorrectly marked as a CA
- unsupported key usage / EKU values
- invalid validity values
- incompatible workflow use
- disabled template use

Validation errors are intended to be operator-readable and to fail early before issuance.
