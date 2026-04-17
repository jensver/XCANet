# Codex prompt: bootstrap GitHub planning for XcaNet

Work in the root of my local `xcanet` repository.

Goals:
1. Ensure the repository has the labels, milestones, project, and initial issues defined in `SPEC.md`.
2. Add the upstream XCA repo as a local-only git remote named `xca-upstream`.
3. Do **not** fork the upstream repository.
4. Use GitHub CLI (`gh`) and git.
5. Be idempotent: if something already exists, keep it and move on.

Steps:
- Verify `gh auth status` and refresh project scope with `gh auth refresh -s project` if needed.
- Run the bootstrap script at `./github_bootstrap_xcanet.sh <owner>/<repo> <project-owner>`.
- If the repo does not yet contain the script, create it from the contents in the attached/bootstrap artifact.
- Report back with:
  - created or confirmed labels
  - created or confirmed milestones
  - project URL/name
  - created issues and numbers
  - whether `xca-upstream` remote was added

Constraints:
- Do not modify source code yet.
- Do not create a fork of `chris2511/xca`.
- Keep all planning assets in GitHub native tools: Issues, Milestones, and Projects.
