#!/usr/bin/env bash
set -euo pipefail

# Bootstrap GitHub planning for the XcaNet repo.
# Requires: gh, git, jq
# Usage:
#   ./github_bootstrap_xcanet.sh <owner>/<repo> [project-owner]
# Example:
#   ./github_bootstrap_xcanet.sh jensvercruysse/xcanet jensvercruysse

if ! command -v gh >/dev/null 2>&1; then
  echo "gh CLI is required." >&2
  exit 1
fi
if ! command -v git >/dev/null 2>&1; then
  echo "git is required." >&2
  exit 1
fi
if ! command -v jq >/dev/null 2>&1; then
  echo "jq is required." >&2
  exit 1
fi

REPO="${1:-}"
PROJECT_OWNER="${2:-}"
if [[ -z "$REPO" ]]; then
  echo "Usage: $0 <owner>/<repo> [project-owner]" >&2
  exit 1
fi
OWNER="${REPO%%/*}"
REPO_NAME="${REPO##*/}"
if [[ -z "$PROJECT_OWNER" ]]; then
  PROJECT_OWNER="$OWNER"
fi

# Ensure auth and project scope.
gh auth status >/dev/null
# Best effort; safe if already granted.
gh auth refresh -s project || true

create_label() {
  local name="$1"
  local color="$2"
  local desc="$3"
  gh label create "$name" -R "$REPO" --color "$color" --description "$desc" --force >/dev/null
  echo "[ok] label: $name"
}

create_milestone() {
  local title="$1"
  local desc="$2"
  local exists
  exists=$(gh api "repos/$REPO/milestones" --paginate --jq ".[] | select(.title == \"$title\") | .number" | head -n1 || true)
  if [[ -n "$exists" ]]; then
    echo "$exists"
    echo "[ok] milestone exists: $title" >&2
    return 0
  fi
  gh api -X POST "repos/$REPO/milestones" -f title="$title" -f description="$desc" --jq '.number'
}

get_milestone_number() {
  local title="$1"
  gh api "repos/$REPO/milestones" --paginate --jq ".[] | select(.title == \"$title\") | .number" | head -n1
}

create_issue() {
  local title="$1"
  local body_file="$2"
  local milestone_title="$3"
  shift 3
  local labels=("$@")
  local existing
  existing=$(gh issue list -R "$REPO" --state all --search "$title in:title" --json title,number --jq ".[] | select(.title == \"$title\") | .number" | head -n1 || true)
  if [[ -n "$existing" ]]; then
    echo "[ok] issue exists: #$existing $title"
    return 0
  fi
  local milestone_num
  milestone_num=$(get_milestone_number "$milestone_title")
  local args=(issue create -R "$REPO" --title "$title" --body-file "$body_file" --milestone "$milestone_title")
  for label in "${labels[@]}"; do
    args+=(--label "$label")
  done
  gh "${args[@]}" >/dev/null
  echo "[ok] issue: $title"
}

ensure_project() {
  local project_title="XcaNet Rewrite"
  local project_id
  project_id=$(gh project list --owner "$PROJECT_OWNER" --format json --jq ".projects[] | select(.title == \"$project_title\") | .id" | head -n1 || true)
  if [[ -z "$project_id" ]]; then
    project_id=$(gh project create --owner "$PROJECT_OWNER" --title "$project_title" --format json --jq '.id')
    echo "[ok] project created: $project_title"
  else
    echo "[ok] project exists: $project_title"
  fi
  echo "$project_id"
}

add_issue_to_project() {
  local project_id="$1"
  local issue_number="$2"
  local issue_node_id
  issue_node_id=$(gh api graphql -f query='query($owner:String!, $repo:String!, $number:Int!){repository(owner:$owner, name:$repo){issue(number:$number){id}}}' -F owner="$OWNER" -F repo="$REPO_NAME" -F number="$issue_number" --jq '.data.repository.issue.id')
  gh project item-add "$project_id" --owner "$PROJECT_OWNER" --url "https://github.com/$REPO/issues/$issue_number" >/dev/null || true
  echo "[ok] project item added: issue #$issue_number"
}

# Labels
create_label "type:feature" "0E8A16" "New user-facing or system capability"
create_label "type:bug" "D73A4A" "Bug fix"
create_label "type:task" "1D76DB" "Implementation or maintenance task"
create_label "type:spike" "5319E7" "Research or exploratory work"
create_label "area:ui" "A2EEEF" "User interface and UX"
create_label "area:storage" "C2E0C6" "Database, repositories, migrations"
create_label "area:security" "B60205" "Security, secrets, encryption"
create_label "area:crypto" "FBCA04" "Crypto operations, certs, PKI"
create_label "area:interop" "7057FF" "Native bridge and interoperability"
create_label "area:tests" "BFDADC" "Testing and validation"
create_label "area:docs" "0075CA" "Documentation"
create_label "backend:dotnet" "0052CC" "Managed .NET crypto/backend"
create_label "backend:openssl" "C5DEF5" "OpenSSL-backed behavior"
create_label "priority:high" "D93F0B" "High priority"
create_label "priority:medium" "FBCA04" "Medium priority"
create_label "priority:low" "0E8A16" "Low priority"

