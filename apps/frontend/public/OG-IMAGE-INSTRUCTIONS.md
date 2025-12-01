# Open Graph Image Setup

## Quick Instructions

Save the landing page screenshot you provided as:

**File path**: `apps/frontend/public/og-image.png`

**Recommended specifications**:
- **Dimensions**: 1200 x 630 pixels (required for proper display)
- **Format**: PNG or JPEG
- **File size**: Under 1MB
- **Aspect ratio**: 1.91:1

## How to Create the OG Image

### Option 1: Take a Screenshot (Quick)
1. Open your landing page in browser at full width
2. Take screenshot of the hero section
3. Crop/resize to 1200 x 630 pixels
4. Save as `og-image.png` in this folder (`public/`)

### Option 2: Use Design Tool (Better Quality)
1. Export your landing page hero from Figma/design tool
2. Resize to 1200 x 630 pixels
3. Add any branding/text overlays if needed
4. Export as PNG
5. Save as `og-image.png` in this folder

### Option 3: Create Custom Card
For better engagement, create a custom card with:
- Your logo
- Tagline: "Built for value, not engagement"
- Brand colors (#20C1AC)
- 1200 x 630 dimensions

## Testing the Image

Once saved, test it:

1. **Local testing**:
   ```bash
   # View the image directly
   http://localhost:5173/og-image.png
   ```

2. **Social media preview tools**:
   - Facebook: https://developers.facebook.com/tools/debug/
   - Twitter: https://cards-dev.twitter.com/validator
   - LinkedIn: https://www.linkedin.com/post-inspector/

## Current Usage

This single image is used for ALL Open Graph previews:
- Landing page shares
- Pulse feed shares
- Thread/pulse shares (when no pulse image exists)
- Profile shares (when no avatar exists)

## Future Improvement

For Phase 2, you can add dynamic OG images:
- Generate custom cards for each pulse
- Include pulse text and author
- Unique image per profile

But for MVP, this single image works perfectly!

---

**Status**: Waiting for `og-image.png` file
**Location**: `apps/frontend/public/og-image.png`
**Current reference**: All SEO meta tags point to `https://codewrinkles.com/og-image.png`
