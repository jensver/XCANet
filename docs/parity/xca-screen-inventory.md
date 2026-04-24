# XCA Screen Parity Inventory

**Status**: Active reference document — updated per milestone  
**Source of truth**: XCA 2.9.0 (https://github.com/chris2511/xca)  
**Rule**: A screen is not parity-complete until its visible buttons, columns, tabs, dialogs,
and workflows match XCA or have an explicitly documented intentional deviation recorded here.

---

## How to Use This Document

Each section covers one XCA screen area. For each:

- **XCA reference** — what the real XCA screen contains (exact labels, columns, buttons)
- **XcaNet current** — what exists today in XcaNet
- **Gaps** — checklist of missing or wrong items
- **Intentional deviations** — explicit exceptions approved by the project (if any)
- **Priority** — milestone where this screen reaches parity

Do not remove a gap item unless the corresponding UI element is implemented and visually
confirmed to match XCA. Do not mark a screen parity-complete unless all gaps are resolved
or moved to Intentional Deviations with a written justification.

---

## 1. Main Window Shell

### XCA Reference

**Window layout:**
- Menu bar (File | Import | Token | Extra | Help)
- Central area: tab widget with 5 object tabs
- Status bar (bottom)
- Default size: 702 × 511 px (resizable)

**Menu bar — File menu:**
- New DataBase
- Open DataBase
- Open Remote DataBase
- (Recent databases — dynamic list)
- Set default database
- Close DataBase
- Options
- Language Selection (submenu)
- Exit

**Menu bar — Import menu:**
- Keys
- Requests
- Certificates
- PKCS#12
- PKCS#7
- Template
- Revocation list
- PEM files (also paste from clipboard)

**Menu bar — Token menu:**
- Manage Security token
- Init Security token
- Change PIN
- Change SO PIN
- Init PIN

**Menu bar — Extra menu:**
- Database dump
- Certificate index export
- Hierarchy export
- Password change
- DH parameter generation
- OID Resolver

**Menu bar — Help menu:**
- Documentation
- About

**Tab bar:**
- Tab 1: Private Keys
- Tab 2: Certificate Signing Requests
- Tab 3: Certificates (default active)
- Tab 4: Templates
- Tab 5: Revocation Lists

**Status bar:**
- Shows operational status messages

**Toolbar:**
- Not present in base XCA (menu-driven)

### XcaNet Current

File: `src/XcaNet.App/Views/MainWindow.axaml`

- Menu bar present: File | Token | Help
- Object tab bar present: 5 tabs (Private Keys, Certificate Signing Requests, Certificates,
  Templates, Revocation Lists)
- Bottom status bar present with search field
- Content fills edge-to-edge below tab bar
- Authoring dialog rendered as overlay on top of main window

### Gaps

**Menus:**
- [ ] `Import` menu missing entirely (Keys, Requests, Certificates, PKCS#12, PKCS#7,
      Template, Revocation list, PEM files / clipboard)
- [ ] `Extra` menu missing (Database dump, Certificate index export, Hierarchy export,
      Password change, DH parameter generation, OID Resolver)
- [ ] `File → Open Remote DataBase` missing
- [ ] `File → Set default database` missing
- [ ] `File → Close DataBase` missing
- [ ] `File → Options` missing (links to Options/Preferences dialog)
- [ ] `File → Language Selection` missing (low priority, intentional deviation candidate)
- [ ] `Help → Documentation` missing
- [ ] `Help → About` missing
- [ ] `Token → Manage Security token` missing
- [ ] `Token → Init Security token` missing
- [ ] `Token → Change PIN` missing
- [ ] `Token → Change SO PIN` missing
- [ ] `Token → Init PIN` missing

**Shell behavior:**
- [ ] No "Recent databases" list in File menu
- [ ] Search field is in the bottom status bar — XCA has per-tab filter bars (acceptable
      deviation if documented)

### Intentional Deviations

None declared yet.

### Priority

M13.4 (shell menus), M15 (Token menu, remote database, language)

---

## 2. Private Keys Tab

### XCA Reference

**Tab layout:**
- Left panel: tree view of private keys
- Right panel: vertical button stack
- Bottom: 200 × 94 px preview label (key info / icon)

**Right-side buttons (top to bottom):**
1. New
2. Export
3. Import
4. Show Details
5. Delete

**Tree view columns (default):**
- Name (internal database name)
- Algorithm (RSA, DSA, EC, etc.)
- Bit length / Curve
- Use (count of related objects referencing this key)
- Token (security token indicator)

**Context menu (right-click on key):**
- Import
- Export → Clipboard (Ctrl+C) / File (Ctrl+S)
- Rename
- Properties (opens ItemProperties dialog)
- Delete
- Change password (if common password protection)
- Reset password (if private password protection)
- Set own password
- Reset own password
- Security token → Change PIN / Init PIN / Change SO PIN / Export to token

**Double-click:** Opens Show Details (KeyDetail dialog)

**New Key dialog (NewKey.ui):**
- Field: Key Name (default "New Key")
- Dropdown: Key Type (RSA / DSA / EC)
- Dropdown: Key Size (conditional on type)
- Dropdown: Curve Name (conditional on EC type)
- Checkbox: "Remember as default"
- Buttons: OK / Cancel

**Key Detail dialog (KeyDetail.ui) — tabs:**
1. Key — public exponent, key size, private exponent (clickable), modulus
2. Security Token — label, PKCS#11 ID, model, manufacturer, serial, label
3. Fingerprint
4. Comment — plain text editor

### XcaNet Current

File: `src/XcaNet.App/Views/Pages/PrivateKeysPageView.axaml`

**Table columns:** DisplayName, Algorithm, SizeOrCurve, CreatedUtc, RelatedObjectSummary

**Right-side buttons:**
- New Key (→ GenerateKeyCommand)
- Export (→ ExportSelectedToFileCommand)
- Import (disabled)
- Show Details (disabled)
- Delete (disabled)
- Create CSR (→ OpenCertificateSigningRequestAuthoringCommand)
- Create Certificate (→ OpenSelfSignedCaAuthoringCommand)
- Preview Export (→ ExportSelectedCommand)
- Refresh (→ RefreshCommand)

**Inspector panel (bottom):**
- Tabs: Details, New Key, Export
- Details tab: Name, Algorithm, Fingerprint, Created (label/value rows)
- New Key tab: inline name + algorithm + curve + Generate button
- Export tab: format combo, password, PEM preview text box

### Gaps

**Columns:**
- [ ] `Use` column missing (count of certificates/requests referencing this key)
- [ ] `Token` column missing (security token indicator)
- [ ] Column header context menu missing (show/hide, reorder, auto-fit)
- [ ] `Name` in XCA is the "internal database name" — should match XCA semantics

**Buttons / button order:**
- [ ] Button labels and order differ from XCA:
  - XCA: New, Export, Import, Show Details, Delete
  - XcaNet: New Key, Export, Import (disabled), Show Details (disabled), Delete (disabled),
    Create CSR, Create Certificate, Preview Export, Refresh
  - Correct order should match XCA (New, Export, Import, Show Details, Delete) followed by
    key-specific extra actions
- [ ] "New Key" should be labeled "New" (matching XCA convention)

**New Key dialog:**
- [ ] Currently inline in inspector "New Key" tab — XCA launches a separate modal dialog
- [ ] Missing "Key Type" dropdown (XCA: RSA / DSA / EC)
- [ ] Missing "Remember as default" checkbox

**Key Detail dialog:**
- [ ] No standalone Key Detail modal (XCA: KeyDetail.ui with 4 tabs)
- [ ] "Show Details" button disabled — must open a detail dialog
- [ ] Missing tabs: Key (modulus, exponent), Security Token, Fingerprint, Comment
- [ ] `Delete` button disabled — must implement delete with confirmation

**Context menus:**
- [ ] No right-click context menu on key rows at all
- [ ] Missing: Rename, Properties, Delete from context menu
- [ ] Missing: Password operations (Change, Set own, Reset own)
- [ ] Missing: Security token submenu

**Bottom preview panel:**
- [ ] XCA has a 200 × 94 px preview/icon area at bottom of button stack
- [ ] XcaNet has no equivalent — low priority intentional deviation candidate

**Import:**
- [ ] Import button disabled — must implement key import (PEM/DER/PKCS#12)

### Intentional Deviations

None declared yet.

### Priority

M13.5 (columns, button order, New Key dialog, Show Details, Delete), M15 (context menus,
password ops, security token)

---

## 3. Certificate Signing Requests Tab

### XCA Reference

**Tab layout:**
- Left panel: tree view of CSRs
- Right panel: vertical button stack
- Bottom: preview label

**Right-side buttons:**
1. New
2. Export
3. Import
4. Show Details
5. Delete

**Tree view columns (default):**
- Name (internal)
- Subject (DN string)
- Private key (linked key name)
- Signed (yes/no indicator)
- Algorithm

**Context menu (right-click on CSR):**
- Import / Export → Clipboard / File
- Rename
- Properties
- Delete
- Sign (→ opens NewX509 dialog in signing mode)
- Mark signed / Unmark signed
- Similar Request (→ creates a new CSR based on selected)

**Double-click:** Opens Show Details (CSR detail view via NewX509 in read mode)

**New Request workflow:**
- "New" button opens NewX509 dialog in request-creation mode
- Tabs: Source, Subject, Extensions, Key Usage, Netscape, Advanced, Comment

### XcaNet Current

File: `src/XcaNet.App/Views/Pages/CertificateRequestsPageView.axaml`

**Table columns:** DisplayName, Subject, PrivateKeySummary, CreatedUtc, KeyAlgorithm

**Right-side buttons:**
- New Request (disabled)
- Sign (→ OpenIssuanceAuthoringCommand)
- Export (→ ExportSelectedToFileCommand)
- Import (disabled)
- Show Details (disabled)
- Delete (disabled)
- Similar Request (→ CreateSimilarRequestCommand)
- To Template (→ CreateTemplateFromRequestCommand)
- Open Key (→ OpenSelectedPrivateKeyCommand)
- Refresh (→ RefreshCommand)

**Inspector panel (bottom):**
- Tabs: Details, Export
- Details: Name, Subject, Alternative names, Algorithm
- Export: format combo, preview text box

### Gaps

**Columns:**
- [ ] `Signed` column missing (shows whether CSR has been signed into a certificate)
- [ ] Column header context menu missing

**Buttons:**
- [ ] "New Request" disabled — must implement CSR creation via NewX509 dialog
- [ ] "New" label should match XCA ("New" not "New Request")
- [ ] Import disabled — must implement PKCS#10 import
- [ ] Show Details disabled — must open detail view (read-only NewX509 tabs or equivalent)
- [ ] Delete disabled — must implement with confirmation

**Context menus:**
- [ ] No right-click context menu on rows
- [ ] Missing: Sign, Mark signed / Unmark signed, Similar Request, Rename, Properties,
      Delete from context menu

**Sign workflow:**
- [ ] "Sign" button opens CertificateAuthoringSurfaceView overlay — acceptable, but the
      NewX509 Source tab must clearly identify the CSR being signed

**Detail view:**
- [ ] No standalone CSR detail dialog — Show Details disabled
- [ ] XCA shows CSR content in NewX509 dialog in read mode (or separate view)

### Intentional Deviations

None declared yet.

### Priority

M13.5 (columns, New Request, Show Details, Delete, context menu basics), M14 (Sign
workflow completeness)

---

## 4. Certificates Tab

### XCA Reference

**Tab layout:**
- Left panel: hierarchical tree view (chains are grouped)
- Right panel: vertical button stack
- Bottom: preview label

**Right-side buttons:**
1. New
2. Export
3. Import
4. Show Details
5. Delete
6. Import PKCS#12
7. Import PKCS#7
8. Plain View (toggle — switches between tree/flat view)

**Tree view columns (default, in hierarchy/tree mode):**
- Name (internal)
- Common Name
- Status (trust / expiry / revocation indicator — color coded)
- Not Before
- Not After
- Serial number
- Fingerprint (MD5 or SHA1)

**Hierarchy display:**
- Certificates are grouped under their signing CA
- Root CAs at top level
- Issued certificates indented under issuer
- "Plain View" toggle switches to flat list

**Context menu (right-click on certificate):**
- Import / Export → Clipboard / File
- Rename
- Properties
- Delete
- Convert to request
- Export to token
- Create similar certificate
- Generate CRL (if CA certificate)
- Manage revocations (if CA certificate)
- CA Properties (if CA certificate)
- Certificate renewal
- Revoke
- Unrevoke

**Double-click:** Opens Certificate Detail dialog (CertDetail.ui)

**Certificate Detail dialog (CertDetail.ui) — 6 tabs:**
1. Status — name, signature algorithm, key reference, serial, MD5/SHA1/SHA256 fingerprints,
   Not before, Not after
2. Subject — distinguished name components with OID tooltips
3. Issuer — distinguished name of issuer
4. Attributes — certificate attributes
5. Extensions — v3 extensions (raw text), "Show config" button
6. Comment — plain text editor

**Certificate Renewal dialog (CertExtend.ui):**
- Not before / Not after date pickers
- Duration spinner + Days/Months/Years dropdown + Apply button
- Checkboxes: Midnight, Local time, No well-defined expiration
- Checkboxes: Revoke old certificate, Replace old certificate, Keep serial number

### XcaNet Current

File: `src/XcaNet.App/Views/Pages/CertificatesPageView.axaml`

**Table columns:** DisplayName, Subject, Issuer, Serial, NotBefore, NotAfter,
CertificateKind, RevocationStatus, PrivateKeyStatus

**Filter row:** Name, Subject, Issuer, Serial text boxes + Refresh button (at top of pane)

**Right-side buttons:**
- New Certificate (disabled)
- Export (→ ExportSelectedCommand)
- Import (→ ImportFilesCommand)
- Show Details (disabled)
- Delete (disabled)
- Revoke (→ RevokeSelectedCommand)
- Generate CRL (→ GenerateCertificateRevocationListCommand)
- To Template (→ CreateTemplateFromCertificateCommand)
- Plain View (disabled)
- Open Issuer (→ OpenIssuerCommand)

**Inspector panel (bottom):**
- Tabs: General, Details, Extensions, Relationships, Raw / PEM
- General: DisplayName, Subject, Issuer, Validity, Private key
- Details: Serial, SHA-1, SHA-256, Key algorithm
- Extensions: Subject Alternative Names list, Key Usage list, EKU list
- Relationships: Type, Issuer display name, Children combobox, Open Child button
- Raw/PEM: export target/format combos, password field, text box for PEM output

### Gaps

**Columns:**
- [ ] `Common Name` column missing (separate from full Subject)
- [ ] `Status` column missing (trust/expiry/revocation with color coding)
- [ ] `Fingerprint` column missing (MD5 or SHA1 for quick identification)
- [ ] Column header context menu missing

**Hierarchy / tree view:**
- [ ] Flat list only — no certificate hierarchy tree grouping CAs with their issued certs
- [ ] No "Plain View" toggle to switch between tree and flat list (button present but disabled)
- [ ] XCA color-codes expired certificates in the list — XcaNet has no color coding

**Buttons:**
- [ ] "New Certificate" disabled — must implement (opens NewX509 dialog in cert-creation mode)
- [ ] "Show Details" disabled — must open CertDetail dialog (6 tabs)
- [ ] "Delete" disabled — must implement with confirmation

**Certificate Detail dialog:**
- [ ] No standalone Certificate Detail dialog (CertDetail.ui equivalent)
- [ ] Current inspector tabs partially cover this but are not the same as a detail modal
- [ ] Missing: MD5 fingerprint display
- [ ] Missing: Issuer DN tab (separate from issuer name reference)
- [ ] Missing: Attributes tab
- [ ] Missing: Comment tab on certificate
- [ ] Missing: "Show config" button in Extensions view

**Certificate Renewal dialog:**
- [ ] No renewal dialog (CertExtend.ui equivalent)
- [ ] Context menu "Certificate renewal" missing

**Revocation:**
- [ ] Revoke button calls RevokeSelectedCommand — XCA opens a Revoke dialog with serial,
      date, and reason inputs — XcaNet must show equivalent before committing
- [ ] "Unrevoke" not available from button or context menu

**Context menus:**
- [ ] No right-click context menu on rows
- [ ] Missing: Convert to request, Export to token, Create similar certificate,
      Generate CRL, Manage revocations, CA Properties, Certificate renewal, Revoke,
      Unrevoke, Rename, Properties, Delete

**Import:**
- [ ] Import button present but covers only file import
- [ ] Missing: explicit PKCS#12 import button
- [ ] Missing: PKCS#7 import button

### Intentional Deviations

None declared yet.

### Priority

M13.6 (hierarchy tree, Show Details, New Certificate, color coding, column fixes),
M14 (renewal dialog, revoke dialog, CA context menus, CA Properties)

---

## 5. Templates Tab

### XCA Reference

**Tab layout:**
- Left panel: tree view of templates
- Right panel: vertical button stack
- Bottom: preview label

**Right-side buttons:**
1. New
2. Export
3. Import
4. Show Details
5. Delete

**Tree view columns:**
- Name (template identifier)
- (Templates have minimal column data — they are named configurations)

**Context menu (right-click on template):**
- Import / Export → Clipboard / File
- Rename
- Properties
- Delete
- Duplicate (creates copy with " copy" appended to name)
- Create certificate (opens NewX509 with template pre-applied)
- Create request (opens NewX509 with template pre-applied in request mode)

**Template Edit dialog:**
- Accessed via Show Details / double-click
- Integrated into NewX509 dialog (same tab structure) — template editing uses the shared
  certificate/request input surface
- Additional: Name, insertion date, comment fields

### XcaNet Current

File: `src/XcaNet.App/Views/Pages/TemplatesPageView.axaml`

**Table columns:** Name, Usage, Enabled, Favorite, SubjectPreset, ExtensionPreset

**Filter row:** Usage filter combo, Status filter combo, Refresh button

**Right-side buttons:**
- New Template (→ CreateNewCommand)
- Edit (→ EditTemplateCommand)
- Clone (→ CloneTemplateCommand)
- Delete (→ DeleteTemplateCommand)
- Enable/Disable (→ ToggleEnabledCommand)
- Favorite (→ ToggleFavoriteCommand)
- Export/Import (disabled)

**Inspector panel (bottom):**
- Tabs: General, Details, Validation
- General: Name, Usage, State, Summary (label/value rows)
- Details: SubjectPreset + ExtensionPreset text boxes
- Validation: validation summary text box

### Gaps

**Columns:**
- [ ] XCA has a single "Name" column — XcaNet extensions (Usage, Enabled, Favorite) are
      acceptable additions but not present in XCA; document as intentional extension
- [ ] Column header context menu missing

**Buttons:**
- [ ] Button order / labels differ from XCA (New, Export, Import, Show Details, Delete)
- [ ] "Edit" should map to "Show Details" in XCA semantics
- [ ] Export / Import disabled — must implement template export/import

**Template editing surface:**
- [ ] XCA edits templates using the full NewX509 dialog tabs (Source, Subject, Extensions,
      Key Usage, Netscape, Advanced, Comment) — XcaNet's TemplateAuthoringDialogView
      provides the shared CertificateAuthoringSurfaceView but displayed differently
- [ ] Verify that XcaNet's template editing surface covers all NewX509 tabs

**Context menus:**
- [ ] No right-click context menu on rows
- [ ] Missing: Duplicate (XcaNet has Clone — same intent, label differs)
- [ ] Missing: Create certificate from template
- [ ] Missing: Create request from template
- [ ] Missing: Rename, Properties, Delete from context menu

**Template Detail / Properties:**
- [ ] No equivalent to ItemProperties.ui modal (Name, Source, Insertion date, Comment)
- [ ] Comment field not present on templates in XcaNet

### Intentional Deviations

- **Usage, Enabled, Favorite columns**: XcaNet-specific extensions not present in XCA.
  Retained as they improve template management UX. Must be documented as additions, not
  replacements for XCA columns.
- **Clone vs Duplicate**: Same intent. Label "Clone" is acceptable.

### Priority

M13.7 (button order, export/import, context menus, Comment field), M14 (template
application from context menu — Create cert/request from template)

---

## 6. Revocation Lists Tab

### XCA Reference

**Tab layout:**
- Left panel: tree view of CRLs
- Right panel: vertical button stack
- Bottom: preview label

**Right-side buttons:**
1. New
2. Export
3. Import
4. Show Details
5. Delete

**Tree view columns:**
- Name (internal)
- Issuer (CA name)
- Last update
- Next update
- Revoked (count of revoked entries)

**New CRL workflow:**
- If no CA certificates: informational message
- If one CA: auto-selects it
- If multiple CAs: itemComboCert dialog for selection
- Then opens NewCrl.ui dialog

**NewCrl dialog fields:**
- Last update (date/time picker)
- Next update (date/time picker)
- Duration spinner + Days/Months/Years + Apply
- Checkboxes: Midnight, Local time
- Hash algorithm dropdown
- Checkboxes: Subject alternative name, Authority key identifier, CRL number (+ text field),
  Revocation reasons

**CRL Detail dialog (CrlDetail.ui) — 5 tabs:**
1. Status — name, signed by, signature algorithm, version, last update, next update
2. Issuer — DN widget
3. Extensions — v3 extensions raw text
4. Revocation list — tree widget of revoked entries
5. Comment — plain text editor

**Context menu:**
- Show Details
- Export → Clipboard / File
- Import
- Delete

### XcaNet Current

File: `src/XcaNet.App/Views/Pages/CertificateRevocationListsPageView.axaml`

**Table columns:** DisplayName, CrlNumber, IssuerDisplayName, ThisUpdate, NextUpdateUtc,
RevokedEntryCount

**Right-side buttons:**
- Generate CRL (disabled)
- Export (→ ExportSelectedCommand)
- Import (disabled)
- Show Details (disabled)
- Delete (disabled)
- Open Issuer (→ OpenIssuerCommand)
- Refresh (→ RefreshCommand)

**Inspector panel (bottom):**
- Tabs: Details, Revoked entries
- Details: Issuer, CRL number, This update, Next update
- Revoked entries: list of revoked entries (DisplayName, SerialNumber, Reason, RevokedAt)

### Gaps

**Columns:**
- [ ] `CRL number` is present in XcaNet but not a default XCA column — acceptable addition
- [ ] Column header context menu missing

**Buttons:**
- [ ] "Generate CRL" disabled — must implement (selects CA, opens NewCrl dialog)
- [ ] Import disabled — must implement CRL import
- [ ] Show Details disabled — must open CrlDetail dialog (5 tabs)
- [ ] Delete disabled — must implement with confirmation
- [ ] "Generate CRL" in XcaNet is on this tab — in XCA it is accessed from the
      Certificates tab context menu on a CA certificate AND the CRL tab New button

**New CRL dialog:**
- [ ] No NewCrl dialog (NewCrl.ui equivalent)
- [ ] Missing: CA selection when multiple CAs present
- [ ] Missing: Last/Next update date pickers
- [ ] Missing: Hash algorithm selection
- [ ] Missing: Extension checkboxes (SAN, AKI, CRL number, Revocation reasons)

**CRL Detail dialog:**
- [ ] No standalone CRL Detail modal (CrlDetail.ui equivalent)
- [ ] Inspector covers partial content but is not modal and lacks Issuer DN tab,
      Extensions tab, Comment tab

**Context menus:**
- [ ] No right-click context menu on rows

### Intentional Deviations

None declared yet.

### Priority

M14 (Generate CRL dialog, Show Details, Delete, Import, context menus)

---

## 7. Certificate / Request Input Dialog (NewX509 Equivalent)

### XCA Reference

This is the single most important dialog in XCA. It is used for:
- Creating a new certificate (self-signed or CA-signed)
- Creating a new certificate signing request
- Editing/viewing a template
- Signing a CSR into a certificate

**Dialog tabs (some conditional):**

1. **Source** — always present
   - Certificate signing mode: Self-signed / Sign with CA / Use existing request
   - CSR selection with extension copy options
   - Signature algorithm dropdown
   - Template selector (with application mode: Replace / Merge / Don't apply)
   - Validity section (Not before, Not after, duration)

2. **Subject** — shown unless using a CSR without modification
   - Internal name field
   - Distinguished Name widget (multi-field: CN, O, OU, C, ST, L, email, etc.)
   - Custom attribute support
   - Private key dropdown (itemComboKey — selects existing key)
   - Option to generate new key

3. **Extensions** — always present
   - Basic Constraints: CA checkbox, Path length spinner
   - Subject key identifier checkbox
   - Authority key identifier checkbox
   - Validity (Not before / Not after — alternative to Source tab placement)
   - Subject Alternative Names (SAN)
   - CRL Distribution Points
   - Authority Information Access
   - OCSP stapling checkbox

4. **Key Usage** — always present
   - Key Usage flags (checkboxes): Digital signature, Key encipherment, Data encipherment,
     Key agreement, Key cert sign, CRL sign, Encipher only, Decipher only
   - Critical flag for Key Usage
   - Extended Key Usage selections (multi-select list)
   - Critical flag for EKU

5. **Netscape** — conditional (hidden if `disable_netscape` setting active)
   - Netscape certificate types selector
   - URL: revocation
   - URL: renewal
   - CA policy references
   - SSL server name field

6. **Advanced** — always present
   - Direct OpenSSL config syntax editor
   - Validate button

7. **Comment** — always present
   - Plain text editor for user notes

**Dialog buttons:**
- OK
- Cancel

### XcaNet Current

File: `src/XcaNet.App/Views/Shared/CertificateAuthoringSurfaceView.axaml`

**Tabs:**
1. Source — Operation summary, Source summary, Template selector (with application mode),
   Validity days
2. Subject — Name, Subject DN (single text box), Alternative names (text box)
3. Extensions — CA checkbox, Path length (Constrained checkbox + spinner), Signature
   algorithm (conditional), Key algorithm section (type + RSA size + EC curve)
4. Key usage — Key usage (text box, multi-line), Extended key usage (text box, multi-line)
5. Advanced — Raw key usage (text box), Raw extended usage (text box)
6. Signing / Issuer — Issuer certificate combo, Issuer key combo (conditional on mode)

**Bottom action bar:**
- Title text block, Cancel button, Primary action button

### Gaps

**Source tab:**
- [ ] No explicit mode radio buttons (Self-signed / Sign with CA / Use existing request) —
      XcaNet infers mode from authoring kind but does not present this choice visually
- [ ] Signature algorithm dropdown missing
- [ ] Validity should show Not before + Not after date pickers, not just "Validity days"
      (XCA uses date pickers with duration shortcut)
- [ ] Template application mode dropdown present (good) — labels should match XCA:
      Replace / Merge / Don't apply

**Subject tab:**
- [ ] Subject DN is a single free-form text box — XCA uses a structured DN widget with
      individual fields per OID component (CN, O, OU, C, ST, L, emailAddress)
- [ ] Private key selector missing — XCA has a key dropdown on Subject tab
- [ ] "Generate new key" option missing (generates key inline during cert/request creation)

**Extensions tab:**
- [ ] Extensions tab mixes key algorithm (key generation) with certificate extensions
- [ ] SAN editor is on the Subject tab (text box) — XCA has SAN on Extensions tab with
      structured multi-value editor
- [ ] CRL Distribution Points field missing
- [ ] Authority Information Access field missing
- [ ] OCSP stapling checkbox missing
- [ ] Subject key identifier checkbox missing
- [ ] Authority key identifier checkbox missing

**Key Usage tab:**
- [ ] Key Usage is a free-form text box — XCA uses individual checkboxes per flag
- [ ] EKU is a free-form text box — XCA uses a multi-select list
- [ ] Critical flag checkboxes missing for both Key Usage and EKU

**Netscape tab:**
- [ ] Entire Netscape tab missing (intentional deviation candidate — legacy)

**Comment tab:**
- [ ] Comment tab missing entirely — no user notes field on certificates/requests

**Key algorithm in dialog:**
- [ ] Key algorithm section on Extensions tab is misplaced — should be on Subject tab
      or Source tab near the private key selector

**Validity:**
- [ ] Only "Validity days" NumericUpDown — must add Not before + Not after date pickers

### Intentional Deviations

- **Netscape tab**: Netscape extensions are deprecated. XcaNet may omit by default,
  matching XCA's `disable_netscape` option. **Must be documented as an explicit omission.**

### Priority

M13.7 (structured DN widget, private key selector, validity date pickers, Key Usage
checkboxes, EKU multi-select, Critical flags, Comment tab), M14 (SAN editor, CDP, AIA,
key identifier checkboxes)

---

## 8. Template Input / Edit Dialog

### XCA Reference

Template editing in XCA reuses the NewX509 dialog with the same 7 tabs (Source, Subject,
Extensions, Key Usage, Netscape, Advanced, Comment). The template captures the certificate
configuration that will be applied when creating certificates or requests from it.

Additional template-level fields (shown in ItemProperties.ui and the properties header):
- Name (editable)
- Source (read-only: how template was created)
- Insertion date (read-only)
- Comment (plain text)

### XcaNet Current

File: `src/XcaNet.App/Views/Shared/TemplateAuthoringDialogView.axaml`

- Top row: Name text box, IntendedUsage combo, Enabled checkbox, Favorite checkbox
- Main area: CertificateAuthoringSurfaceView (left) + Summary/Validation tabs (right)
- Bottom row: Description text box, Clone, Delete, Save Template buttons

### Gaps

- [ ] All gaps from §7 (NewX509 tabs) apply here too
- [ ] "Source" (creation origin) read-only field missing
- [ ] "Insertion date" read-only field missing
- [ ] Comment tab in CertificateAuthoringSurfaceView missing (also listed in §7)
- [ ] Usage / Enabled / Favorite are XcaNet additions — acceptable but document as such
- [ ] Bottom action bar: XCA uses OK/Cancel — XcaNet uses Save/Clone/Delete with
      Description field; acceptable deviation for the inline editing model

### Intentional Deviations

- **Enabled, Favorite, IntendedUsage**: XcaNet additions not in XCA. Retained.
- **Inline editing model**: XcaNet shows template editing in a panel alongside a preview
  rather than as a modal. Acceptable if the fields match.

### Priority

M13.7 (align with NewX509 tab fixes), M14 (Source, Insertion date, Comment)

---

## 9. CA Properties Dialog

### XCA Reference

**CaProperties.ui — single dialog with 2 fields:**
- Days until next CRL issuing (spinner)
- Default template (itemComboTemp dropdown — selects a template)

Accessed via: Certificates tab → right-click CA certificate → CA Properties

### XcaNet Current

No CA Properties dialog exists. No equivalent accessible from the UI.

### Gaps

- [ ] CA Properties dialog missing entirely
- [ ] No access point: right-click context menu on CA certificate missing
- [ ] Days until next CRL issuing setting missing
- [ ] Default template assignment per CA missing

### Intentional Deviations

None declared yet.

### Priority

M14 (CA Properties dialog, CA context menu)

---

## 10. Certificate Revocation Dialog

### XCA Reference

**Revoke.ui — single dialog:**
- Title: "Certificate revocation" (Arial 14pt)
- Serial field (read-only, shows certificate serial number)
- Invalid Since (date/time picker)
- Local Time checkbox
- Revocation Reason dropdown (unspecified, keyCompromise, CACompromise, affiliationChanged,
  superseded, cessationOfOperation, certificateHold, removeFromCRL, privilegeWithdrawn,
  AACompromise)

**Manage Revocations dialog (RevocationList.ui):**
- Tree widget of revoked certificates (multi-select)
- Right-side buttons: Add, Delete, Edit
- OK / Cancel

Accessed via: Certificates tab → right-click CA certificate → Manage revocations

### XcaNet Current

- `RevokeSelectedCommand` exists on Certificates page but directly revokes without a
  confirmation/reason dialog
- No Manage Revocations dialog

### Gaps

- [ ] Revoke dialog missing — must show serial, date picker, reason dropdown before committing
- [ ] Revocation reason dropdown missing (must use RFC 5280 reasons)
- [ ] Manage Revocations dialog missing (add/delete/edit revocation entries for a CA)
- [ ] Unrevoke action missing entirely

### Intentional Deviations

None declared yet.

### Priority

M14 (Revoke dialog with reason, Manage Revocations, Unrevoke)

---

## 11. Database Open / Create / Change Password Flows

### XCA Reference

**New Database:**
- File → New DataBase → file save dialog → creates new .xdb file
- Password set via PwDialog (two fields: new password, confirm)
- Hex checkbox option for binary passwords

**Open Local Database:**
- File → Open DataBase → file open dialog → selects .xdb
- If password-protected: PwDialog appears (one field: enter password)

**Open Remote Database (OpenDb.ui):**
- File → Open Remote DataBase
- Fields: database type, hostname, username, password, database name, table prefix
- Types: SQLite, MySQL/MariaDB, PostgreSQL, Microsoft SQL Server

**Change Password:**
- Extra → Password change → PwDialog (two fields: new password, confirm)

**PwDialog fields:**
- Password field A (masked)
- Confirmation field B (masked)
- Hex checkbox (interpret as hexadecimal)

### XcaNet Current

- File → Create Database (→ CreateDatabaseCommand)
- File → Open Database (→ OpenDatabaseCommand)
- File → Unlock (→ UnlockDatabaseCommand)
- File → Lock (→ LockDatabaseCommand)

### Gaps

- [ ] Password dialog UI not visible / inspectable — unclear if it shows the XCA-style
      two-field confirmation on creation vs one-field on open
- [ ] Hex password checkbox missing
- [ ] Open Remote Database missing
- [ ] Change Password missing (Extra menu missing entirely)
- [ ] "Set default database" missing
- [ ] Recent databases list missing
- [ ] Close Database missing

### Intentional Deviations

- **Remote database**: Out of scope until M15. XcaNet uses a local SQLite model.

### Priority

M13.4 (Close Database, proper PwDialog on create), M15 (remote database, Change Password)

---

## 12. Import / Export Dialogs

### XCA Reference

**Import Multi dialog (ImportMulti.ui):**
- Left: list of importable items
- Right buttons: Import All, Import (selected), Done, Remove from list, Details,
  Delete from token, Rename on token
- Bottom: slot information display
- Used for bulk import from file or token

**Export dialog (ExportDialog.ui):**
- Name field (read-only — internal DB name)
- Filename field + browse ("...") button
- Format dropdown
- Checkbox: "Export comment into PEM file"
- Info text area

**Import menu triggers (per type):**
- Keys → PEM/DER key import
- Requests → PKCS#10 import
- Certificates → PEM/DER/PKCS#12/PKCS#7 import
- PKCS#12 → combined key+cert bundle
- PKCS#7 → certificate chain
- Template → template file
- Revocation list → CRL file
- PEM files → auto-detect type, includes clipboard paste

### XcaNet Current

- Import button on Certificates page calls ImportFilesCommand (file dialog, minimal UI)
- Export on various pages calls export commands
- No ImportMulti dialog
- No ExportDialog modal with format selection and comment option
- Import menu entirely missing from menu bar

### Gaps

- [ ] Import menu missing from menu bar (see §1 — Shell)
- [ ] No ImportMulti dialog
- [ ] No ExportDialog modal (format + filename + comment option)
- [ ] Clipboard import/paste missing
- [ ] Per-type import commands missing from Import menu
- [ ] "Export comment into PEM file" option missing
- [ ] Import from security token missing

### Intentional Deviations

- **Security token import**: Out of scope until hardware token support added (M15).

### Priority

M13.5 (per-type import from menu, Export dialog modal), M14 (Import All / ImportMulti
for bulk), M15 (token import/export)

---

## 13. Options / Preferences Dialog

### XCA Reference

**Options.ui — 3 tabs:**

**Tab 1: Settings**
- Hash algorithm dropdown (default: SHA-256)
- String types configuration
- Suppress success messages checkbox
- Disable legacy Netscape extensions checkbox
- Translate X.509 terminology checkbox
- Restrict token hash support checkbox
- Disable colorization of expired certificates checkbox
- Certificate expiry warning threshold (spinner, days)
- Calendar reminder threshold (spinner, days)
- Serial number length (8–256 bits, default 64)

**Tab 2: Distinguished Name**
- Mandatory Subject Entries list
- Explicit Subject Entries list (drag-and-drop reorder, Add/Remove buttons)
- "Dynamically arrange explicit entries" checkbox

**Tab 3: PKCS#11 Provider**
- Draggable list of PKCS#11 provider libraries
- Add, Remove, Search buttons

Accessed via: File → Options

### XcaNet Current

- No Options/Preferences dialog
- `SettingsSecurityCommand` in ShellViewModel — unclear if it covers any of the above

### Gaps

- [ ] Options dialog missing entirely
- [ ] Hash algorithm preference missing
- [ ] Expiry warning thresholds missing
- [ ] Serial number length preference missing
- [ ] DN configuration missing (mandatory / explicit subject entries)
- [ ] Suppress success messages missing
- [ ] Netscape disable setting missing
- [ ] PKCS#11 provider management missing (low priority)
- [ ] File → Options menu item missing (see §1)

### Intentional Deviations

- **PKCS#11 Provider tab**: Out of scope until hardware token support (M15).

### Priority

M14 (hash algorithm, expiry warnings, serial preferences, basic settings tab),
M15 (DN configuration, PKCS#11 tab)

---

## 14. OID Resolver Dialog

### XCA Reference

**OidResolver.ui — modeless dialog:**
- Search field (text input — real-time lookup as user types)
- Results: OID (copyable), Short name / sn (copyable), Long name / ln (copyable), Nid (copyable)
- Instruction text: "Enter the OID, the Nid, or one of the textual representations"

Accessed via: Extra → OID Resolver

### XcaNet Current

No OID Resolver dialog or equivalent.

### Gaps

- [ ] OID Resolver dialog missing
- [ ] Extra menu missing (see §1)

### Intentional Deviations

None declared. Low priority but should be present for parity.

### Priority

M15

---

## 15. Certificate Renewal Dialog

### XCA Reference

**CertExtend.ui:**
- Not before date picker
- Not after date picker
- Duration spinner + Days/Months/Years dropdown + Apply button
- Checkboxes: Midnight, Local time, No well-defined expiration
- Checkboxes: Revoke old certificate, Replace old certificate, Keep serial number

Accessed via: Certificates tab → right-click → Certificate renewal

### XcaNet Current

No certificate renewal dialog or workflow.

### Gaps

- [ ] Certificate renewal dialog missing entirely
- [ ] Context menu access point missing
- [ ] Not before / Not after date picker missing
- [ ] Duration quick-entry missing (Days/Months/Years)
- [ ] Revoke old / Replace old / Keep serial options missing

### Intentional Deviations

None declared yet.

### Priority

M14

---

## Implementation Roadmap

### M13.4 — Main Shell and Object Tabs Exact Parity

**Scope:**
- Add Import menu to menu bar with all import items (wired to stubs if backend not ready)
- Add Extra menu with: Password change, OID Resolver placeholder
- Add File → Options (stub), Close Database, Recent databases list
- Add Help → Documentation, About
- Fix object tab labels to match XCA exactly
- Ensure status bar shows meaningful operational status
- Confirm selected tab is visually obvious (accent border on active tab)

**Acceptance:**
- [ ] Menu bar matches XCA: File | Import | Token | Extra | Help with all top-level items
- [ ] All 5 object tabs visible and labeled as in XCA
- [ ] Status bar visible at bottom
- [ ] Active tab clearly indicated

---

### M13.5 — Private Keys and Requests Tab Parity

**Scope:**

*Private Keys:*
- Add `Use` column (related object count)
- Add `Token` column (security token indicator)
- Change button order/labels to: New, Export, Import, Show Details, Delete (+ key extras)
- Implement New Key modal dialog (separate from inspector inline tab)
- Implement Show Details — Key Detail modal (Key, Fingerprint, Comment tabs minimum)
- Implement Delete with confirmation dialog
- Implement Import (PEM/DER)

*Certificate Requests:*
- Add `Signed` column
- Change button order/labels to: New, Export, Import, Show Details, Delete (+ Sign etc.)
- Implement New Request (opens NewX509 dialog in request mode)
- Implement Show Details — CSR detail view
- Implement Delete with confirmation dialog
- Implement Import (PKCS#10)
- Implement per-type Import from Import menu (Keys, Requests)

**Acceptance:**
- [ ] Private Keys: columns match XCA, New opens modal, Show Details opens modal,
      Delete works, Import works
- [ ] CSR: columns match XCA, New opens modal (NewX509 request mode), Sign works,
      Show Details works, Delete works

---

### M13.6 — Certificates Tab and Chain Tree Parity

**Scope:**
- Add `Common Name` column
- Add `Status` column with expiry/revocation color coding
- Add `Fingerprint` column
- Implement hierarchical tree view grouping CAs with issued certificates
- Implement Plain View toggle (tree ↔ flat list)
- Implement New Certificate (opens NewX509 in certificate creation mode)
- Implement Show Details — CertDetail modal (Status, Subject, Issuer, Attributes,
  Extensions, Comment tabs)
- Implement Delete with confirmation
- Implement Import PKCS#12 and PKCS#7 buttons
- Implement Certificate Renewal dialog

**Acceptance:**
- [ ] Certificates: columns match XCA, tree hierarchy displays, Plain View toggles,
      New opens NewX509, Show Details opens CertDetail modal, Delete works

---

### M13.7 — Certificate / Request / Template Input Dialog Parity

**Scope:**

*NewX509 equivalent (CertificateAuthoringSurfaceView + TemplateAuthoringDialogView):*
- Source tab: Add mode radio buttons, signature algorithm dropdown, Not before/Not after
  date pickers (replace Validity days only)
- Subject tab: Replace free-form Subject text box with structured DN widget (CN, O, OU, C,
  ST, L, emailAddress minimum); Add private key selector; Add "Generate new key" option
- Extensions tab: Restructure — remove key algorithm from here; add SAN structured editor,
  subject/authority key identifier checkboxes
- Key Usage tab: Replace text boxes with individual checkboxes per flag; add Critical
  checkbox; add EKU multi-select list with Critical checkbox
- Comment tab: Add new tab with plain text editor
- Advanced tab: Verify OpenSSL config syntax editor works

*Template Dialog:*
- Apply all NewX509 tab improvements above
- Add Comment tab to template editing surface
- Add Source (read-only) and Insertion date (read-only) fields

**Acceptance:**
- [ ] Source tab shows mode selection and date pickers
- [ ] Subject tab has structured DN widget and key selector
- [ ] Key Usage tab uses checkboxes per flag
- [ ] Comment tab exists
- [ ] Template editing covers same tabs as certificate/request editing

---

### M14 — CA, CRL, Validation, Import/Export Behavior Parity

**Scope:**
- CA Properties dialog (CRL interval, default template)
- Revoke dialog (serial, date, reason dropdown)
- Manage Revocations dialog (tree + Add/Delete/Edit)
- Unrevoke action
- Generate CRL dialog (NewCrl equivalent) — CA selection, date pickers, extension checkboxes
- CRL Show Details modal (CrlDetail — 5 tabs)
- Context menus on all object list rows (right-click)
- Certificate Renewal dialog (CertExtend)
- Export dialog modal (format + filename + comment option)
- ImportMulti dialog for bulk import
- Options dialog (Settings tab: hash algorithm, expiry warnings, serial length)

**Acceptance:**
- [ ] CA-centric actions (CA Properties, Manage Revocations, Generate CRL) accessible
      from Certificates context menu on CA certs
- [ ] Revoke opens dialog with reason before committing
- [ ] CRL tab New opens NewCrl dialog
- [ ] All Show Details on all tabs open detail modals
- [ ] Right-click context menus present on all object list rows

---

### Remaining Gaps After M14 (M15 scope)

- Token menu operations (hardware token / PKCS#11)
- Open Remote Database
- Options → DN configuration tab
- Options → PKCS#11 Provider tab
- Password change dialog (Extra menu)
- OID Resolver dialog
- DH parameter generation
- Language selection
- Database dump / Certificate index export / Hierarchy export
- SSH public key import
- Security token key import/export/rename/delete

---

## Screen Parity Status Summary

| Screen | M13.4 | M13.5 | M13.6 | M13.7 | M14 | M15 |
|---|---|---|---|---|---|---|
| Main shell / menus | target | | | | | |
| Private Keys tab | | target | | | | |
| CSR tab | | target | | | | |
| Certificates tab | | | target | | | |
| Templates tab | | | | target | | |
| CRL tab | | | | | target | |
| NewX509 / input dialog | | | | target | | |
| CA Properties | | | | | target | |
| Revoke / Manage Revocations | | | | | target | |
| New CRL dialog | | | | | target | |
| Certificate Renewal | | | | | target | |
| Export dialog modal | | | | | target | |
| ImportMulti | | | | | target | |
| Options / Preferences | | | | | target | partial |
| OID Resolver | | | | | | target |
| Token operations | | | | | | target |
| Remote database | | | | | | target |

---

## Parity Completion Rule

A screen is **parity-complete** when all of the following are true:

1. Every visible button, column, tab, and dialog in the XCA reference for this screen
   is either implemented in XcaNet or listed under **Intentional Deviations** in this
   document with a written justification.
2. The workflow to reach and operate the screen matches the XCA mental model
   (object-centric, table-first, action-from-selection).
3. A person familiar with XCA can locate and operate the screen without guidance.
4. Any visual differences are minor (color scheme, font) and do not change where things
   are or what they are called.

Do not claim parity-complete based on "similar enough." Claim it based on this checklist.
