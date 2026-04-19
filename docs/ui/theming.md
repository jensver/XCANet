# UI Theming

XcaNet uses shared Avalonia theme resources so the desktop UI stays readable in both light and dark mode without per-view color patching.

## Where theme resources live

- `src/XcaNet.App/Styles/ThemeResources.axaml` contains:
  - light and dark `ThemeDictionaries`
  - reusable semantic brushes
  - shared styles for cards, empty states, diagnostics panels, notifications, nav buttons, and read-only surfaces
- `src/XcaNet.App/App.axaml` imports the shared style dictionary once for the whole application

## Current design rules

- Prefer `DynamicResource` and shared classes over inline `#RRGGBB` values in page views.
- Keep business and application layers unaware of theming.
- Use semantic classes instead of one-off color choices when a surface communicates state.

## Shared surface classes

- `surface-card`: default page card and inspector surface
- `surface-subtle`: quieter nested container
- `status-pill`: compact busy/status surface
- `empty-state`: empty lists and placeholder guidance
- `info-panel`: informational guidance blocks
- `validation-panel`: warnings or confirmation-heavy actions
- `notification-chip`: shell notifications with semantic modifiers
- `readonly-surface`: read-only text payload/summary surface

## Shared text classes

- `secondary-text`: supporting copy
- `muted-text`: tertiary or summary text
- `accent-text`: notable but non-error emphasis
- `success-text`
- `warning-text`
- `error-text`

## Extending the UI safely

When adding a new page or inspector:

1. Start with `surface-card` for the main container.
2. Use `secondary-text` or `muted-text` for supporting copy.
3. Reuse `empty-state`, `info-panel`, or `validation-panel` before inventing a new visual pattern.
4. Only add a new semantic resource if the same meaning is expected to recur across pages.

## Semantic state guidance

- Success, warning, error, and info surfaces should remain readable without relying only on color.
- Empty states should include a clear title and supporting instruction text.
- Selected navigation and disabled states should stay visible in both themes through background, border, and text contrast together.

## Manual verification checklist

Verify both light and dark mode for:

- Certificates page
- Private Keys page
- CSRs page
- CRLs page
- Templates page
- Settings / Security page
- shell navigation
- inspector/detail panels
- empty states
- validation and confirmation sections
- notification chips

Regression guardrails also exist in tests to catch hard-coded page brush literals and missing MCP guidance references.
