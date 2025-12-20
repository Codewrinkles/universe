# Nova: Alpha to Launch Roadmap

> **Purpose**: Strategic roadmap combining GTM and technical implementation to take Nova from Alpha (18 users) to Beta to Launch with real pricing.

> **Last Updated**: December 20, 2025

---

## 1. Current State Analysis

### Your Audience

| Channel | Size | Reached Yet? | Notes |
|---------|------|--------------|-------|
| YouTube | 31k subs | **Not yet** | Biggest asset, untapped |
| LinkedIn | 18k followers | Yes | Professional reach |
| X/Twitter | 4.5k followers | Yes | Tech community |
| Substack | 370 subscribers | ? | Highest intent |
| Pulse | ? active users | Yes | Owned platform |

**Channels reached so far**: LinkedIn, X, Pulse (~23k)
**Alpha applications**: 18

### Analysis: 18 Applications is Actually Solid

With YouTube (31k) not yet reached, the 18 applications came from ~23k across LinkedIn, X, and Pulse. This is a reasonable first result:

- **Application form was intentionally designed with friction** to filter for committed Alpha testers (quality over quantity)
- **Single announcement per channel** - conversion typically takes 2-4 weeks and multiple touchpoints
- **No video content yet** - your strongest medium hasn't been used

### What's Working

- Personal emails to accepted users (founder-led, builds loyalty)
- Application form filters for serious testers
- Multi-channel announcement
- The 18 who applied are likely high-quality, committed users

### The Opportunity

Your biggest audience (31k YouTube subscribers) hasn't been reached yet. This is your primary lever for Phase 1.

---

## 2. The Strategic Framework

### Research-Backed Insights

