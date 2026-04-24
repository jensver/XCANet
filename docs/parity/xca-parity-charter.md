# XCA Parity Charter

## Status

This document resets the product target for XcaNet.

XcaNet already has a credible PKI workbench foundation, but the current application is **not yet XCA parity**. `v0.1.0` must be treated as a foundation release, not as evidence of parity with upstream XCA.

Upstream XCA is the reference product for the parity work described here.

## Product Contract

The product target is no longer "a clean .NET PKI workbench with modern UI". The target is:

- XCA-equivalent **functional parity** for the core operator workflows users expect
- XCA-equivalent **workflow parity** for how those tasks are started, transformed, and completed
- XCA-equivalent **screen and layout parity** for the primary workspaces, lists, dialogs, and inspection surfaces

UI parity is not polish. It is part of the product contract. A screen that exposes the same backend capability through a different mental model does **not** satisfy parity.

Modernization is allowed only when an experienced XCA user would still immediately recognize:

- where to go
- what object type they are working with
- which actions are available
- how to move between request, certificate, template, CA, and revocation workflows

## Non-Goals For This Charter

This charter does not:

- change the current application architecture
- require a literal visual clone of Qt-era XCA chrome
- require shipping new product code as part of the charter itself
- redefine `v0.1.0` as parity-complete

## Required Parity Layers

### 1. Functional Parity

XcaNet must expose the same basic object capabilities and PKI operations that a normal XCA operator expects in daily usage for:

- private keys
- certificates
- certificate requests
- templates
- CRLs and revocation
- CA-centric signing and properties workflows
- import and export of common object forms

Functional parity is incomplete if a supported object exists in storage or contracts but cannot be operated on through an XCA-like UI flow.

### 2. Workflow Parity

XcaNet must preserve the core XCA workflow mental model:

- object-centric tabs or workspaces
- table-first browsing before detail inspection
- CA-centric issuance, revocation, and CRL actions
- request to certificate and template to object transforms
- one central certificate/request/template input dialog rather than unrelated per-screen forms
- obvious context actions from the currently selected object

Workflow parity is incomplete if the feature exists but requires a different navigation strategy than an XCA user would expect.

### 3. Screen And Layout Parity

The primary screen structure must map closely enough to XCA that users recognize the application from the layout alone:

- object workspace organization
- object list/table emphasis
- chain/tree inspection where XCA uses relationship views
- shared input surfaces for certificate, request, and template editing
- action placement that follows object selection and CA context

Screen parity is incomplete if the same operations exist behind dashboards, wizard-heavy flows, or layout patterns that hide the object/workspace model.

## Parity Review Rules

A PR that claims XCA progress must state which of the three parity layers it advances.

A feature is not parity-complete unless all of the following are true:

- the object exists and is usable end to end
- the workflow matches the XCA mental model
- the screen layout presents the object in an XCA-recognizable way

Backend capability alone is not acceptance evidence.

## Screen Parity Matrix

| Screen area | Required parity anchors | Acceptance criteria |
| --- | --- | --- |
| Database / workspace | Persistent workspace concept, explicit open/create/unlock lifecycle, object work begins from a selected database/workspace rather than an abstract shell state | Operators can create, open, unlock, lock, and recognize the active workspace before performing PKI actions. Workspace state is visible and does not compete with object workspaces for primary attention. |
| Private keys | Dedicated object workspace, table-first key list, key-specific context actions, linkage to related certificates/requests | Private keys are browsed in a dedicated list with sortable identity columns and selection-driven actions. A selected key clearly exposes create request, create certificate, import/export, and related-object inspection paths. |
| Certificates | Dedicated certificate workspace, table-first certificate list, visible CA vs leaf distinction, relationship-aware inspection | Certificates are browsed in a dedicated list with recognizable XCA-style columns. A selected certificate exposes inspect, export, renew or transform, revoke, and related issuer/child navigation from the same workspace. |
| Certificate requests | Dedicated request workspace, request list, transform path from request to signed certificate, request inspection | Requests are first-class objects, not a side form. A selected request can be inspected, exported, and signed through a CA-centric workflow without leaving the object mental model. |
| Templates | Dedicated template workspace, template list, template usage modes, template-to-object application flows | Templates can be browsed, edited, cloned, enabled or disabled, and applied in the same usage modes XCA users expect. The UI makes it clear whether a template is being used for request creation, certificate issuance, or CA creation defaults. |
| CRLs / revocation | Revocation status attached to certificate workflows, CA-scoped revocation actions, CRL object workspace | Revocation starts from selected certificates in CA context, not from an isolated utility flow. Generated CRLs appear in their own object workspace with issuer, update, and entry information visible from the list and detail surfaces. |
| Central certificate/request/template input dialog | One shared mental model for certificate, request, and template authoring with consistent field grouping | The app provides a central input dialog or equivalent shared editing surface for certificate, request, and template fields. Subject, SAN, validity, key usage, EKU, CA/basic constraints, and signing inputs are grouped consistently across modes. |
| CA properties / signing flows | CA-centric actions, clear issuer selection, signing properties in CA context, revocation and CRL management from CA workflows | Issuance and revocation flows begin from or clearly identify the acting CA. Operators can inspect CA properties and use that CA for signing, renewal-related issuance, revocation, and CRL generation without switching to unrelated screens. |
| Common object tables, columns, and context actions | Table-first browsing, recognizable default columns, selection-driven actions, context menus or equivalent | Every major object type exposes a primary table with stable default columns, sortable selection, and context actions that mirror common XCA object operations. Actions are available from the selected row/object, not hidden behind generic global commands. |
| Certificate chain / tree view | Relationship-oriented chain visualization for issuer and child relationships | Certificate inspection includes a chain or tree view that lets operators understand issuer hierarchy and navigate related certificates directly from the certificate workspace. |

