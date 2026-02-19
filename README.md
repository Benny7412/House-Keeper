# HouseKeeper

HouseKeeper is a Blazor Server web app for shared household management. It provides fast, low-friction tracking for chores, expenses, members, and recent household activity.

## Feature Summary

- Account registration, login, logout (cookie auth)
- Household membership with admin role
- Invite-by-username workflow for adding members
- Chore feed with urgent flag, due date, and completion toggles
- Expense feed with urgent flag and settled toggles
- Town Hall activity timeline for recent household events
- Mobile-friendly dashboard navigation and quick-add modal

## Tech Stack

- .NET 10 Blazor Server
- MongoDB (official MongoDB .NET driver)
- Cookie authentication
- Razor component scoped CSS + Sass build pipeline

## Setup

### 1. Configure MongoDB

Set either config values in `appsettings.Development.json` or environment variables:

- `MONGODB_CONNECTION_STRING`
- `MONGODB_DATABASE_NAME`

`MONGODB_CONNECTION_STRING` is required (or `MongoDb:ConnectionString` in app settings).

### 2. Install Sass dependencies

```bash
npm install
```

### 3. Build scoped/global styles from `.scss`

```bash
npm run sass:build
```

### 4. Run the app

```bash
dotnet run
```

## Architecture Notes

- `Components/Features/*` holds vertical slices (pages, state, service, contracts, components).
- `State` classes provide UI state and error feedback.
- `Service` classes handle data access and household scoping.
- `Components/Services/HouseholdContextAccessor.cs` enforces household-level query scoping for authenticated users.
- Mongo indexes are created at startup via `Data/MongoIndexInitializer.cs`.

## Accessibility and UX

The app includes:

- Semantic headings and navigation landmarks
- Skip-to-content navigation
- Keyboard-focus styling (`:focus-visible`)
- Modal keyboard support (Escape to close) with focus targeting
- `aria-live` status and error announcements for loading/feedback
- Explicit labels for form controls used in quick-add and settings flows

Target guideline: WCAG 2.1 AA-aligned behavior for core interaction paths.

## Error Handling

- Feature state classes catch service errors and expose user-friendly error messages.
- Exceptions are logged through `ILogger` in state layers for diagnostics.
- UI pages surface accessible loading and error messages.
- Account endpoints use server-side validation and return actionable feedback.

## Security Notes

- HTTPS redirection is enabled.
- Antiforgery is enabled for account and logout form posts.
- Return URLs are normalized to local routes only.
- Household operations are scoped to the authenticated user and household membership.

## Verification

Recommended local verification commands:

```bash
npm run sass:build
dotnet build
```
