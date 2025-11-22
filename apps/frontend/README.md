# Codewrinkles Frontend

A multi-app universe built with Vite + React + TypeScript + TailwindCSS.

## Project Structure

```
apps/frontend/
├── src/
│   ├── components/
│   │   └── ui/              # Reusable UI components
│   │       ├── Card.tsx
│   │       ├── Button.tsx
│   │       ├── Tag.tsx
│   │       └── Toggle.tsx
│   ├── features/            # Feature-based organization
│   │   ├── shell/           # Header, layout, navigation
│   │   ├── home/            # Control room / landing
│   │   ├── pulse/           # Social feed
│   │   ├── learn/           # Learning paths
│   │   ├── twin/            # AI twin chat
│   │   ├── settings/        # Settings pages
│   │   ├── auth/            # Login / Register
│   │   └── onboarding/      # Onboarding wizard
│   ├── hooks/
│   │   └── useTheme.ts      # Theme hook with localStorage
│   ├── types.ts             # Shared TypeScript types
│   ├── index.css            # Tailwind imports + theme styles
│   ├── App.tsx              # Main app component
│   └── main.tsx             # Entry point
├── public/
├── index.html
├── package.json
├── tsconfig.json            # Strict TypeScript config
├── vite.config.ts
├── tailwind.config.js       # Design system tokens
└── postcss.config.js
```

## Features

- **TypeScript Strict Mode**: Full type safety with no `any`
- **Feature-Based Architecture**: Organized by domain, not tech layer
- **Centralized Design System**: Custom Tailwind tokens for colors, spacing, typography
- **Theme Support**: Dark/Light mode with localStorage persistence
- **Responsive**: Mobile-first with adaptive layouts

## Tech Stack

- **Vite**: Fast build tool and dev server
- **React 18**: UI library with hooks
- **TypeScript 5.7**: Strict mode enabled
- **TailwindCSS 3**: Utility-first CSS with custom design tokens
- **PostCSS**: CSS processing

## Getting Started

### Install Dependencies

```bash
npm install
```

### Development Server

```bash
npm run dev
```

The app will be available at `http://localhost:5173`

### Build for Production

```bash
npm run build
```

Outputs to `dist/` directory.

### Preview Production Build

```bash
npm run preview
```

### Type Check

```bash
npm run lint
```

## Design System

The app uses a centralized design system defined in `tailwind.config.js`:

### Colors

- **Brand**: `#20C1AC` (teal) - Primary brand color
- **Surface**: Dark backgrounds (`#050505`, `#0A0A0A`, `#111111`)
- **Border**: Subtle borders (`#2A2A2A`, `#1A1A1A`)
- **Text**: Semantic text colors (primary, secondary, tertiary)
- **Pulse**: Sky accent for Social app
- **Nova**: Violet accent for Learn/Twin apps

### Theme Switching

The app supports dark and light themes via the `useTheme` hook:

- Persists to `localStorage` under key `cw-theme`
- Applies `.dark-theme` or `.light-theme` class to document root
- Theme-specific color overrides in `src/index.css`

## Architecture Notes

### State Management

- **Local state**: `useState` for component-level state
- **No global state library**: Simple prop drilling for now
- **Future**: Context API or Zustand when complexity grows

### Navigation

- **No router yet**: View switching via `useState`
- String unions ensure type safety: `MainView`, `AppSection`
- **Future**: React Router when URL persistence is needed

### Type Safety

- All props are strongly typed with interfaces
- No `any` types in the codebase
- Event handlers use React's built-in types (`React.MouseEvent`, `React.FormEvent`)
- `noUncheckedIndexedAccess` ensures safe array/object access

## Future Enhancements

- [ ] Add React Router for URL-based navigation
- [ ] Integrate backend API
- [ ] Add Zustand or Context for global state
- [ ] Implement real auth flows
- [ ] Add unit tests (Vitest)
- [ ] Add E2E tests (Playwright)
- [ ] Performance monitoring
- [ ] Analytics integration
