# Nova Launch Roadmap

> **Goal**: Launch Nova as a monetized, differentiated AI learning coach that feels like a colleague who knows you.

> **Database Policy**: All database changes are made through **EF Core migrations only**. We NEVER execute raw SQL.

---

## The Vision

Nova isn't another ChatGPT wrapper. It's a personalized learning coach that:
- Knows your background, goals, and learning style
- Remembers your journey across sessions
- Tracks your skills and identifies gaps
- Guides you with curated knowledge (RAG)
- Adapts to how YOU learn best

---

## Launch Phases Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                              │
│   ALPHA              BETA               PUBLIC             GROWTH            │
│   ─────              ────               ──────             ──────            │
│   50 Pulse users     Waitlist           Open access        Advanced          │
│   Free               Paid               Paid               Premium tiers     │
│   Feedback focus     Validate pricing   Scale              Expand            │
│                                                                              │
│   After Phase 1      After Phase 2      After Phase 3      Ongoing           │
│   Personalization    + Memory           + Skills/RAG       + Adaptive        │
│                      + Payments                                              │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Phase 1: Personalization (Alpha Prerequisite)

**Goal**: Cody knows who you are and tailors responses accordingly.

### Deliverables

| Item | Description |
|------|-------------|
| `LearnerProfile` entity | Role, experience, tech stack, goals, learning style |
| Nova settings page | `/nova/settings` with profile form |
| Sidebar integration | Profile summary + gear icon in sidebar footer |
| System prompt injection | Profile data included in every Cody response |
| Conversation summaries | Recent session context for continuity |

### What Users Experience

- Cody references their tech background
- Explanations match their experience level
- Learning style preferences respected
- Feels personalized, not generic

### Alpha Release Criteria

- [ ] Learning profile can be created and edited
- [ ] Cody's responses reflect profile data
- [ ] At least 10 internal test conversations feel "personalized"
- [ ] No critical bugs in chat flow

---

## Alpha Release

### Access Model

```
WHO:     50 existing Pulse users (hand-picked)
COST:    Free
期間:    4-6 weeks
PURPOSE: Feedback, bug discovery, validate personalization value
```

### Invitation Strategy

1. Email Pulse users with Nova early access offer
2. Require feedback commitment (survey after 2 weeks)
3. Create `#nova-alpha` channel for direct feedback
4. Track usage metrics (conversations/user, return rate)

### Success Metrics

| Metric | Target |
|--------|--------|
| Activation rate | >80% try Nova within 1 week |
| Return rate | >50% return within 2 weeks |
| Profile completion | >70% complete learning profile |
| NPS score | >40 |
| "Feels personalized" survey | >60% agree |

### Feedback Collection

- In-app feedback button (thumbs up/down on responses)
- Weekly survey (5 questions max)
- Direct Discord/Slack channel
- Usage analytics (anonymous)

---

## Phase 2: Memory Layer

**Goal**: Cody remembers your journey and references it naturally.

### Deliverables

| Item | Description |
|------|-------------|
| `Memory` entity | Facts, breakthroughs, struggles, preferences |
| Extraction pipeline | Background job extracts memories from conversations |
| Embedding storage | Vector column for semantic search |
| Memory retrieval | Relevant memories injected into context |
| Memory management UI | View/delete memories (privacy) |

### What Users Experience

- "Remember when we discussed async last week?"
- Cody recalls breakthroughs and struggles
- No need to re-explain context
- Feels like a colleague relationship

---

## Payments Infrastructure (Parallel Track)

**Scope**: Ecosystem-wide billing system for all Codewrinkles apps.

### Schema Design