From [GTM Strategist](https://knowledge.gtmstrategist.com/p/how-ai-products-go-to-market): Quiet pre-launches with free access in exchange for feedback, combined with referrals, can drive 35%+ retention - a strong early signal of product-market fit.

From [Boldstart Ventures](https://medium.com/boldstart-ventures/clearing-the-3-biggest-dev-tool-activation-hurdles-1cbc9e0c3063): Activation is the "A-ha moment" - if users sign up and don't get to core value, there's little chance of conversion. Developer tools suffer from long Time-To-Value (TTV).

From [ProductLed on YouTube Strategy](https://productled.com/blog/youtube-video-marketing-strategy-for-saas): Education first, selling second. Create content that solves real problems. Product comparison videos capture high-intent searches.

From [Bain Capital on AI Pricing](https://baincapitalventures.com/insight/pricing-your-early-stage-b2b-ai-saas-product-a-framework-not-a-formula/): AI reintroduces marginal costs. Freemium that gives away too much can hurt revenue. Usage-based and outcome-based pricing growing at same pace as subscriptions.

From [Common Room on Community-Led Growth](https://www.commonroom.io/resources/ultimate-guide-to-community-led-growth/): CLG acts as a multiplier on product-led growth. Notion achieved 95% organic traffic through community efforts.

From [Heavybit on Founder-Led Sales](https://www.heavybit.com/library/article/founder-led-sales-strategy): Founders should lead early sales - they understand the product, market, and vision better than anyone. It feels special for the buyer to talk directly to the founder.

---

## 3. The Three-Phase Roadmap

```
┌─────────────────────────────────────────────────────────────────┐
│  PHASE 1: ALPHA ACTIVATION                                      │
│  Goal: Get 50 active users, prove personalization value         │
│  Duration: 4-6 weeks                                            │
│  Pricing: Free                                                  │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  PHASE 2: PRIVATE BETA                                          │
│  Goal: 200 users, validate pricing, build social proof          │
│  Duration: 6-8 weeks                                            │
│  Pricing: Pay-what-you-want or early-bird discount              │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  PHASE 3: PUBLIC LAUNCH                                         │
│  Goal: Sustainable growth, real pricing                         │
│  Pricing: Freemium + Pro ($12-15/month)                         │
└─────────────────────────────────────────────────────────────────┘
```

---

## 4. Phase 1: Alpha Activation (Now → +6 weeks)

### 4.1 The Core Problem to Solve

Your Alpha users need to hit the "A-ha moment" fast. For Nova, this is:
> "Cody remembers my background and adapts to where I am in my journey."

Current technical implementation supports this via:
- LearnerProfile (background, goals, preferences)
- Memory system (conversation context across sessions)
- Personalized system prompts

**But users need to FEEL this**, not just have it technically available.

### 4.2 GTM Actions (Weeks 1-2)

| Action | Channel | Effort | Expected Impact |
|--------|---------|--------|-----------------|
| **YouTube video: "I Built an AI Coach"** | YouTube | High | 5-10k views, 50+ applications |
| **Behind-the-scenes thread** | X | Low | Engagement, humanizes product |
| **Personal DMs to engaged followers** | LinkedIn/X | Medium | 10-20 high-quality applications |
| **Newsletter deep-dive** | Substack | Low | 50+ opens, 5-10 applications |
| **Pulse announcement with context** | Pulse | Low | Community buy-in |

#### YouTube Video Strategy

This is your unfair advantage. 31k subscribers who trust you for .NET content.

**Video Concept**: "I'm Building an AI Learning Coach for Developers"

Structure:
1. **Hook** (0:00-0:30): "What if your AI assistant remembered everything you've ever discussed?"
2. **Problem** (0:30-2:00): Current AI tools are stateless, generic, don't adapt
3. **Solution Demo** (2:00-6:00): Show Nova in action - profile setup, personalized response, memory recall
4. **The Vision** (6:00-7:00): Where Nova is going (skill tracking, plugins)
5. **CTA** (7:00-end): "I'm looking for 50 developers to test this. Link in description."

**Key**: This isn't a sales pitch. It's sharing what you're building with your community.

### 4.3 Technical Priorities (Aligned with GTM)

| Priority | Feature | Why Now | Effort |
|----------|---------|---------|--------|
| 1 | **First-message onboarding** | Reduce TTV - Cody should ask about background on first message | 1 day |
| 2 | **Memory visibility** | Show users "Here's what I remember about you" | 1-2 days |
| 3 | **SK Plugins (basic)** | Make Cody noticeably smarter with memory recall | 3-4 days |
| 4 | **Session title generation** | Better UX, easier to find past conversations | 0.5 day |

#### Technical: First-Message Onboarding

Instead of requiring users to fill out a profile form, Cody should proactively gather context:

```
User's first message: "How does async work in C#?"

Cody: "Before I dive in - I want to make sure I pitch this at the right level.
Quick context: what's your current role, and roughly how long have you
been writing C#? This helps me tailor explanations to where you are."
```

This is a **conversational onboarding** pattern that:
- Feels natural (not a form)
- Gathers LearnerProfile data organically
- Demonstrates personalization value immediately

#### Technical: Memory Visibility

Add a way for users to see what Cody remembers:
- A "What Cody knows about you" sidebar/section in the UI
- Or: Cody can say "Based on our past conversations, I remember you're working on [X] and prefer [Y] style explanations..."

This makes the personalization tangible, not invisible.

### 4.4 User Activation Checklist

Define your activation criteria. A user is "activated" when they:

- [ ] Complete LearnerProfile (conversationally or via form)
- [ ] Have 3+ conversations
- [ ] Return after 24 hours for another conversation
- [ ] Give feedback (survey or reply to personal email)

Track these metrics. Your Alpha isn't successful at 50 signups - it's successful at 30+ activated users.

### 4.5 Founder-Led Outreach

You've already started this (personal emails to accepted users). Double down:

1. **Reply personally** to every Alpha user's first few days of feedback
2. **Ask for 15-minute calls** with 5-10 users - understand their "A-ha" moment
3. **Segment your audience**: Who are the most engaged? What do they have in common?

From [Folk.app on Founder-Led Sales](https://www.folk.app/articles/founder-led-sales-101-actionable-strategies-for-early-stage-startup-founders): Focus on relationships, not immediate sales. Understand pain points.

---

## 5. Phase 2: Private Beta (Week 7 → Week 14)

### 5.1 Entry Criteria

Move to Beta when:
- 40+ activated Alpha users
- 70%+ would be "disappointed" if Nova went away (PMF signal)
- At least 50% have had 5+ conversations
- Clear "A-ha moment" pattern identified

### 5.2 Beta Goals

| Goal | Metric | Target |
|------|--------|--------|
| Scale | Total users | 200 |
| Activation | Users with 5+ conversations | 60% |
| Willingness to pay | Survey: "Would you pay $12/month?" | 40%+ |
| Social proof | Testimonials collected | 10+ |

### 5.3 GTM Actions

| Action | Channel | Effort | Expected Impact |
|--------|---------|--------|-----------------|
| **YouTube: "What 50 Developers Taught Me"** | YouTube | High | Builds credibility, drives Beta signups |
| **Testimonial tweets** | X | Low | Social proof |
| **Beta waitlist with referral bonus** | Landing page | Medium | Viral loop |
| **Guest on .NET podcasts** | Podcasts | Medium | Reach new audience |
| **LinkedIn thought leadership** | LinkedIn | Medium | Professional credibility |

#### Beta Pricing Strategy

From [Valueships on AI Pricing](https://www.valueships.com/post/ai-pricing-8-biggest-saas-trends-in-2025): AI SaaS can't afford generous freemium due to marginal costs. Usage-based and outcome-based models are growing.

**Recommendation for Beta**: Pay-What-You-Want or Early-Bird

```
Beta Pricing Options:

Option A: Pay-What-You-Want ($0-50/month)
- Signals value without forcing price point
- Collects data on willingness to pay
- Risk: Anchors expectations low

Option B: Early-Bird Discount (50% off launch price)
- "Beta testers get $6/month for life (normally $12)"
- Creates urgency
- Locks in revenue early
- Risk: Might leave money on table

Option C: Token-Based (e.g., 1000 messages free, then pay)
- Aligns cost with usage
- Lets power users self-select
- Risk: Complexity, billing infrastructure needed

Recommendation: Option B (Early-Bird) for Beta
```

### 5.4 Technical Priorities (Beta)

| Priority | Feature | Why Now | Effort |
|----------|---------|---------|--------|
| 1 | **SK Plugins (full)** | Verification, memory recall, profile lookup | 1 week |
| 2 | **Skill Tracking (MVP)** | Differentiate from generic AI, key value prop | 2 weeks |
| 3 | **Usage tracking** | Know who's active, predict churn | 1 week |
| 4 | **Billing infrastructure** | Stripe integration, subscription management | 1 week |

### 5.5 Community Building

Pulse is your owned community platform. Use it:

1. **#nova-feedback channel/hashtag** - Public feedback loop
2. **Weekly "What did you learn with Nova?"** - User-generated content
3. **Beta user badges** - Visible status, gamification
4. **Nova tips from power users** - Let users teach each other

From [Disco.co on Community-Led Growth](https://www.disco.co/blog/harnessing-the-power-of-community-led-growth-in-2023): Notion maximized a community flywheel with user-generated templates featured on their site.

---

## 6. Phase 3: Public Launch (Week 15+)

### 6.1 Entry Criteria

Move to Launch when:
- 150+ Beta users retained after 30 days
- 40%+ indicated willingness to pay $12/month
- 10+ strong testimonials
- Activation flow proven (70%+ completion)
- Billing infrastructure tested

### 6.2 Pricing Model

```
┌─────────────────────────────────────────────────────────────────┐
│  NOVA PRICING                                                   │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  FREE                    PRO                    LIFETIME        │
│  $0/month               $12/month              $199 one-time    │
│                                                                 │
│  - 10 messages/day      - Unlimited messages   - Unlimited      │
│  - Basic memory         - Full memory          - Full memory    │
│  - 7-day history        - Unlimited history    - Unlimited      │
│  - No skill tracking    - Skill tracking       - Skill tracking │
│                         - Priority support     - Priority       │
│                         - Early features       - Early features │
│                                                - Founding badge │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

**Why this structure:**

- **Free tier**: Acquisition funnel, lets users hit "A-ha" before paying
- **Pro at $12/month**: Affordable for individuals, covers AI costs + margin
- **Lifetime at $199**: ~17 months breakeven, attracts committed users, upfront cash

**Founding Member perks**: Alpha + early Beta users get:
- Lifetime discount (e.g., $8/month instead of $12)
- "Founding Member" badge on profile
- Priority feature requests

### 6.3 Launch GTM

| Action | Channel | Effort | Impact |
|--------|---------|--------|--------|
| **YouTube: "Nova is Live"** | YouTube | High | Biggest reach |
| **Product Hunt launch** | Product Hunt | High | New audience, PR |
| **LinkedIn carousel** | LinkedIn | Medium | Professional reach |
| **Twitter/X thread** | X | Medium | Tech community |
| **Newsletter announcement** | Substack | Low | Highest conversion |
| **Reddit (r/dotnet, r/csharp)** | Reddit | Medium | Targeted developers |

---

## 7. Content Strategy (Ongoing)

### Your Unfair Advantage

You have 31k YouTube subscribers who trust you for .NET education. Nova is a natural extension of that mission.

From [ProductLed](https://productled.com/blog/youtube-video-marketing-strategy-for-saas): Education first, selling second. When viewers learn valuable skills from your content, they associate your brand with expertise.

### Content Flywheel

```
YouTube (Deep dives)
     │
     │ "Learn more with Nova →"
     ▼
Landing Page / Nova
     │
     │ "Share your learning on Pulse →"
     ▼
Pulse (Community)
     │
     │ "Best pulses featured in newsletter →"
     ▼
Newsletter (Retention)
     │
     │ "New video on [topic] →"
     ▼
YouTube (Loop continues)
```

### Video Ideas That Drive Nova Signups

| Video Title | Hook | CTA |
|-------------|------|-----|
| "Why AI Coding Assistants Aren't Making You Better" | Problem with code completion vs learning | "I'm building something different..." |
| "How I'd Learn .NET in 2025 (With AI)" | Learning path with AI assistance | "Try Nova to follow this path" |
| "The Problem With ChatGPT for Developers" | Stateless, forgets context, generic | "Nova remembers your journey" |
| "Behind the Scenes: Building Nova" | Tech deep dive (React, .NET, OpenAI) | "Help me test it" |
| "What 100 Developers Taught Me About Learning" | Insights from Beta users | Social proof → signups |

### LinkedIn Strategy

Your 18k followers are professionals. Different content:

- **Thought leadership**: "What I've learned building an AI product"
- **Behind-the-scenes**: Metrics, challenges, decisions
- **User stories**: "A developer with 15 years of experience said..."
- **Polls**: "How do you learn new technologies?" (engagement + data)

---

## 8. Metrics Dashboard

### Alpha Metrics (Phase 1)

| Metric | Current | Target | How to Track |
|--------|---------|--------|--------------|
| Applications | 18 | 50 | Database |
| Activated users | ? | 40 | Profile complete + 3 convos |
| Avg conversations/user | ? | 5 | Database |
| Return rate (D7) | ? | 60% | Analytics |
| NPS | ? | 40+ | Survey |

### Beta Metrics (Phase 2)

| Metric | Target | How to Track |
|--------|--------|--------------|
| Total users | 200 | Database |
| Activation rate | 60% | Defined criteria |
| D30 retention | 50% | Analytics |
| Willingness to pay | 40%+ | Survey |
| Testimonials | 10+ | Manual collection |

### Launch Metrics (Phase 3)

| Metric | Target | How to Track |
|--------|--------|--------------|
| Free signups | 500+ | Database |
| Free → Pro conversion | 5-10% | Stripe |
| MRR | $500+ | Stripe |
| Churn (monthly) | <5% | Stripe |
| LTV/CAC | 3:1+ | Calculated |

---

## 9. Technical Roadmap Aligned with GTM

### Phase 1: Alpha (Technical)

```
Week 1-2:
├── [ ] First-message conversational onboarding
├── [ ] Memory visibility in UI ("What Cody knows")
└── [ ] Session title generation improvements

Week 3-4:
├── [ ] SK Plugins: MemoryPlugin (recall_memories)
├── [ ] SK Plugins: ProfilePlugin (get_learning_context)
└── [ ] Basic usage analytics (messages/user, sessions/user)

Week 5-6:
├── [ ] Alpha user feedback survey integration
├── [ ] SK Plugins: VerificationPlugin (technical accuracy)
└── [ ] Performance optimizations (streaming, latency)
```

### Phase 2: Beta (Technical)

```
Week 7-10:
├── [ ] Skill Tracking MVP (Concept entity, UserSkillState)
├── [ ] Skill-aware prompting (adjust explanation depth)
├── [ ] Skill signal extraction from conversations
└── [ ] User skill progress dashboard

Week 11-14:
├── [ ] Stripe integration (subscriptions)
├── [ ] Free tier rate limiting
├── [ ] Usage tracking + billing alignment
├── [ ] Upgrade prompts in product
└── [ ] Email: Trial ending, upgrade reminders
```

### Phase 3: Launch (Technical)

```
Week 15+:
├── [ ] Full skill taxonomy seeding
├── [ ] Learning path recommendations
├── [ ] Export data (compliance)
├── [ ] Team/organization features (future)
└── [ ] API access (future)
```

---

## 10. Immediate Action Items (Next 7 Days)

### The Main Lever: YouTube

Your biggest untapped audience is YouTube (31k). One video announcing Nova to this audience is likely to generate significantly more applications than all other channels combined.

**Suggested approach:**
- **[ ] Write + record YouTube video** "I'm Building an AI Learning Coach for Developers"
- Frame it as sharing what you're building with your community, not a sales pitch
- CTA: "I'm looking for 50 developers to test this. Link in description."

### Supporting Actions (Optional)

These can amplify the YouTube video but aren't required:

- **[ ] Newsletter to Substack** (if not already sent) - highest intent audience
- **[ ] Schedule a few user calls** with current Alpha users to learn what's resonating

### Technical Actions (Your Choice)

Based on what you want to prioritize:

| Option | Purpose | Effort |
|--------|---------|--------|
| First-message onboarding | Reduce time-to-value for new users | 1 day |
| SK Plugins | Make Cody smarter with memory/profile lookup | 3-4 days |
| Analytics | Understand user behavior | 1 day |

You know the product and users better than I do. Pick what feels most impactful.

---

## 11. Key Risks and Mitigations

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Low activation rate | Medium | High | Improve onboarding, shorten TTV |
| Users don't perceive personalization | Medium | High | Make memory/profile visible in UI |
| AI costs exceed revenue | Medium | Medium | Rate limiting, usage-based pricing |
| Competition (ChatGPT, Claude) | High | Medium | Double down on differentiation (memory, skill tracking) |
| YouTube video underperforms | Low | Medium | Have backup content plan, iterate |

---

## 12. Success Criteria

### Phase 1 Success (Alpha)
- [ ] 50 applications
- [ ] 40 activated users
- [ ] 70% would be "disappointed" if Nova went away
- [ ] 5 user calls completed with insights documented

### Phase 2 Success (Beta)
- [ ] 200 users
- [ ] 60% activation rate
- [ ] 40% willingness to pay $12/month
- [ ] 10 publishable testimonials
- [ ] Billing infrastructure working

### Phase 3 Success (Launch)
- [ ] 500 free signups in first month
- [ ] 5-10% free → paid conversion
- [ ] $500+ MRR
- [ ] <5% monthly churn
- [ ] Positive unit economics (LTV > CAC)

---

## Appendix: Research Sources

- [How AI Products Go-to-Market - GTM Strategist](https://knowledge.gtmstrategist.com/p/how-ai-products-go-to-market)
- [2025 GTM Playbook for AI Startups - GTMfusion](https://www.gtmfusion.com/insights/2025-gtnm-playbook)
- [Three Steps to GTM Strategy for AI Software - Gartner](https://www.gartner.com/en/digital-markets/insights/gtm-strategy-for-ai-software)
- [Clearing the 3 Biggest Dev Tool Activation Hurdles - Boldstart Ventures](https://medium.com/boldstart-ventures/clearing-the-3-biggest-dev-tool-activation-hurdles-1cbc9e0c3063)
- [YouTube Video Marketing Strategy for SaaS - ProductLed](https://productled.com/blog/youtube-video-marketing-strategy-for-saas)
- [SaaS YouTube Marketing Strategy 2025 - Margin Business](https://marginbusiness.com/saas-youtube-marketing-strategy-in-2025-10x-your-saas-customers-through-youtube/)
- [AI Pricing: 8 Biggest SaaS Trends in 2025 - Valueships](https://www.valueships.com/post/ai-pricing-8-biggest-saas-trends-in-2025)
- [Pricing Your Early-Stage B2B AI SaaS - Bain Capital Ventures](https://baincapitalventures.com/insight/pricing-your-early-stage-b2b-ai-saas-product-a-framework-not-a-formula/)
- [Ultimate Guide to Community-Led Growth - Common Room](https://www.commonroom.io/resources/ultimate-guide-to-community-led-growth/)
- [Community-Led Growth: The Future - Disco.co](https://www.disco.co/blog/harnessing-the-power-of-community-led-growth-in-2023)
- [Founder-Led Sales 101 - Folk.app](https://www.folk.app/articles/founder-led-sales-101-actionable-strategies-for-early-stage-startup-founders)
- [How to Build a Founder-Led Sales Strategy - Heavybit](https://www.heavybit.com/library/article/founder-led-sales-strategy)
- [Developer-Led Growth - Daily.dev](https://business.daily.dev/resources/plg-for-developers-or-dlg-developer-led-growth-what-do-you-need-to-know)

---

**Document Status**: Ready for review and execution
