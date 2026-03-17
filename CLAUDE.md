# Morii Coffee - Workflow Orchestration
## Workflow Orchestration

### 1. Plan Mode Default
- Enter plan mode for ANY non-trivial task (3+ steps or architectural decisions).
- If something goes sideways, STOP and re-plan immediately - don't keep pushing blindly.
- Use plan mode for verification steps, not just building.
- Write detailed specs upfront to reduce ambiguity before writing any code.

### 2. Subagent Strategy & Delegation
- Use subagents liberally to keep the main context window clean and focused.
- Offload research, exploration, and parallel analysis to subagents.
- Pass clear input context and demand structured output from subagents.
- One task per subagent for focused execution.

### 3. Self-Improvement Loop
- After ANY correction from the user: update `tasks/lessons.md` with the failure pattern.
- Write rules for yourself that prevent the exact same mistake.
- Ruthlessly iterate on these lessons until the mistake rate drops to zero.
- Review `lessons.md` at the start of every session for the relevant project.

### 4. Verification Before Done
- Never mark a task complete without proving it works.
- Diff behavior between main and your changes when relevant.
- Ask yourself: "Would a staff engineer approve this PR?"
- Run tests, check logs, verify UI (if applicable), and demonstrate correctness.
- Ensure code passes local linting and formatting standards.

### 5. Demand Elegance (Balanced)
- For non-trivial changes: pause and ask "Is there a more elegant/performant way?"
- If a fix feels hacky: "Knowing everything I know now, implement the elegant solution."
- Skip this for simple, obvious fixes - don't over-engineer.
- Challenge your own work before presenting it to the user.

### 6. Autonomous Execution & Communication
- When given a bug report: just fix it. Don't ask for hand-holding.
- Point at logs, errors, failing tests - then resolve them.
- Go fix failing CI tests without being told how.
- **Communication:** Be concise. No fluff, no apologies, no unnecessary conversational filler. Just state what was done, why, and the result.

## Task Management (Definition of Done)

1. **Plan First**: Write the plan to `tasks/todo.md` with checkable items.
2. **Verify Plan**: Check in with the user before starting implementation (if architectural).
3. **Track Progress**: Mark items complete as you go.
4. **Git Discipline**: Make atomic, descriptive Git commits after verifying each logical chunk.
5. **Document Results**: Add a quick review/summary section to `tasks/todo.md`.
6. **Capture Lessons**: Update `tasks/lessons.md` after any user corrections.

## Core Principles

- **Simplicity First**: Make every change as simple as possible. Impact minimal code.
- **No Laziness**: Find root causes. No temporary band-aids. Uphold Senior Developer standards.
- **Minimal Impact**: Changes should only touch what's necessary. Avoid refactoring unrelated code unless explicitly asked.


# Morii Coffee — Next.js Frontend
## Project Overview
Morii Coffee is a coffee shop web application. This repository contains the Next.js frontend that connects to a .NET 8 Clean Architecture backend API.

## Tech Stack
- **Framework**: Next.js 16 (App Router, `src/` directory)
- **Language**: TypeScript (strict mode)
- **Styling**: Tailwind CSS v4 with CSS variables (shadcn-style design system)
- **State Management**: Zustand (with persist middleware for cart/wishlist)
- **i18n**: next-intl (VI/EN), locale stored in cookie
- **Dark Mode**: next-themes, class-based `.dark` toggling
- **UI Primitives**: Radix UI + custom components (shadcn pattern)
- **Animations**: Framer Motion
- **Icons**: Lucide React
- **Carousel**: Embla Carousel
- **Charts**: Recharts (admin dashboard)
- **Package Manager**: pnpm

