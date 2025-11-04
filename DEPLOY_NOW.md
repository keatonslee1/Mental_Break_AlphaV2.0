# Quick Deploy to Vercel - Dashboard Method

Since Vercel CLI is not installed, use the Vercel Dashboard to deploy.

## Build Location
Your Unity WebGL build is located at:
```
Mental_Break_AlphaV2.0/webgl-build/
```

## Deploy via Vercel Dashboard

### Step 1: Prepare Build
The build is ready at: `Mental_Break_AlphaV2.0/webgl-build/`
- ✅ Contains `index.html`
- ✅ Contains `Build/` directory
- ✅ Contains `TemplateData/` directory
- ✅ Contains `vercel.json` configuration

### Step 2: Deploy to Vercel

**Option A: Drag & Drop (Easiest)**

1. Go to [vercel.com/dashboard](https://vercel.com/dashboard)
2. Click **Add New Project**
3. Select **Upload** or **Browse** (if available)
4. Navigate to and select the `Mental_Break_AlphaV2.0/webgl-build/` folder
5. Click **Deploy**

**Option B: Import from Git (Recommended for Updates)**

1. Push your code to GitHub (if not already)
2. Go to [vercel.com/dashboard](https://vercel.com/dashboard)
3. Click **Add New Project**
4. Click **Import Git Repository**
5. Select your repository
6. Configure:
   - **Framework Preset**: Other
   - **Root Directory**: `Mental_Break_AlphaV2.0/webgl-build` (or leave blank if deploying from root)
   - **Build Command**: Leave blank (pre-built)
   - **Output Directory**: `.` (current directory)
7. Click **Deploy**

### Step 3: Project Configuration

After deployment, configure:
- **Project Name**: `mental-break`
- The deployment will provide a URL (e.g., `mental-break-xxx.vercel.app`)

### Step 4: Configure Domain

1. In Vercel Dashboard → Your Project → **Settings** → **Domains**
2. Click **Add Domain**
3. Enter: `mentalbreak.io`
4. Follow DNS configuration instructions:
   - Add CNAME record: `@` → `cname.vercel-dns.com`
   - Or add A record with Vercel's IP (shown in dashboard)
5. Wait for DNS propagation (can take a few minutes to 48 hours)
6. SSL certificate will be automatically provisioned

### Step 5: Verify

1. Test deployment URL from Vercel dashboard
2. Test domain: `https://mentalbreak.io`
3. Verify game loads correctly
4. Check browser console for any errors

## Team Information

- **Team ID**: `team_w3aTDuy3oozvmAKuhIdJCGq3`
- **Team Name**: Keaton Lee's projects
- **Project Name**: `mental-break` (will be created on first deploy)
- **Domain**: `mentalbreak.io`

## Troubleshooting

- **Build not found**: Ensure you're selecting the `Mental_Break_AlphaV2.0/webgl-build/` directory
- **Game won't load**: Check browser console, verify HTTPS is used
- **Domain not working**: Check DNS records, wait for propagation

## Next Steps After Deployment

Once deployed, you can:
1. Set up automatic deployments from Git (push to trigger deploy)
2. Configure custom headers if needed
3. Monitor deployment logs and analytics

