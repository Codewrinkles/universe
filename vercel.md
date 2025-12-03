# Deploy Frontend to Vercel - Step by Step Guide

This guide will help you deploy your Codewrinkles frontend to Vercel and connect it to your Azure backend.

**What you already have:**
- ✅ Backend running on Azure: `app-codewrinkles-api.azurewebsites.net`
- ✅ Azure SQL Database configured
- ✅ All environment variables set in Azure App Service
- ✅ Custom domain: `codewrinkles.com` (currently on Cloudflare DNS)

**What we're going to do:**
1. Create a Vercel account
2. Connect your GitHub repository
3. Configure Vercel to build your frontend
4. Deploy your site (you'll get a vercel.app URL first)
5. Connect your custom domain `codewrinkles.com`
6. Update Google/GitHub OAuth settings

**Time needed:** About 15-20 minutes

---

## Step 1: Create Vercel Account

1. Open your browser and go to: **https://vercel.com**

2. Click the **"Sign Up"** button (top right corner)

3. Click **"Continue with GitHub"**
   - This is important - you need to use GitHub because your code is on GitHub

4. GitHub will ask you to authorize Vercel
   - Click **"Authorize Vercel"**
   - Enter your GitHub password if prompted

5. You'll see a welcome screen
   - Just click **"Continue"** or **"Skip"** through any tutorial prompts

✅ **You now have a Vercel account!**

---

## Step 2: Import Your Repository

1. You should see the Vercel dashboard
   - If you see a button that says **"Add New..."**, click it
   - If you see **"Import Project"**, click that instead

2. Click **"Project"** from the dropdown

3. You'll see a list of your GitHub repositories
   - Find **"universe"** in the list
   - Click the **"Import"** button next to it

   **If you don't see your repository:**
   - Click **"Adjust GitHub App Permissions"**
   - Select which repositories Vercel can access
   - Make sure "universe" is included
   - Click **"Save"**
   - Go back to Vercel and refresh the page

✅ **Vercel is now connected to your GitHub repo!**

---

## Step 3: Configure Your Project

After clicking Import, you'll see a configuration screen with several sections:

### 3.1 Configure Project

**Project Name:**
- You'll see a field with your project name
- Leave it as `universe` or change it to `codewrinkles` (doesn't matter)

**Framework Preset:**
- Vercel should automatically detect **"Vite"**
- If it says "Other" instead, click the dropdown and select **"Vite"**

### 3.2 Root Directory

**This is VERY important:**

1. Click **"Edit"** next to "Root Directory"

2. Type: `apps/frontend`
   - Make sure there's NO slash at the beginning
   - Make sure there's NO slash at the end
   - Exactly: `apps/frontend`

3. Vercel will show a preview of the directory structure
   - You should see `package.json`, `src/`, `public/`, etc.
   - If you don't see these files, the path is wrong

### 3.3 Build and Output Settings

Click **"Build and Output Settings"** to expand this section.

**Build Command:**
- Should show: `npm run build`
- If it's empty or different, change it to: `npm run build`

**Output Directory:**
- Should show: `dist`
- If it's empty or different, change it to: `dist`

**Install Command:**
- Should show: `npm install`
- Leave this as is

### 3.4 Environment Variables

**This is CRITICAL - your app won't work without this:**

1. Click **"Environment Variables"** to expand this section

2. You'll see fields for adding environment variables

3. In the **Name** (or **Key**) field, type exactly: `VITE_API_BASE_URL`

4. In the **Value** field, type exactly: `https://app-codewrinkles-api.azurewebsites.net`

5. If you see any dropdown or options for which environments to apply this to, make sure it applies to all environments (Production, Preview, Development)

6. Click **"Add"** or the **+** button to add the variable

### 3.5 Deploy

**Double-check everything:**
- ✅ Framework: Vite
- ✅ Root Directory: `apps/frontend`
- ✅ Build Command: `npm run build`
- ✅ Output Directory: `dist`
- ✅ Environment Variable: `VITE_API_BASE_URL` = `https://app-codewrinkles-api.azurewebsites.net`

**Click the big "Deploy" button**

✅ **Vercel is now building your site!**

---

## Step 4: Wait for Deployment

You'll see a screen with animated dots and build logs.

**What's happening:**
1. Vercel is cloning your GitHub repository
2. Installing dependencies (`npm install`)
3. Building your React app (`npm run build`)
4. Deploying to their global CDN

**This takes about 1-3 minutes.**

**You'll see logs scrolling by:**
- `> Building...`
- `> Installing dependencies...`
- `> Running build command...`
- `✓ Build completed`

**When it's done, you'll see:**
- 🎉 Confetti animation
- A big preview image of your site
- A URL like: `https://universe-abc123.vercel.app`

✅ **Your site is live!**

---

## Step 5: Test Your Deployment

1. Click the **"Visit"** button or click on the URL

2. You should see your Codewrinkles landing page
   - The design should look exactly like your local version
   - If you see errors or a blank page, check the browser console (F12)

3. **Don't test login yet** - OAuth won't work until we finish Step 9

✅ **Your frontend is working on Vercel!**

---

## Step 6: Add Your Custom Domain

Now let's make it accessible at `codewrinkles.com` instead of that long vercel.app URL.

1. In the Vercel dashboard, make sure you're viewing your project
   - You should see tabs like: Overview, Deployments, Analytics, Settings, etc.

2. Click **"Settings"** (top menu)

3. Click **"Domains"** (left sidebar)

4. You'll see a big input field that says **"Add domain"**

5. Type: `codewrinkles.com` (no http://, no www, just the domain)

6. Click **"Add"**

**Vercel will ask: "Add codewrinkles.com and redirect www.codewrinkles.com to it?"**

7. Click **"Add codewrinkles.com & redirect www.codewrinkles.com"**
   - This means both `codewrinkles.com` and `www.codewrinkles.com` will work
   - Both will show your site

**You'll see a screen showing:**
- ❌ `codewrinkles.com` - Invalid Configuration
- ❌ `www.codewrinkles.com` - Invalid Configuration

**This is normal!** We need to update DNS records next.

---

## Step 7: Configure DNS on Cloudflare

Vercel will show you exactly what DNS records to add. Let me tell you what to do in Cloudflare.

### 7.1 Open Cloudflare in a New Tab

1. Go to: **https://dash.cloudflare.com**

2. Log in

3. Click on your domain: **codewrinkles.com**

4. Click **"DNS"** in the left menu (you might already be there)

### 7.2 Update DNS Records

**Back in Vercel, you'll see DNS instructions. Look for:**
- An **A Record** with value `76.76.21.21`
- A **CNAME Record** for `www`

**In Cloudflare:**

#### For the apex domain (codewrinkles.com):

1. Look for an **A record** with name `@` or `codewrinkles.com`
   - If you see one pointing to Azure's IP, we need to change it

2. Click **"Edit"** on that record (or delete it and add a new one)

3. Fill in:
   - **Type:** `A`
   - **Name:** `@`
   - **IPv4 address:** `76.76.21.21`
   - **Proxy status:** Click to turn it **OFF** (grey cloud, not orange)
   - **TTL:** Auto

4. Click **"Save"**

#### For the www subdomain:

1. Look for a **CNAME record** with name `www`
   - If you see one, we need to change it

2. Click **"Edit"** on that record (or delete it and add a new one)

3. Fill in:
   - **Type:** `CNAME`
   - **Name:** `www`
   - **Target:** `cname.vercel-dns.com`
   - **Proxy status:** Click to turn it **OFF** (grey cloud, not orange)
   - **TTL:** Auto

4. Click **"Save"**

**IMPORTANT:** Make sure the proxy status is **OFF** (grey cloud) for both records. The orange cloud can cause issues with SSL certificate provisioning.

### 7.3 Verify DNS Changes

Back in your terminal (or Git Bash), run:

```bash
nslookup codewrinkles.com
```

You should see:
```
Address: 76.76.21.21
```

And:

```bash
nslookup www.codewrinkles.com
```

You should see:
```
... cname.vercel-dns.com
```

**If the results are different:**
- Wait 5 minutes and try again
- DNS changes can take up to 10 minutes to propagate
- Sometimes up to an hour (rare)

---

## Step 8: Wait for SSL Certificate

**Go back to Vercel:**

1. On the **Settings > Domains** page

2. Refresh the page every minute or so

3. The status will change from ❌ Invalid Configuration to:
   - ⏳ Pending... (verifying DNS)
   - 🔒 Provisioning SSL Certificate...
   - ✅ Valid Configuration

**This usually takes 1-5 minutes after DNS is updated.**

**When both domains show ✅ Valid Configuration, you're done!**

---

## Step 9: Test Your Custom Domain

1. Open a new browser tab (or clear cache)

2. Go to: **https://codewrinkles.com**

3. You should see:
   - ✅ Your Codewrinkles landing page
   - ✅ Green padlock in the address bar (SSL working)
   - ✅ Fast loading

4. Try: **https://www.codewrinkles.com**
   - Should redirect to `https://codewrinkles.com`

✅ **Your custom domain is working!**

---

## Step 10: Update OAuth Redirect URIs

For Google/GitHub login to work, you need to update the OAuth redirect URIs.

### 10.1 Google OAuth

1. Go to: **https://console.cloud.google.com**

2. Make sure you're in the correct project (top left dropdown)

3. Go to **APIs & Services** > **Credentials**

4. Click on your **OAuth 2.0 Client ID**

5. Scroll to **Authorized redirect URIs**

6. You should see:
   - `https://localhost:7280/api/identity/oauth/google/callback` (for local dev)

7. Click **"+ ADD URI"**

8. Add: `https://app-codewrinkles-api.azurewebsites.net/api/identity/oauth/google/callback`

9. Scroll to **Authorized JavaScript origins**

10. Click **"+ ADD URI"**

11. Add: `https://codewrinkles.com`

12. Click **"SAVE"** at the bottom

### 10.2 GitHub OAuth

1. Go to: **https://github.com/settings/developers**

2. Click **"OAuth Apps"** in the left menu

3. Click on your **Codewrinkles** OAuth App

4. **Homepage URL:**
   - Change to: `https://codewrinkles.com`

5. **Authorization callback URL:**
   - Should have: `https://localhost:7280/api/identity/oauth/github/callback` (for local dev)
   - Update to: `https://app-codewrinkles-api.azurewebsites.net/api/identity/oauth/github/callback`

6. Click **"Update application"**

---

## Step 11: Test Everything

### Test 1: Landing Page
1. Go to `https://codewrinkles.com`
2. ✅ Page loads correctly
3. ✅ Navigation works
4. ✅ All images and assets load

### Test 2: Email/Password Registration
1. Click **"Get Started"** or **"Sign Up"**
2. Register a new account with email/password
3. ✅ You should be logged in
4. ✅ You should see your profile

### Test 3: Google OAuth
1. Log out
2. Click **"Login"**
3. Click **"Sign in with Google"**
4. You're redirected to Google
5. Select your Google account
6. You're redirected back to `codewrinkles.com`
7. ✅ You should be logged in

### Test 4: GitHub OAuth
1. Log out
2. Click **"Login"**
3. Click **"Sign in with GitHub"**
4. Authorize the app
5. You're redirected back to `codewrinkles.com`
6. ✅ You should be logged in

### Test 5: Token Refresh
1. Stay logged in
2. Wait 16 minutes (access token expires after 15 min)
3. Navigate to a different page
4. ✅ You should still be logged in (refresh token works)

---

## What Happens on Future Code Changes?

**Vercel automatically deploys when you push to GitHub!**

1. Make changes to your code locally
2. Commit and push to `main` branch
3. Vercel detects the push
4. Automatically builds and deploys
5. Your site is updated in 1-2 minutes

**To see deployments:**
- Go to Vercel dashboard
- Click **"Deployments"** tab
- See all builds, logs, and deployment status

---

## Troubleshooting

### Problem: Site shows blank page

**Check:**
1. Environment variable set correctly: `VITE_API_BASE_URL=https://app-codewrinkles-api.azurewebsites.net`
2. Root directory is: `apps/frontend`
3. Build command is: `npm run build`
4. Output directory is: `dist`

**Fix:**
- Go to Vercel **Settings** > **Environment Variables**
- Make sure `VITE_API_BASE_URL` is set correctly
- Go to **Deployments**
- Click the three dots (...) next to the latest deployment
- Click **"Redeploy"**

### Problem: "CORS error" in browser console

**Check:**
1. Open browser DevTools (F12)
2. Go to Console tab
3. Look for errors mentioning "CORS" or "Access-Control-Allow-Origin"

**Fix:**
- Your backend CORS is already configured for `codewrinkles.com`
- Make sure your Azure App Service is running
- Test backend directly: `https://app-codewrinkles-api.azurewebsites.net/health`
- If backend is down, restart it in Azure Portal

### Problem: OAuth redirects to wrong URL

**Symptom:**
After clicking "Sign in with Google/GitHub", you're redirected to a weird URL or get an error.

**Check:**
1. Go to backend: `https://app-codewrinkles-api.azurewebsites.net`
2. Make sure Azure App Service environment variable `ASPNETCORE_ENVIRONMENT` is set to `Production`

**Fix:**
- Go to Azure Portal
- Navigate to your App Service
- Click **Configuration** > **Application settings**
- Find `ASPNETCORE_ENVIRONMENT`
- Make sure value is `Production` (capital P)
- Click **Save** and **Continue** when prompted

### Problem: Domain shows "Not Found" or 404

**Check DNS:**
```bash
nslookup codewrinkles.com
```

Should return: `76.76.21.21`

**If different:**
- Check Cloudflare DNS records
- Make sure A record points to `76.76.21.21`
- Make sure proxy is OFF (grey cloud)
- Wait 10 minutes and try again

### Problem: SSL certificate not working (red padlock or warning)

**Check:**
1. Vercel **Settings** > **Domains**
2. Both domains should show ✅ Valid Configuration

**If not:**
1. Make sure DNS records are correct (see Step 7)
2. Make sure Cloudflare proxy is OFF (grey cloud)
3. Wait up to 1 hour for DNS propagation
4. Vercel will automatically retry SSL provisioning

---

## Cost Summary

| Service | Plan | Monthly Cost |
|---------|------|--------------|
| **Vercel** | Free (Hobby) | $0 |
| **Azure App Service** | B1 Basic | ~$13 |
| **Azure SQL Database** | Basic (5 DTU) | ~$5 |
| **Cloudflare DNS** | Free | $0 |
| **Total** | | **~$18/month** |

**Vercel Free Tier Limits:**
- ✅ Unlimited sites and deployments
- ✅ Custom domains with SSL
- ✅ 100 GB bandwidth/month (enough for thousands of users)
- ✅ Automatic deployments from GitHub
- ✅ Preview deployments for pull requests

---

## You're Done!

Your Codewrinkles app is now live at:
- **Frontend:** https://codewrinkles.com (Vercel)
- **Backend:** https://app-codewrinkles-api.azurewebsites.net (Azure)
- **Database:** Azure SQL

Every time you push code to GitHub, Vercel automatically deploys the updates. No manual deployment needed!

---

## Next Steps (Optional)

### Enable Cloudflare Proxy (After Everything Works)

Once you've confirmed everything is working perfectly:

1. Go to Cloudflare DNS
2. Click "Edit" on the `@` A record
3. Click the cloud icon to turn it **orange** (proxied)
4. Click "Save"
5. Do the same for the `www` CNAME record

**Benefits:**
- DDoS protection
- Additional CDN layer
- Web Application Firewall (WAF)
- Better analytics

### Set Up Preview Deployments

Vercel automatically creates preview URLs for pull requests:

1. Create a new branch: `git checkout -b feature/my-feature`
2. Make changes and push: `git push origin feature/my-feature`
3. Create a pull request on GitHub
4. Vercel automatically deploys a preview
5. You get a unique URL like: `https://universe-abc123-username.vercel.app`
6. Test your changes before merging to main

### Add Application Insights (Azure Monitoring)

To monitor your backend:

1. Create Application Insights resource in Azure
2. Add NuGet package to your backend
3. Configure instrumentation key in App Service
4. View logs, errors, and performance metrics in Azure Portal

---

**Need help?** You have all the facts now. Everything is set up and connected!