```
billing schema (implement as EF Core entities + migrations)

billing.Customers
├── Id (GUID, PK)
├── IdentityId (GUID, FK → identity.Identities, unique)
├── StripeCustomerId (string, unique)
├── Email (string)
├── Name (string?)
├── CreatedAt (DateTimeOffset)
└── UpdatedAt (DateTimeOffset)

billing.Subscriptions
├── Id (GUID, PK)
├── CustomerId (GUID, FK → Customers)
├── StripeSubscriptionId (string, unique)
├── PlanId (string)                    -- "nova_pro_monthly", "nova_pro_yearly"
├── Status (string)                    -- active, canceled, past_due, trialing
├── CurrentPeriodStart (DateTimeOffset)
├── CurrentPeriodEnd (DateTimeOffset)
├── CancelAtPeriodEnd (bool)
├── CreatedAt (DateTimeOffset)
└── UpdatedAt (DateTimeOffset)

billing.PaymentMethods
├── Id (GUID, PK)
├── CustomerId (GUID, FK → Customers)
├── StripePaymentMethodId (string)
├── Type (string)                      -- card, sepa_debit, etc.
├── Last4 (string?)
├── Brand (string?)                    -- visa, mastercard, etc.
├── ExpiryMonth (int?)
├── ExpiryYear (int?)
├── IsDefault (bool)
└── CreatedAt (DateTimeOffset)

billing.Invoices
├── Id (GUID, PK)
├── CustomerId (GUID, FK → Customers)
├── StripeInvoiceId (string, unique)
├── AmountDue (decimal)
├── AmountPaid (decimal)
├── Currency (string)
├── Status (string)                    -- draft, open, paid, void, uncollectible
├── InvoiceUrl (string?)
├── PdfUrl (string?)
├── PeriodStart (DateTimeOffset)
├── PeriodEnd (DateTimeOffset)
└── CreatedAt (DateTimeOffset)
```

### Stripe Integration

```
Backend Components:
├── StripeService                      -- API wrapper
├── WebhookController                  -- Handle Stripe events
├── BillingRepository                  -- Data access
└── Endpoints
    ├── POST /api/billing/checkout     -- Create checkout session
    ├── POST /api/billing/portal       -- Customer portal session
    ├── GET  /api/billing/subscription -- Current subscription
    └── POST /api/billing/webhook      -- Stripe webhooks

Webhook Events to Handle:
├── checkout.session.completed         -- New subscription
├── customer.subscription.created
├── customer.subscription.updated      -- Plan changes
├── customer.subscription.deleted      -- Cancellation
├── invoice.paid                       -- Successful payment
├── invoice.payment_failed             -- Failed payment
└── customer.updated                   -- Customer info changes
```

### Settings UI (Ecosystem-Wide)

New section in global settings at `/settings/billing`:

```
┌─────────────────────────────────────────────────────────────────┐
│  Billing & Subscription                                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Current Plan                                                    │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │  Nova Pro                                    $15/month  │    │
│  │  ✓ Unlimited conversations                              │    │
│  │  ✓ Memory persistence                                   │    │
│  │  ✓ Skill tracking                                       │    │
│  │                                                         │    │
│  │  Next billing: January 15, 2025                         │    │
│  │                                                         │    │
│  │  [Manage Subscription]  [Cancel]                        │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
│  Payment Method                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │  💳 Visa ending in 4242          Expires 12/25          │    │
│  │                                           [Update]      │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
│  Billing History                                                 │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │  Dec 15, 2024    Nova Pro Monthly    $15.00    [PDF]    │    │
│  │  Nov 15, 2024    Nova Pro Monthly    $15.00    [PDF]    │    │
│  │  Oct 15, 2024    Nova Pro Monthly    $15.00    [PDF]    │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Gating Logic

```csharp
// Check subscription status before Nova features
public interface ISubscriptionService
{
    Task<bool> HasActiveSubscriptionAsync(Guid identityId, string product);
    Task<SubscriptionTier> GetTierAsync(Guid identityId, string product);
}

public enum SubscriptionTier
{
    Free,
    Pro,
    Lifetime
}

