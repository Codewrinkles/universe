# Azure Deployment Guide - Codewrinkles Universe

> **Purpose**: Step-by-step instructions to set up all Azure resources and configure GoDaddy DNS for production deployment.

---

## Prerequisites

- Azure account (create at https://azure.microsoft.com if needed)
- GoDaddy account with `codewrinkles.com` domain
- Google Cloud Console access (for OAuth)
- GitHub account with OAuth app access

---

## Cost Estimate

| Service | SKU | Monthly Cost |
|---------|-----|--------------|
| Azure Static Web Apps | Free | $0 |
| Azure App Service | B1 Basic | ~$13 |
| Azure SQL Database | Free tier | $0 |
| **Total** | | **~$13/month** |

---

## Part 1: Create Azure Resources

### Step 1.1: Create Resource Group

1. Go to [Azure Portal](https://portal.azure.com)
2. Search for "Resource groups" in the top search bar
3. Click **+ Create**
4. Fill in:
   - **Subscription**: Your subscription
   - **Resource group**: `rg-codewrinkles-prod`
   - **Region**: `West Europe` (or closest to your users)
5. Click **Review + create** → **Create**

---

### Step 1.2: Create Azure SQL Database (Free Tier)

#### 1.2.1: Create SQL Server

1. Search for "SQL servers" in Azure Portal
2. Click **+ Create**
3. Fill in **Basics** tab:
   - **Subscription**: Your subscription
   - **Resource group**: `rg-codewrinkles-prod`
   - **Server name**: `sql-codewrinkles` (must be globally unique, try `sql-codewrinkles-prod` if taken)
   - **Location**: Same as resource group (e.g., `West Europe`)
   - **Authentication method**: `Use SQL authentication`
   - **Server admin login**: `codewrinkles-admin` (write this down!)
   - **Password**: Create a strong password (write this down!)
4. Click **Review + create** → **Create**
5. Wait for deployment to complete

#### 1.2.2: Create Database

1. Go to your newly created SQL server
2. Click **+ Create database** (top menu)
3. Fill in:
   - **Database name**: `codewrinkles`
   - **Want to use SQL elastic pool?**: No
   - **Workload environment**: `Development`
4. Click **Configure database** under Compute + storage
5. Select **Service tier**: `Free tier` (100,000 vCore seconds/month)
   - If Free tier is not visible, select "Serverless" and look for the free option
6. Enable **Auto-pause**: Yes (important for free tier!)
7. Click **Apply**
8. Click **Review + create** → **Create**

#### 1.2.3: Configure Firewall Rules

1. Go to your SQL server (`sql-codewrinkles`)
2. Left menu → **Networking**
3. Under **Firewall rules**:
   - Set **Allow Azure services and resources to access this server**: `Yes`
   - Click **+ Add your client IPv4 address** (adds your current IP for migrations)
4. Click **Save**

#### 1.2.4: Get Connection String

1. Go to your database (`codewrinkles`)
2. Left menu → **Connection strings**
3. Copy the **ADO.NET (SQL authentication)** connection string
4. It looks like:
   ```
   Server=tcp:sql-codewrinkles.database.windows.net,1433;Initial Catalog=codewrinkles;Persist Security Info=False;User ID=codewrinkles-admin;Password={your_password};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
   ```
5. **Replace `{your_password}` with your actual password**
6. Save this connection string securely - you'll need it later!

---

### Step 1.3: Create App Service (Backend API)

#### 1.3.1: Create App Service Plan

1. Search for "App Service plans" in Azure Portal
2. Click **+ Create**
3. Fill in **Basics** tab:
   - **Subscription**: Your subscription
   - **Resource group**: `rg-codewrinkles-prod`
   - **Name**: `asp-codewrinkles`
   - **Operating System**: `Linux`
   - **Region**: Same as SQL Server (e.g., `West Europe`)
   - **Pricing plan**: Click "Explore pricing plans"
     - Select **Basic B1** (~$13/month)
     - This includes "Always On" capability
4. Click **Review + create** → **Create**

#### 1.3.2: Create Web App

1. Search for "App Services" in Azure Portal
2. Click **+ Create** → **Web App**
3. Fill in **Basics** tab:
   - **Subscription**: Your subscription
   - **Resource group**: `rg-codewrinkles-prod`
   - **Name**: `app-codewrinkles-api` (becomes `app-codewrinkles-api.azurewebsites.net`)
   - **Publish**: `Code`
   - **Runtime stack**: `.NET 10 (Preview)` or latest .NET available
   - **Operating System**: `Linux`
   - **Region**: Same as others
   - **App Service Plan**: Select `asp-codewrinkles`
4. Click **Review + create** → **Create**
5. Wait for deployment to complete

#### 1.3.3: Configure App Service Settings

1. Go to your App Service (`app-codewrinkles-api`)
2. Left menu → **Configuration**
3. Click **+ New application setting** for each of the following:

| Name | Value |
|------|-------|
| `ConnectionStrings__DefaultConnection` | Your Azure SQL connection string (from Step 1.2.4) |
| `Jwt__SecretKey` | Generate a 64-character random string (see below) |
| `Jwt__Issuer` | `Codewrinkles` |
| `Jwt__Audience` | `Codewrinkles` |
| `OAuth__Google__ClientId` | Your Google OAuth client ID |
| `OAuth__Google__ClientSecret` | Your Google OAuth client secret |
| `OAuth__GitHub__ClientId` | Your GitHub OAuth client ID |
| `OAuth__GitHub__ClientSecret` | Your GitHub OAuth client secret |
| `ASPNETCORE_ENVIRONMENT` | `Production` |

**Generate JWT Secret Key:**
Run this in PowerShell or use an online generator:
```powershell
[Convert]::ToBase64String((1..64 | ForEach-Object { Get-Random -Maximum 256 }) -as [byte[]])
```
Or use: https://generate-random.org/api-key-generator (64 characters, alphanumeric)

4. Click **Save** at the top
5. Click **Continue** to confirm restart

#### 1.3.4: Enable Always On

1. Still in App Service → **Configuration**
2. Click **General settings** tab
3. Set **Always on**: `On`
4. Click **Save**

#### 1.3.5: Note the Default Domain

Your API will be available at: `https://app-codewrinkles-api.azurewebsites.net`

---

### Step 1.4: Create Static Web App (Frontend)

1. Search for "Static Web Apps" in Azure Portal
2. Click **+ Create**
3. Fill in **Basics** tab:
   - **Subscription**: Your subscription
   - **Resource group**: `rg-codewrinkles-prod`
   - **Name**: `swa-codewrinkles`
   - **Plan type**: `Free`
   - **Region**: Pick one close to users (e.g., `West Europe`)
   - **Source**: `GitHub`
4. Click **Sign in with GitHub** and authorize Azure
5. Fill in **GitHub details**:
   - **Organization**: `Codewrinkles`
   - **Repository**: `universe`
   - **Branch**: `main`
6. Fill in **Build Details**:
   - **Build Presets**: `Custom`
   - **App location**: `apps/frontend`
   - **Api location**: (leave empty)
   - **Output location**: `dist`
7. Click **Review + create** → **Create**
8. Wait for deployment

**Important**: Azure will create a GitHub Action workflow file automatically. We'll customize it later.

#### 1.4.1: Get Static Web App Token

1. Go to your Static Web App (`swa-codewrinkles`)
2. Left menu → **Manage deployment token**
3. Click **Copy** to copy the token
4. Save this token - you'll need it for GitHub Actions!

#### 1.4.2: Note the Default Domain

Your frontend will be available at: `https://<random-name>.azurestaticapps.net`
(You can find this on the Overview page)

---

## Part 2: Configure Custom Domains

### Step 2.1: Configure Static Web App Custom Domain

#### 2.1.1: Add Apex Domain (codewrinkles.com)

1. Go to Static Web App (`swa-codewrinkles`)
2. Left menu → **Custom domains**
3. Click **+ Add**
4. Select **Custom domain on other DNS**
5. Enter: `codewrinkles.com`
6. Click **Next**
7. Azure will show you a **TXT record** to add for validation:
   - **Type**: TXT
   - **Name**: `@` or `codewrinkles.com`
   - **Value**: Something like `asuid.codewrinkles.com` + a validation code
8. **Don't close this page yet** - go to GoDaddy (see Part 3)

#### 2.1.2: Add WWW Subdomain (www.codewrinkles.com)

1. After apex domain is verified, click **+ Add** again
2. Enter: `www.codewrinkles.com`
3. Azure will show a **CNAME record** to add
4. Add this to GoDaddy (see Part 3)

### Step 2.2: Configure App Service Custom Domain

#### 2.2.1: Add API Subdomain (api.codewrinkles.com)

1. Go to App Service (`app-codewrinkles-api`)
2. Left menu → **Custom domains**
3. Click **+ Add custom domain**
4. Enter: `api.codewrinkles.com`
5. Click **Validate**
6. Azure will show you the required DNS records:
   - **CNAME record** pointing to `app-codewrinkles-api.azurewebsites.net`
   - Or **TXT record** for validation + **A record** for IP
7. Note down these values for GoDaddy configuration

---

## Part 3: Configure GoDaddy DNS

### Step 3.1: Access DNS Management

1. Log in to [GoDaddy](https://www.godaddy.com)
2. Go to **My Products**
3. Find `codewrinkles.com` and click **DNS**

### Step 3.2: Add DNS Records

Delete any existing A or CNAME records for `@`, `www`, and `api` first (if they exist).

Add the following records:

#### For Static Web App (Frontend):

| Type | Name | Value | TTL |
|------|------|-------|-----|
| TXT | `@` | The validation value from Azure Static Web App | 1 Hour |
| A | `@` | IP address provided by Azure Static Web App | 1 Hour |
| CNAME | `www` | `<your-swa-name>.azurestaticapps.net` | 1 Hour |

**Note**: For the apex domain (`@`), Azure Static Web Apps may provide an IP address. If they ask for an alias record and GoDaddy doesn't support ALIAS, use the A record with the IP.

#### For App Service (Backend API):

| Type | Name | Value | TTL |
|------|------|-------|-----|
| CNAME | `api` | `app-codewrinkles-api.azurewebsites.net` | 1 Hour |
| TXT | `asuid.api` | Validation token from Azure (if required) | 1 Hour |

### Step 3.3: Verify DNS Propagation

1. Wait 5-15 minutes for DNS propagation
2. Verify using: https://dnschecker.org
   - Check `codewrinkles.com`
   - Check `www.codewrinkles.com`
   - Check `api.codewrinkles.com`

### Step 3.4: Complete Azure Domain Verification

1. Go back to Azure Static Web App → Custom domains
2. Click **Refresh** or re-validate
3. Domain should show as "Ready"
4. Repeat for App Service custom domain

---

## Part 4: Enable SSL Certificates

### Step 4.1: Static Web App SSL

SSL is **automatically enabled** for Static Web Apps custom domains. No action needed.

### Step 4.2: App Service SSL

1. Go to App Service (`app-codewrinkles-api`)
2. Left menu → **Custom domains**
3. You should see `api.codewrinkles.com` listed
4. Under **SSL state**, if it says "Not Secure":
   - Click **Add binding**
   - Select **App Service Managed Certificate** (free)
   - Click **Create**
5. Wait for certificate to be issued (can take 5-15 minutes)
6. Once ready, click **Add binding** again
7. Select the certificate and binding type **SNI SSL**
8. Click **Add**

---

## Part 5: Update OAuth Providers

### Step 5.1: Update Google OAuth

1. Go to [Google Cloud Console](https://console.cloud.google.com)
2. Navigate to **APIs & Services** → **Credentials**
3. Click on your OAuth 2.0 Client ID
4. Under **Authorized redirect URIs**, add:
   ```
   https://api.codewrinkles.com/api/identity/oauth/google/callback
   ```
5. Click **Save**

### Step 5.2: Update GitHub OAuth

1. Go to [GitHub Developer Settings](https://github.com/settings/developers)
2. Click on your OAuth App
3. Update **Authorization callback URL**:
   ```
   https://api.codewrinkles.com/api/identity/oauth/github/callback
   ```
4. Click **Update application**

---

## Part 6: Run Database Migrations

Before deploying the app, run EF Core migrations against Azure SQL.

### Step 6.1: Run Migrations Locally

Open terminal in `apps/backend` directory:

```bash
# Make sure you have the Azure SQL connection string with your actual password
# Replace the connection string below with yours

dotnet ef database update \
  --project src/Codewrinkles.Infrastructure \
  --startup-project src/Codewrinkles.API \
  --connection "Server=tcp:sql-codewrinkles.database.windows.net,1433;Initial Catalog=codewrinkles;Persist Security Info=False;User ID=codewrinkles-admin;Password=YOUR_PASSWORD_HERE;MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
```

**On Windows (PowerShell):**
```powershell
dotnet ef database update `
  --project src/Codewrinkles.Infrastructure `
  --startup-project src/Codewrinkles.API `
  --connection "Server=tcp:sql-codewrinkles.database.windows.net,1433;Initial Catalog=codewrinkles;Persist Security Info=False;User ID=codewrinkles-admin;Password=YOUR_PASSWORD_HERE;MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
```

### Step 6.2: Verify Migration

1. Go to Azure Portal → SQL databases → `codewrinkles`
2. Left menu → **Query editor (preview)**
3. Login with your SQL credentials
4. Run: `SELECT name FROM sys.tables`
5. You should see tables like: `Identities`, `Profiles`, `Pulses`, etc.

---

## Part 7: Add GitHub Secrets

Before creating GitHub Actions, add these secrets to your repository.

### Step 7.1: Access GitHub Secrets

1. Go to your GitHub repository: `https://github.com/Codewrinkles/universe`
2. Click **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret** for each:

### Step 7.2: Required Secrets

| Secret Name | Value | Where to Get It |
|-------------|-------|-----------------|
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | Deployment token | Azure Static Web App → Manage deployment token |
| `AZURE_WEBAPP_PUBLISH_PROFILE` | Full XML content | Azure App Service → Download publish profile |

#### To get Publish Profile:
1. Go to App Service (`app-codewrinkles-api`)
2. Click **Download publish profile** (top menu)
3. Open the downloaded file
4. Copy the **entire XML content**
5. Paste as the value for `AZURE_WEBAPP_PUBLISH_PROFILE`

---

## Summary Checklist

### Azure Resources Created:
- [ ] Resource Group: `rg-codewrinkles-prod`
- [ ] SQL Server: `sql-codewrinkles`
- [ ] SQL Database: `codewrinkles` (Free tier)
- [ ] App Service Plan: `asp-codewrinkles` (B1 Basic)
- [ ] App Service: `app-codewrinkles-api`
- [ ] Static Web App: `swa-codewrinkles`

### Azure Configuration:
- [ ] SQL Server firewall allows Azure services
- [ ] SQL Server firewall has your IP (for migrations)
- [ ] App Service has all environment variables configured
- [ ] App Service has Always On enabled

### Custom Domains:
- [ ] `codewrinkles.com` → Static Web App
- [ ] `www.codewrinkles.com` → Static Web App
- [ ] `api.codewrinkles.com` → App Service

### GoDaddy DNS:
- [ ] TXT record for Static Web App validation
- [ ] A record for `@` (apex domain)
- [ ] CNAME record for `www`
- [ ] CNAME record for `api`

### SSL Certificates:
- [ ] Static Web App SSL (automatic)
- [ ] App Service managed certificate for `api.codewrinkles.com`

### OAuth Providers:
- [ ] Google OAuth redirect URI updated
- [ ] GitHub OAuth callback URL updated

### Database:
- [ ] EF Core migrations run against Azure SQL

### GitHub:
- [ ] `AZURE_STATIC_WEB_APPS_API_TOKEN` secret added
- [ ] `AZURE_WEBAPP_PUBLISH_PROFILE` secret added

---

## Next Steps

After completing all the above, you're ready to:
1. Create GitHub Actions workflows for CI/CD
2. Deploy the application
3. Verify everything works

---

## Troubleshooting

### Database Connection Issues
- Verify firewall rules allow your IP
- Double-check the password in connection string
- Ensure `Encrypt=True` is in the connection string

### Custom Domain Not Working
- Wait for DNS propagation (up to 24 hours, usually 15 minutes)
- Verify DNS records at https://dnschecker.org
- Check Azure domain validation status

### SSL Certificate Issues
- App Service managed certificates can take 15-30 minutes
- Ensure custom domain is validated first
- Try refreshing the Custom Domains page

### OAuth Redirect Errors
- Verify redirect URIs match exactly (including https and trailing slashes)
- Clear browser cache and cookies
- Check browser console for specific errors

---

**Last Updated**: December 2024