# Milestones
create_milestone "M1 - Skeleton" "Solution skeleton, dependency graph, CI, coding standards, Avalonia shell." >/dev/null
create_milestone "M2 - Storage and Security" "SQLite schema, EF Core migrations, encrypted private-key storage, lock/unlock flow, audit events." >/dev/null
create_milestone "M3 - Managed Crypto" "Managed backend for RSA/ECDSA, self-signed certs, CSR creation, standard import/export, parsing." >/dev/null
create_milestone "M4 - Core UI" "Lists, detail views, import/export, create CA, sign CSR." >/dev/null
create_milestone "M5 - Revocation" "Revocation workflow and CRL generation." >/dev/null
create_milestone "M6 - OpenSSL Bridge" "Thin native bridge, capability detection, compatibility-sensitive operations." >/dev/null
create_milestone "M7 - Parity Hardening" "Fixtures, parity tests, compatibility tuning, packaging docs." >/dev/null
create_milestone "v0.1.0" "First usable cross-platform preview release." >/dev/null

PROJECT_ID=$(ensure_project)

WORKDIR=$(mktemp -d)
trap 'rm -rf "$WORKDIR"' EXIT

cat > "$WORKDIR/m1-001.md" <<'EOF'
## Goal
Create the initial XcaNet solution and enforce the architecture boundaries from `SPEC.md`.

## Scope
- Create the `.NET 10` solution and required projects.
- Configure project references to respect the layered architecture.
- Add a minimal Avalonia shell app that launches on desktop.
- Enable nullable reference types and treat warnings as errors in app/core projects.
- Add basic DI, logging, and configuration bootstrap.
- Add `.editorconfig`, root `Directory.Build.props`, and a clean dependency graph.
- Add CI that restores, builds, and runs tests.

## Acceptance criteria
- Solution builds locally and in CI.
- Avalonia app launches to a placeholder shell.
- Project references match the intended layer boundaries.
- `README.md` contains bootstrap/build instructions.
- No crypto, storage, or native interop logic yet beyond placeholders.
EOF

cat > "$WORKDIR/m1-002.md" <<'EOF'
## Goal
Establish architecture decision records and repository conventions.

## Scope
- Add ADRs for architecture, crypto backend strategy, and secure key storage.
- Add issue templates and PR template.
- Add `docs/architecture` skeleton.
- Add contribution guidance for Codex and humans.
- Add a feature status matrix.

## Acceptance criteria
- ADR documents exist and explain context, decision, alternatives, and consequences.
- Repo templates exist for bugs, features, and spikes.
- A new contributor can understand the project structure from docs.
EOF

cat > "$WORKDIR/m2-001.md" <<'EOF'
## Goal
Implement the local database and secure private-key-at-rest design.

## Scope
- Add SQLite storage via EF Core.
- Create initial schema and migrations.
- Model certificates, private keys, CSRs, CRLs, templates, tags, audit events, and app settings.
- Store private keys as encrypted PKCS#8 blobs.
- Add open/create/unlock/lock database flow.
- Add audit event recording for security-sensitive actions.

## Acceptance criteria
- Database can be created and reopened.
- Private keys are never stored in plaintext.
- Migrations apply cleanly on a fresh machine.
- Unit/integration tests cover encryption and persistence basics.
EOF

cat > "$WORKDIR/m2-002.md" <<'EOF'
## Goal
Implement the security service and secret handling policy.

## Scope
- Add master password setup and change flow.
- Add key-derivation and authenticated encryption helpers.
- Add inactivity auto-lock policy.
- Add export warnings and secure logging redaction.
- Add a clear boundary between UI, security, and storage.

## Acceptance criteria
- Security service can derive a master key and encrypt/decrypt key blobs.
- Logs do not contain plaintext secrets or private keys.
- Auto-lock can be triggered by policy.
- Tests cover failure cases and wrong-password behavior.
EOF

create_issue "M1.1 - Create solution skeleton and Avalonia shell" "$WORKDIR/m1-001.md" "M1 - Skeleton" "type:task" "area:ui" "area:docs" "priority:high"
create_issue "M1.2 - Add ADRs, templates, and contributor guidance" "$WORKDIR/m1-002.md" "M1 - Skeleton" "type:task" "area:docs" "priority:medium"
create_issue "M2.1 - Implement SQLite schema, migrations, and encrypted key storage" "$WORKDIR/m2-001.md" "M2 - Storage and Security" "type:feature" "area:storage" "area:security" "priority:high"
create_issue "M2.2 - Build security service, password flow, and secret redaction" "$WORKDIR/m2-002.md" "M2 - Storage and Security" "type:feature" "area:security" "area:tests" "priority:high"

# Add created issues to project.
for title in \
  "M1.1 - Create solution skeleton and Avalonia shell" \
  "M1.2 - Add ADRs, templates, and contributor guidance" \
  "M2.1 - Implement SQLite schema, migrations, and encrypted key storage" \
  "M2.2 - Build security service, password flow, and secret redaction"; do
  num=$(gh issue list -R "$REPO" --state all --search "$title in:title" --json title,number --jq ".[] | select(.title == \"$title\") | .number" | head -n1)
  add_issue_to_project "$PROJECT_ID" "$num"
done

# Set local upstream remote if we're inside a git repo.
if git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
  if git remote get-url xca-upstream >/dev/null 2>&1; then
    echo "[ok] remote exists: xca-upstream"
  else
    git remote add xca-upstream https://github.com/chris2511/xca.git
    echo "[ok] added remote: xca-upstream"
  fi
else
  echo "[warn] not inside a git repo; skipped adding local xca-upstream remote"
fi

echo "\nBootstrap complete for $REPO"
echo "Project owner: $PROJECT_OWNER"
