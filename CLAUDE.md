# Claude Session Guide - Codewrinkles Universe

> **Purpose**: Quick reference for Claude Code sessions working on this codebase. Read this at the start of each session along with `core.md`.

---

## Tech Stack

### Frontend (`apps/frontend/`)
- **Build Tool**: Vite 6.4.1
- **Framework**: React 18.3.1
- **Language**: TypeScript 5.7.3 (strict mode)
- **Styling**: TailwindCSS 3.4.17
- **Routing**: React Router DOM 7.1.1
- **Node**: 20+

### Backend (`apps/backend/`)
- **Framework**: .NET 10, ASP.NET Core Minimal APIs
- **Language**: C# 13
- **Database**: SQL Server (latest)
- **ORM**: Entity Framework Core 10
- **Architecture**: Clean Architecture (layered monolith)
- **API Docs**: Scalar
- **Testing**: xUnit (plain assertions, no FluentAssertions)
- **CQRS**: Kommand (https://github.com/Atherio-Ltd/Kommand)

### Package Manager
- **npm** (not yarn or pnpm) for frontend
- **dotnet CLI** for backend

---

## Code Standards - ENFORCE STRICTLY

### 🚨 CRITICAL: Library Usage Policy

**❌ NEVER USE LIBRARIES UNLESS IT'S THE ONLY FEASIBLE OPTION**

This is a **fundamental principle** for both frontend and backend:

- ❌ **NO FluentAssertions** - use plain xUnit assertions
- ❌ **NO FluentValidation** - write validation logic ourselves
- ❌ **NO popular convenience libraries** that save a few lines of code
- ❌ **NO libraries for simple utilities** (string manipulation, mediation, validation, etc.)
- ✅ **ALWAYS ASK BEFORE adding any new library or package**

**When a library IS acceptable:**
- Framework essentials (React, .NET, EF Core, etc.)
- Complex, well-tested infrastructure (OpenTelemetry, BCrypt, JWT)
- Features that would take weeks to build correctly (OAuth clients, etc.)

**Why this matters:**
- Keeps dependencies minimal
- Forces us to understand the code we write
- Reduces security surface area
- Avoids bloat and version conflicts

**BEFORE adding any package, ask yourself:**
1. Can I build this in < 1 day?
2. Does this require deep domain expertise?
3. Is this a critical security feature?

If all three are "no", **build it yourself**.

---

### TypeScript
- ✅ Strict mode enabled in `tsconfig.json`
- ✅ ALL strictness flags enabled (strictNullChecks, noImplicitAny, etc.)
- ❌ **NEVER use `any` type** - use `unknown` or proper types
- ❌ **NEVER use `@ts-ignore`** - fix the type error instead
- ✅ Explicit return types on functions
- ✅ Use type imports: `import type { Foo } from "./types"`

### React
- ✅ Use functional components (no class components)
- ✅ Use hooks (`useState`, `useEffect`, etc.)
- ✅ JSX return type: `JSX.Element`
- ❌ **NO `import React from "react"`** - we use the new JSX transform
- ✅ Props interfaces named `{ComponentName}Props`
- ✅ Export component function directly: `export function ComponentName()`

### File Naming
- ✅ Components: `PascalCase.tsx` (e.g., `LoginPage.tsx`, `UserProfile.tsx`)
- ✅ Hooks: `camelCase.ts` with `use` prefix (e.g., `useTheme.ts`, `useAuth.ts`)
- ✅ Types: `types.ts` (shared types in `src/types.ts`, feature-specific in feature folder)
- ✅ Utilities: `camelCase.ts` (e.g., `formatDate.ts`)

### Imports
- ✅ Group imports: React → third-party → types → local components → hooks → utils
- ✅ Use relative imports for local files
- ✅ Barrel exports OK for features (`index.ts` re-exporting components)

---

## Project Structure

```
apps/frontend/
├── src/
│   ├── features/           # Feature-based organization (PRIMARY pattern)
│   │   ├── auth/          # Login, Register, AuthCard, etc.
│   │   ├── home/          # HomePage and related components
│   │   ├── pulse/         # Microblogging feature
│   │   ├── learn/         # Learning feature (will merge into Nova)
│   │   ├── twin/          # AI Twin feature (will merge into Nova)
│   │   ├── settings/      # Settings page and sections
│   │   ├── shell/         # App shell (Header, Nav, Layout)
│   │   └── onboarding/    # Onboarding flow
│   ├── components/        # Shared UI components only
│   │   └── ui/            # Button, Card, Toggle, Tag, etc.
│   ├── hooks/             # Shared hooks (useTheme, etc.)
│   ├── types.ts           # Shared type definitions
│   ├── App.tsx            # Main app with routing
│   └── main.tsx           # Entry point
├── public/                # Static assets
├── package.json
├── tsconfig.json          # TypeScript config (strict mode)
├── tailwind.config.js     # Design tokens
├── vite.config.ts
└── postcss.config.js
```

### When to Create a New Feature Folder
- Feature has 2+ components
- Feature has its own routing
- Feature has self-contained logic
- Examples: `auth/`, `pulse/`, `settings/`

### When to Use `components/ui/`
- Component is reusable across multiple features
- Component is purely presentational (no business logic)
- Examples: `Button`, `Card`, `Toggle`, `Tag`

---

## Routing Patterns

### React Router v7
- ✅ Use `BrowserRouter` in `App.tsx`
- ✅ Use `<Routes>` and `<Route>` for route definitions
- ✅ Use `<Outlet>` for nested routes (see `ShellLayout`, `SettingsPage`)
- ✅ Use `<Link>` for navigation
- ✅ Use `<NavLink>` for nav items (has `isActive` prop)
- ✅ Use `useNavigate()` for programmatic navigation
- ✅ Use `useLocation()` to access current route

### Route Structure
```
/ → Home
/social → Pulse (microblogging)
/learn → Learn (will become /nova)
/twin → Twin (will become /nova)
/settings → Settings
  /settings/profile
  /settings/account
  /settings/apps
  /settings/notifications
/login → Login
/register → Register
/onboarding → Onboarding
```

### Nested Routes Pattern
See `SettingsPage.tsx` and `App.tsx` for reference:
- Parent route renders `<Outlet />`
- Child routes render inside Outlet
- Use `<Navigate>` for default redirects

---

## Design System

### Colors (Tailwind Custom Tokens)
```javascript
brand: {
  DEFAULT: "#20C1AC",  // Primary brand color
  soft: "#35D6C0"      // Softer variant
}

surface: {
  page: "#050505",     // Page background
  card1: "#0A0A0A",    // Card background (elevated)
  card2: "#111111"     // Card background (more elevated)
}

border: {
  DEFAULT: "#2A2A2A",  // Default borders
  deep: "#1A1A1A"      // Deeper borders
}

text: {
  primary: "#F3F4F6",    // Main text
  secondary: "#A1A1AA",  // Secondary text
  tertiary: "#737373"    // Tertiary text (subtle)
}
```

### Usage in Components
- ✅ Use Tailwind classes with these tokens: `bg-surface-card1`, `text-text-primary`, `border-border`
- ✅ Consistent spacing: `space-y-6`, `gap-3`, `p-4`, `px-3 py-2`
- ✅ Consistent rounding: `rounded-xl` for cards, `rounded-full` for buttons/tags
- ✅ Consistent borders: `border border-border`

### Typography Scale
- Page title: `text-base font-semibold tracking-tight`
- Section title: `text-sm font-semibold tracking-tight`
- Body text: `text-sm text-text-primary`
- Secondary text: `text-xs text-text-secondary`
- Tertiary/muted: `text-xs text-text-tertiary`
- Tags/badges: `text-[11px]`

---

## Component Patterns

### Basic Component Template
```tsx
import type { SomeType } from "../../types";

export interface ComponentNameProps {
  someProp: string;
  optionalProp?: number;
}

export function ComponentName({ someProp, optionalProp }: ComponentNameProps): JSX.Element {
  return (
    <div className="...">
      {/* component content */}
    </div>
  );
}
```

### Hooks Pattern
```tsx
import { useState, useEffect } from "react";

export function useCustomHook() {
  const [state, setState] = useState<Type>(initialValue);

  useEffect(() => {
    // side effects
  }, []);

  return { state, setState };
}
```

### Event Handlers
- ✅ Use explicit types: `(e: React.FormEvent): void => {}`
- ✅ Name handlers: `handleSubmit`, `handleClick`, `handleChange`
- ✅ For callbacks passed as props: `onSubmit`, `onClick`, `onChange`

---

## State Management

### Current Approach
- ✅ Local state: `useState` for component-level state
- ✅ Shared state: Custom hooks (see `useTheme.ts`)
- ✅ Theme persistence: `localStorage` in `useTheme` hook
- ❌ NO global state library yet (Redux, Zustand, etc.) - use hooks for now

### Future (when needed)
- User authentication state → Context or lightweight state library
- Global UI state (modals, toasts) → Context
- Server state → React Query or SWR

---

## Styling Rules

### TailwindCSS Usage
- ✅ Use utility classes directly in JSX
- ✅ Conditional classes: Template literals with ternary
  ```tsx
  className={`base-classes ${isActive ? "active-classes" : "inactive-classes"}`}
  ```
- ✅ For complex conditionals: Extract to variable
- ❌ NO inline styles - use Tailwind classes
- ❌ NO custom CSS files (except `index.css` for Tailwind directives)

### Responsive Design
- ✅ Mobile-first approach
- ✅ Use Tailwind breakpoints: `sm:`, `md:`, `lg:`
- ✅ Hide on mobile: `hidden lg:flex`
- ✅ Show on mobile only: `lg:hidden`

---

## Common Commands

```bash
# Development
cd apps/frontend
npm run dev         # Start dev server (http://localhost:5173)

# Type checking
npm run lint        # Run TypeScript compiler (no emit)

# Build
npm run build       # TypeScript compile + Vite build
npm run preview     # Preview production build

# Install dependencies
npm install         # Install all dependencies
```

---

## Anti-Patterns - DO NOT DO

❌ **NO `any` types** - always use proper types or `unknown`
❌ **NO `@ts-ignore`** - fix the actual type error
❌ **NO `import React from "react"`** - unnecessary with new JSX transform
❌ **NO inline styles** - use Tailwind classes
❌ **NO custom CSS files** (except necessary global styles)
❌ **NO class components** - use functional components
❌ **NO prop drilling beyond 2 levels** - use context or hooks
❌ **NO magic numbers** - extract to constants
❌ **NO console.log in production code** - remove before committing
❌ **NO over-engineering** - keep it simple, iterate based on needs

---

## Best Practices

### Code Organization
- ✅ One component per file
- ✅ Co-locate related components in feature folders
- ✅ Keep components small (<200 lines)
- ✅ Extract complex logic to hooks
- ✅ Extract reusable UI to `components/ui/`

### Type Safety
- ✅ Define props interfaces for all components
- ✅ Use union types for variants: `type Variant = "primary" | "secondary"`
- ✅ Use `as const` for readonly arrays/objects
- ✅ Prefer interfaces over types for objects (better error messages)

### Performance
- ✅ Use React.memo() only when needed (avoid premature optimization)
- ✅ Use useCallback/useMemo for expensive computations
- ❌ Don't optimize until you have a performance problem

### Accessibility
- ✅ Use semantic HTML (`<button>`, `<nav>`, `<main>`, etc.)
- ✅ Add `aria-label` for icon-only buttons
- ✅ Ensure keyboard navigation works
- ✅ Use proper heading hierarchy (h1 → h2 → h3)

---

## Git Workflow

### Branches
- `main` - production-ready code
- Feature branches: `feature/feature-name`
- Bug fixes: `fix/bug-description`

### Commits
- ✅ Descriptive commit messages
- ✅ Conventional commits format (optional): `feat:`, `fix:`, `refactor:`, etc.
- ✅ Commit working code (passes TypeScript + builds)

---

## Backend Architecture (Decided)

### Structure
- **Location**: `apps/backend/`
- **Solution**: `Codewrinkles.sln`
- **Projects**:
  - `Codewrinkles.API` - Minimal APIs, endpoints, middleware
  - `Codewrinkles.Application` - Use cases, Kommand handlers
  - `Codewrinkles.Domain` - Entities, value objects, domain events
  - `Codewrinkles.Infrastructure` - EF Core, DbContext, external services

### Patterns
- ✅ Clean Architecture (layered monolith)
- ✅ Single database with schema-per-module (`identity`, `pulse`, `nova`)
- ✅ Single `ApplicationDbContext` with all DbSets
- ✅ Modules as folders (not separate projects)
- ✅ Feature-based organization (NOT convention-based like Commands/Queries folders)
- ✅ CQRS with Kommand
- ✅ OpenTelemetry instrumentation from the start
- ✅ Custom authentication (no ASP.NET Core Identity)
- ✅ JWT + refresh tokens with revocation

### Future Decisions (Not Implemented Yet)

#### Testing (Frontend)
- Unit tests: TBD (Vitest or Jest)
- Component tests: TBD (React Testing Library)
- E2E tests: TBD (Playwright or Cypress)

#### Databases (Future modules)
- Vector DB: TBD (Pinecone, Weaviate, Qdrant for Nova)
- Graph DB: TBD (Neo4j for knowledge graphs)

### CI/CD
- TBD (GitHub Actions, Vercel, etc.)

---

## Quick Debugging Tips

### TypeScript Errors
- Run `npm run lint` to see all type errors
- Check `tsconfig.json` - strict mode is enabled
- Look for missing type imports

### Build Errors
- Check for console.logs or comments with syntax errors
- Verify all imports are correct
- Run `npm run build` to see detailed errors

### Runtime Errors
- Check browser console (dev server runs on http://localhost:5173)
- Verify React Router routes are correct
- Check for missing prop types

---

## Notes for Claude Sessions

1. **Always read `core.md` first** to understand product vision
2. **Always read this file** to understand technical conventions
3. **🚨 ALWAYS ASK before adding any library/package** - see Library Usage Policy above
4. **Run `npm run lint` before finishing** (frontend) to ensure type safety
5. **Test the build** with `npm run build` (frontend) or `dotnet build` (backend) for production readiness
6. **Keep strict TypeScript** - no `any`, no `@ts-ignore`
7. **Feature folders** for new features, not flat component structure (frontend and backend)
8. **Design tokens** - use custom Tailwind colors, don't hardcode hex values (frontend)
9. **Mobile-first** - always consider responsive design (frontend)
10. **Ask before over-engineering** - keep it simple until complexity is needed
11. **Don't make assumptions** - ask questions when unclear
12. **Document decisions** - update this file if you make new technical decisions

---

## Current State Reminders

### Frontend
- ✅ React Router is fully integrated
- ✅ Theme system works (dark/light with localStorage)
- ✅ Settings has nested routes working
- ⚠️ Learn + Twin will merge into Nova (not refactored yet)
- ⚠️ No real authentication (UI only, no backend integration)
- ⚠️ No API calls yet
- ⚠️ No tests yet

### Backend
- ✅ Project structure defined in `backend.md`
- ✅ Solution and projects created
- ✅ NuGet packages installed
- ⚠️ No database schema yet
- ⚠️ No features implemented yet
- ⚠️ Feature implementation will be done one at a time

---

## Infrastructure Preferences

### Database
- ✅ **Use SQL Server LocalDB** (accessed via JetBrains Rider)
- ❌ **NO Docker** - Do not use Docker for SQL Server or any other services unless explicitly requested
- Connection String format: `Server=(localdb)\\mssqllocaldb;Database=Codewrinkles;Trusted_Connection=True;MultipleActiveResultSets=true`

### Containers & Deployment
- ❌ **NO Docker** unless explicitly requested by the user
- Local development uses native tools (Rider, VS, etc.)

---

**Last Updated**: 2025-11-23 (update this when making significant changes)