// In Nova endpoints
if (await subscriptionService.GetTierAsync(identityId, "nova") == SubscriptionTier.Free)
{
    // Check conversation limits
    var count = await novaRepository.GetMonthlyConversationCountAsync(profileId);
    if (count >= FreeTierLimits.ConversationsPerMonth)
    {
        return Results.Json(new { error = "upgrade_required", limit = "conversations" }, statusCode: 402);
    }
}
```

---

## Beta Release

### Access Model

```
WHO:     Waitlist signups + Alpha graduates
COST:    Paid (with free tier option)
PURPOSE: Validate monetization, refine pricing
```

### Prerequisites

- [ ] Phase 2 (Memory) complete
- [ ] Payments infrastructure live
- [ ] Free tier limits implemented
- [ ] Upgrade flow tested end-to-end

### Pricing (Initial)

| Plan | Price | Includes |
|------|-------|----------|
| **Free** | $0 | 10 conversations/month, basic profile, no memory |
| **Pro Monthly** | $15/mo | Unlimited, full memory, skill tracking |
| **Pro Yearly** | $120/yr | Same as monthly, 2 months free |
| **Lifetime** | $200 | Everything forever, limited to first 100 |

### Lifetime Deal Strategy

- **Why offer it**: Cash flow boost, early adopter loyalty, marketing buzz
- **Risk mitigation**: Cap at 100 purchases, clearly communicate "early adopter" pricing
- **Future-proofing**: Lifetime = current features, major new products may be separate

### Waitlist Page

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                  │
│              🚀 Nova is coming to public beta                    │
│                                                                  │
│     An AI learning coach that actually knows you.                │
│                                                                  │
│     ✓ Remembers your journey                                     │
│     ✓ Adapts to your learning style                              │
│     ✓ Tracks your skill progress                                 │
│                                                                  │
│     ┌─────────────────────────────────────────────────────┐     │
│     │  Enter your email                        [Join]     │     │
│     └─────────────────────────────────────────────────────┘     │
│                                                                  │
│     🎉 327 developers on the waitlist                            │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## Phase 3: Skills + RAG

**Goal**: Cody knows what you know and fills gaps with curated knowledge.

### Deliverables

| Item | Description |
|------|-------------|
| Skill taxonomy | Hierarchical concepts (languages, frameworks, patterns) |
| Prerequisite graph | Which concepts require which |
| Skill tracking | Bayesian knowledge tracing per user |
| RAG pipeline | Curated learning content indexed |
| Gap identification | "You know X but haven't covered Y" |

### RAG Content Strategy

```
Knowledge Sources (Curated, Not Generic Web):
├── Official documentation (Microsoft Learn, MDN, etc.)
├── Codewrinkles content (YouTube transcripts, blog posts)
├── Curated articles (hand-picked quality content)
├── Code examples (working, tested snippets)
└── Common pitfalls & solutions (experience-based)

NOT Included:
├── Random Stack Overflow answers
├── Outdated blog posts
└── Unverified code snippets
```

### Knowledge Graph Consideration

For skill prerequisites, evaluate SQL Server graph features vs dedicated graph DB:

```sql
-- SQL Server graph (built-in, no extra infrastructure)
CREATE TABLE nova.Concepts AS NODE;
CREATE TABLE nova.Requires AS EDGE;