## Detailed Acceptance Criteria By Area

### Database / Workspace

- The active database or workspace is always visible.
- Operators can tell whether the workspace is locked or unlocked without opening a secondary diagnostics view.
- Entering a workspace leads naturally into object workspaces rather than a dashboard-only interaction model.

### Private Keys

- The private key screen is a first-class workspace, not a supporting utility page.
- The primary interaction starts with a selectable table.
- Key actions are specific to the selected key and include request/certificate creation where applicable.

### Certificates

- The certificate screen is a first-class workspace with a dense list/table as the primary browsing surface.
- CA certificates, intermediates, and end-entity certificates are visually distinguishable.
- A selected certificate exposes chain-aware inspection and CA-related actions from the same workspace.

### Certificate Requests

- Requests appear in their own workspace and are not hidden inside certificate creation only.
- A selected request can be signed into a certificate through a CA-driven workflow.
- Request inspection uses the same object-first model as certificates and keys.

### Templates

- Templates remain their own object type with table-first browsing.
- Template application modes are explicit and map to XCA-style usage patterns.
- Template editing uses the shared certificate/request/template input model instead of a disconnected screen-specific form.

### CRLs / Revocation

- Revocation is triggered from certificate context with an explicit acting CA.
- Revocation state is visible from certificate browsing and inspection.
- CRLs are browseable as stored objects with issuer and update metadata.

### Central Input Dialog

- Certificate, request, and template editing share the same field taxonomy and grouping.
- The same operator can move between those modes without relearning the form.
- The dialog supports XCA-like transformations rather than separate ad hoc forms for each object type.

### CA Properties / Signing

- The acting CA is always explicit during signing, revocation, and CRL generation.
- CA-specific properties are inspectable without leaving the CA/certificate mental model.
- Signing flows preserve the sense that a CA operates on requests and certificates.

### Common Tables / Context Actions

- Each object workspace has stable default columns suitable for daily operator use.
- Context actions are reachable from the selected object and match the object type.
- Basic object operations do not require hunting through unrelated screens or modal-only flows.

### Chain / Tree View

- Certificate relationships are navigable as a hierarchy, not only as flat metadata links.
- The chain view is part of certificate work, not a hidden developer-style inspector.
- Child certificates and issuer relationships can be traversed without manual searching.

## Roadmap Reset

The roadmap after the current foundation work is reset as follows.

### M13: Certificate Input, Template Application, And Screen Parity

Scope:

- establish the central certificate/request/template input dialog as a shared product surface
- align private key, certificate, request, and template screens to table-first XCA-recognizable layouts
- make template application modes explicit in the UI and workflow model
- normalize common object columns and context actions

Exit criteria:

- the shared input dialog exists as the primary authoring surface for certificate, request, and template workflows
- the core object screens no longer read as generic pages; they read as XCA-like workspaces
- template application is explicit, reviewable, and consistent across supported object transforms

### M14: Object Workspace, Chain View, And CA Workflow Parity

Scope:

- complete the object-centric workspace model across database, keys, certificates, requests, templates, and CRLs
- add certificate chain/tree navigation as a core inspection surface
- make CA properties, signing, revocation, and CRL generation operate through an XCA-like CA mental model

Exit criteria:

- certificate work includes chain-aware navigation
- CA-driven actions are obvious and coherent from the certificate/request workflows
- revocation and CRL generation feel like CA workflows, not standalone utilities

### M15: Interop, Hardware, Advanced Extension, And Export Parity

Scope:

- close remaining parity gaps in import/export breadth and fidelity
- expand advanced extension authoring and object editing toward XCA-equivalent operator control
- address hardware-token and related interop expectations where needed for practical XCA equivalence
- finish higher-friction parity gaps that remain after M13 and M14

Exit criteria:

- import/export coverage matches the expected XCA operator surface for common workflows
- advanced extension authoring is no longer a major parity gap
- remaining interop and hardware differences are reduced to clearly documented exceptions

## Versioning Guidance

`v0.1.0` is a foundation milestone, not an XCA parity milestone.

It proves that XcaNet has:

- a working cross-platform `.NET` desktop base
- secure local storage
- managed crypto workflows
- optional OpenSSL-assisted CSR signing
- import/export, template, revocation, diagnostics, and packaging foundations

It does **not** prove that XcaNet already matches XCA in:

- screen layout
- workflow model
- object workspace behavior
- CA-centric operator experience
- overall day-to-day usability for an existing XCA user

## Definition Of Done For Future Parity Claims

Do not claim "XCA parity" for a milestone, release note, or PR unless review evidence shows:

- the relevant XCA screen area exists in XcaNet
- its core actions are present
- the workflow order matches the expected XCA mental model
- the screen layout remains recognizable to an XCA user
- any intentional deviations are documented as explicit exceptions, not implied omissions