## Backend API Reference
- **Base URL**: `http://localhost:5000/api` (development)
- **Auth Strategy**: JWT Bearer tokens (access + refresh tokens)
- **Key Endpoints** (to be wired):
  - `POST /api/auth/login` — Login
  - `POST /api/auth/register` — Register
  - `POST /api/auth/refresh` — Refresh token
  - `GET /api/products` — List products (supports pagination, filtering)
  - `GET /api/products/{id}` — Product detail
  - `GET /api/categories` — Product categories
  - `POST /api/orders` — Create order
  - `GET /api/orders` — User order history
  - `GET /api/orders/{id}` — Order detail
  - `GET /api/users/profile` — Current user profile
  - `PUT /api/users/profile` — Update profile
  - `GET /api/blogs` — Blog posts
  - `GET /api/notifications` — User notifications
  - `GET /api/stores` — Store locations
  - `GET /api/loyalty/points` — Loyalty points balance
  - `GET /api/reviews?productId={id}` — Product reviews
  - `POST /api/reviews` — Submit review
  - `GET /api/blogs/{id}/comments` — Blog comments
  - `POST /api/blogs/{id}/comments` — Add blog comment
  - `POST /api/feedback` — Submit general feedback
  - **Admin Endpoints**:
  - `GET /api/admin/products` — Admin product list
  - `POST /api/admin/products` — Create product
  - `PUT /api/admin/products/{id}` — Update product
  - `DELETE /api/admin/products/{id}` — Delete product
  - `GET /api/admin/users` — Admin user list
  - `GET /api/admin/orders` — Admin order list
  - `PUT /api/admin/orders/{id}/status` — Update order status
  - `GET /api/admin/reports` — Dashboard statistics
  - `GET /api/admin/promotions` — Coupons & rewards
  - `POST /api/admin/promotions` — Create coupon/reward

## Environment Variables
```env
NEXT_PUBLIC_API_BASE_URL=http://localhost:5000/api
NEXT_PUBLIC_GOOGLE_MAPS_KEY=your_google_maps_api_key
NEXT_PUBLIC_APP_URL=http://localhost:3000
```

## Commands
```bash
pnpm dev        # Start dev server (Turbopack)
pnpm build      # Production build
pnpm start      # Start production server
pnpm lint       # Run ESLint
```

## Project Structure
```
src/
├── app/                    # Next.js App Router pages
│   ├── admin/             # Admin panel (separate layout, sidebar nav)
│   │   ├── layout.tsx     # Admin layout (auth guard, sidebar, header)
│   │   ├── login/         # Admin login page
│   │   ├── products/      # Product CRUD, category management
│   │   ├── users/         # User management, detail views
│   │   ├── orders/        # Order management, invoice
│   │   ├── reports/       # Dashboard stats, charts (Recharts)
│   │   └── promotions/    # Coupons, rewards, banner campaigns
│   ├── layout.tsx         # Root layout (providers, fonts)
│   └── globals.css        # Tailwind + CSS variables
├── components/
│   ├── ui/                # Reusable UI primitives (button, input, card, etc.)
│   ├── layout/            # Header, Footer, navigation
│   ├── home/              # Home page sections (hero, product listing, etc.)
│   ├── reviews/           # Product reviews, blog comments, store ratings
│   ├── admin/             # Admin components (sidebar, data-table, stat-card, etc.)
│   └── notifications/     # Notification bell and list
├── data/                  # Dummy/mock data (products, blogs, orders, etc.)
│   └── admin/             # Admin-specific data (users, statistics, promotions, orders)
├── stores/                # Zustand stores (cart, wishlist, auth, notifications)
├── i18n/                  # next-intl config and message files
│   ├── request.ts
│   └── messages/          # en.json, vi.json
├── lib/                   # Utilities (cn, constants, helpers)
└── types/                 # Shared TypeScript types
```

## Conventions
- Components use `"use client"` directive only when they need client-side interactivity
- CSS uses oklch color space with CSS variables for theming
- Dark mode: class-based via `next-themes` (`.dark` class on `<html>`)
- All UI strings externalized in i18n message files
- Zustand stores use `persist` middleware for client-side persistence
- Mock data is structured to match expected API response shapes for easy replacement
- Image paths use `/images/...` placeholders (to be replaced with real assets or CDN URLs)