-- Query prerequisites
SELECT Prereq.Name
FROM nova.Concepts AS Concept, nova.Requires, nova.Concepts AS Prereq
WHERE MATCH(Concept-(Requires)->Prereq)
AND Concept.Id = 'clean-architecture';
```

**Decision point**: If graph queries become complex or slow, consider Neo4j.

---

## Public Launch

### Access Model

```
WHO:     Open to all
COST:    Freemium (free tier + paid plans)
PURPOSE: Growth, revenue, market validation
```

### Launch Checklist

- [ ] Phase 3 complete (Skills + RAG)
- [ ] Pricing validated in Beta
- [ ] Landing page polished
- [ ] Testimonials from Alpha/Beta users
- [ ] Product Hunt launch prepared
- [ ] Content marketing ready (blog posts, videos)

### Marketing Channels

| Channel | Action |
|---------|--------|
| Product Hunt | Launch day campaign |
| Twitter/X | Thread on the journey, demos |
| YouTube | Codewrinkles video on Nova |
| Dev communities | Reddit, Discord, HN |
| Email | Announce to Pulse users + waitlist |

---

## Phase 4: Adaptive Coaching

**Goal**: Cody proactively guides your learning journey.

### Deliverables

| Item | Description |
|------|-------------|
| Learning paths | Goal → prerequisite → path generation |
| Spaced repetition | Resurface concepts at optimal times |
| Proactive suggestions | "Ready to learn X next?" |
| Progress dashboard | Visual skill tree, achievements |

### Premium Features (Future Tier?)

- Advanced analytics
- Team/organization features
- Custom knowledge bases
- API access

---

## Implementation Tracks

### Track A: Nova Features

```
Week 1-3:   Phase 1 (Personalization)
Week 4:     ──────── ALPHA RELEASE ────────
Week 5-8:   Phase 2 (Memory Layer)
Week 9:     ──────── BETA RELEASE ─────────
Week 10-14: Phase 3 (Skills + RAG)
Week 15:    ──────── PUBLIC LAUNCH ────────
Week 16+:   Phase 4 (Adaptive)
```

### Track B: Payments (Parallel)

```
Week 4-5:   Stripe account setup, API integration
Week 5-6:   billing schema, EF entities, migration
Week 6-7:   Checkout flow, webhook handling
Week 7-8:   Settings UI (/settings/billing)
Week 8:     Gating logic in Nova
Week 9:     ──────── PAYMENTS LIVE ────────
```

### Track C: Marketing (Parallel)

```
Week 1:     Waitlist page live
Week 4:     Alpha announcement to Pulse users
Week 8:     Beta waitlist opens
Week 9:     Beta access emails
Week 14:    Launch prep (PH, content)
Week 15:    Public launch campaign
```

---

## Revenue Projections (Rough)

Assuming 50 Alpha → 200 Beta → 1000 Public users:

| Phase | Users | Conversion | Paying | MRR |
|-------|-------|------------|--------|-----|
| Alpha | 50 | 0% (free) | 0 | $0 |
| Beta | 200 | 30% | 60 | $900 |
| Launch | 1000 | 20% | 200 | $3,000 |
| +6 months | 3000 | 20% | 600 | $9,000 |

Plus lifetime deals: 100 × $200 = $20,000 one-time

---

## Open Questions

1. **Stripe vs Lemon Squeezy?**
   - Stripe: Industry standard, more control
   - Lemon Squeezy: Handles EU VAT, simpler

2. **Free tier limits?**
   - 10 conversations/month? 5?
   - Memory persistence in free tier?

3. **Lifetime deal?**
   - Offer it? Cap at what number?
   - Price point: $150? $200? $300?

4. **Team plans?**
   - Future consideration or MVP scope?

5. **Content for RAG?**
   - Start with Codewrinkles content only?
   - License external content?

---

## Success Metrics

### Product Metrics

| Metric | Alpha Target | Beta Target | Launch Target |
|--------|--------------|-------------|---------------|
| Activation (try within 7d) | 80% | 60% | 40% |
| Retention (return in 14d) | 50% | 40% | 30% |
| Profile completion | 70% | 60% | 50% |
| Conversations/user/week | 3 | 5 | 5 |

### Business Metrics

| Metric | Beta Target | Launch Target |
|--------|-------------|---------------|
| Free → Paid conversion | 30% | 20% |
| Monthly churn | <10% | <8% |
| MRR | $500 | $3,000 |
| NPS | >40 | >50 |

---

## References

- [Nova Personalization Roadmap](./nova-personalization-roadmap.md) - Technical details for Phases 1-4
- [Stripe Docs](https://stripe.com/docs) - Payments integration
- [Lemon Squeezy](https://www.lemonsqueezy.com/) - Alternative payment processor

---

**Last Updated**: 2024-12-16
