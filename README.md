# Codewrinkles Universe

> **Building in public**: A modern ecosystem of interconnected apps for content creators and learners.

---

## What is this?

Codewrinkles Universe is a unified platform that rethinks how we create, share, and consume content. Instead of isolated apps competing for attention through algorithmic manipulation, we're building an ecosystem where apps work together to provide genuine value.

### The Apps

**Pulse** - Microblogging, redefined
- Your followers actually see your posts (chronological feed, no algorithm)
- Discover new voices through genuine social proof, not virality metrics
- Built for meaningful conversations, not engagement farming
- **[Try it now](https://codewrinkles.com)** (Beta)

**Nova** - Learning, reimagined *(coming soon)*
- AI-powered learning companion
- Chat-first interface for exploring topics
- Personalized learning paths that adapt to you

**More to come** - The ecosystem is just getting started

---

## Why build this?

Modern platforms optimize for engagement, not value. Followers don't see your content. Algorithms promote whatever triggers emotional responses. Quality content gets buried under viral noise.

We're building something different:
- **Pulse** respects the follow relationship - if someone follows you, they see your content
- **Nova** surfaces knowledge based on relevance, not clickbait
- **The ecosystem** connects apps to create compounding value

---

## Building in Public

This is an experiment in transparent product development. I'm documenting the journey of building a startup as a CTO, sharing lessons about:
- Product architecture decisions
- Technical implementation
- Community building
- Balancing vision with pragmatism

Follow along on:
- **[Pulse](https://codewrinkles.com)**
- [YouTube - Codewrinkles](https://youtube.com/@codewrinkles)
- [LinkedIn](https://www.linkedin.com/in/dan-patrascu-baba-08b78523/)
- [Substack](https://architecttocto.substack.com/)

---

## Tech Stack

**Frontend**
- React 18 + TypeScript (strict mode)
- Vite
- TailwindCSS (custom design system)
- React Router

**Backend**
- .NET 10 / ASP.NET Core Minimal APIs
- C# 13
- Clean Architecture (layered monolith)
- Entity Framework Core 10
- SQL Server
- JWT authentication with refresh tokens
- OpenTelemetry + Azure Application Insights

---

## Current Status

**Pulse** - Beta

The core microblogging experience is live and functional. We're actively iterating based on feedback and adding polish.

**Nova** - In Design

Check [core.md](./core.md) for the product vision and roadmap.

---

## Want to Contribute?

Not accepting code contributions yet - still in early solo development. But you can:
- ⭐ Star the repo to follow progress
- 👀 Watch for updates
- 💬 Share feedback via [GitHub Issues](https://github.com/Codewrinkles/universe/issues)

---

## For Developers

If you want to explore the codebase:

```bash
# Frontend
cd apps/frontend
npm install
npm run dev      # Dev server at http://localhost:5173
npm run lint     # Type check
npm run build    # Production build

# Backend
cd apps/backend
dotnet build     # Build solution
dotnet run --project src/Codewrinkles.API  # Run API
```

See [CLAUDE.md](./CLAUDE.md) for technical guidelines and architecture decisions.

---

## License

Proprietary - All rights reserved.

This is an open codebase (building in public), but not open source. You're welcome to explore and learn from the code, but please don't use it to build competing products.

---

## About

Built by [Dan @ Codewrinkles](https://youtube.com/@codewrinkles)

Topics I explore: .NET architecture, clean code, agentic AI, ultra-running, startup building.

Let's build something different together.
