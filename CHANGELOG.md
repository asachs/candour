# Changelog

All notable changes to Candour are documented in this file.

Format follows [Keep a Changelog](https://keepachangelog.com/). Versioning follows [Semantic Versioning](https://semver.org/).

## [Unreleased]

## [0.3.0] - 2026-02-27

### Added
- Custom teal MudBlazor theme with intentional brand identity
- Home page: call-to-action button, how-it-works timeline, feature icons
- Branded loading screen with CSS spinner (replaces plain text)
- Breadcrumb navigation on survey builder and survey detail pages
- Delete confirmation dialog for survey questions in builder
- Styled 404 page with MudBlazor components
- Question options displayed as chips on admin survey detail page
- Dark mode compatible Mermaid diagrams across all documentation
- UserJourneys skill for automated screenshot capture with DOM-level PII redaction
- User journey documentation with 13 annotated screenshots across 9 journeys

### Fixed
- **CRITICAL:** Anonymous survey form crash (`AccessTokenNotAvailableException`) — public endpoints now use unauthenticated HttpClient via keyed DI services
- **CRITICAL:** Tagline text now meets WCAG AA contrast (was 3.33:1, now passes 4.5:1)
- Sidebar drawer no longer duplicates top nav on desktop (now mobile-only temporary drawer)
- Content no longer hidden behind fixed app bar (100px inline padding-top with `!important`)
- Heading hierarchy corrected across all pages (proper h1-h6 ordering)
- Question type dropdown shows friendly labels instead of raw enum values
- Publish button conditionally rendered instead of hidden via `display:none`
- Dashboard actions column right-aligned with outlined button style
- Sequence diagram text invisible on GitHub dark mode (removed pastel rect backgrounds)

### Removed
- Bootstrap CSS (32 files) — MudBlazor handles all styling
- Bootstrap-oriented selectors from app.css

## [0.2.0] - 2026-02-27

### Added
- Azure deployment (Functions Flex Consumption, Static Web Apps, Cosmos DB serverless)
- Admin authentication middleware with Entra ID JWT and email allowlist
- FQDN token links with copy-to-clipboard buttons (single + bulk)
- `Candour__FrontendBaseUrl` configuration for shareable link generation
- Cost estimate section in README (< $1/month for small deployments)
- Battle-tested deployment guide (`docs/DEPLOY.md`) with troubleshooting
- Professional Mermaid diagrams: architecture overview, threat model, blind token scheme, access control
- Admin-only access control on aggregate results endpoint

### Fixed
- Mermaid `\n` rendering — switched to `<br/>` for line breaks
- Route descriptions clarified in documentation diagrams

## [0.1.0] - 2026-02-26

### Added
- Core survey engine with CQRS (MediatR) command/query separation
- Blind token scheme: HMAC-SHA256 token generation, SHA256 one-way storage
- Anonymity middleware: IP stripping, timestamp jitter, zero-PII data model
- Blazor WebAssembly frontend with MudBlazor Material Design
- Admin dashboard: survey list, survey builder, survey detail with results
- Respondent survey form with support for multiple choice, free text, rating, yes/no, matrix questions
- Threshold gating: results hidden until minimum response count met
- Aggregate-only results: no API endpoint returns individual response data
- Entra ID authentication with dev-mode API key fallback
- Mobile responsive UI across all pages
- Comprehensive test suite: 201+ tests achieving >80% coverage
- Project logo and branded README

### Security
- Remediated all 14 security audit findings
- Block used/invalid tokens before exposing survey questions
- No FK between UsedTokens and Responses tables (architectural anonymity)

### Fixed
- Rating aggregate options display as "X / 5" instead of bare numbers
- Flaky API integration test resolved with eager server warmup

[Unreleased]: https://github.com/asachs/candour/compare/v0.3.0...HEAD
[0.3.0]: https://github.com/asachs/candour/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/asachs/candour/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/asachs/candour/releases/tag/v0.1.0
