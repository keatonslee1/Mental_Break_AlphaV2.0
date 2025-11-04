# Vercel Deployment Steps for mentalbreak.io

This document outlines the exact steps to deploy the Unity WebGL build to Vercel with the mentalbreak.io domain.

## Prerequisites Completed
- ✅ Unity Editor 6.2 configured
- ✅ `vercel.json` configured with correct routes and headers
- ✅ Deployment scripts created (`deploy-to-vercel.ps1` and `deploy-to-vercel.sh`)

## Step 1: Build Unity WebGL (Manual - Required First)

**This must be done in Unity Editor before proceeding.**

1. Open Unity project: `Mental_Break_AlphaV2.0`
2. Build WebGL with Brotli compression (see `BUILD_INSTRUCTIONS.md`)
3. Output to: `webgl-build/` (project root)
4. Verify build contains: `index.html`, `Build/`, `TemplateData/`

## Step 2: Install Vercel CLI (If Not Already Installed)

```powershell
npm install -g vercel
```

## Step 3: Login to Vercel

```powershell
vercel login
```

## Step 4: Deploy to Vercel

### Option A: Using Deployment Script (Recommended)

```powershell
# Preview deployment
.\deploy-to-vercel.ps1

# Production deployment
.\deploy-to-vercel.ps1 -Prod
```

### Option B: Using Vercel CLI Directly

```powershell
# Navigate to project root (if not already)
cd C:\Users\epick\OneDrive\Documents\Github\Mental_Break_AlphaV2.0

# Deploy (first time will prompt for setup)
vercel --prod
```

When prompted:
- **Set up and deploy?** Yes
- **Which scope?** Select your account or team
- **Link to existing project?** No (first time) or Yes (if updating)
- **Project name?** `mental-break`
- **Directory?** `webgl-build`
- **Override settings?** No

## Step 5: Configure Domain

After successful deployment:

1. Go to [Vercel Dashboard](https://vercel.com/dashboard)
2. Select your project (`mental-break`)
3. Go to **Settings** > **Domains**
4. Click **Add Domain**
5. Enter: `mentalbreak.io`
6. Follow DNS configuration instructions:
   - Add CNAME record: `@` → `cname.vercel-dns.com`
   - Or add A record with Vercel's IP (shown in dashboard)
7. Wait for DNS propagation (can take a few minutes to 48 hours)
8. SSL certificate will be automatically provisioned

## Step 6: Verify Deployment

1. Test deployment URL from Vercel dashboard
2. Test domain: `https://mentalbreak.io`
3. Verify game loads correctly
4. Check browser console for any errors

## Troubleshooting

### Build Not Found
- Ensure `webgl-build/` directory exists
- Verify `webgl-build/index.html` exists
- Rebuild in Unity Editor if needed

### Domain Not Working
- Check DNS records are correctly configured
- Wait for DNS propagation (can take up to 48 hours)
- Verify domain is added in Vercel dashboard

### Game Won't Load
- Check browser console for errors
- Verify file paths are correct
- Ensure using HTTPS (not HTTP)
- Check that Brotli compression is enabled in Unity build

## Team/Project Info

- **Team ID**: `team_w3aTDuy3oozvmAKuhIdJCGq3`
- **Team Name**: Keaton Lee's projects
- **Project Name**: `mental-break` (to be created on first deploy)
- **Domain**: `mentalbreak.io`

## Future Deployments

After initial setup:
1. Build Unity WebGL in Unity Editor
2. Run: `.\deploy-to-vercel.ps1 -Prod`
3. Vercel will automatically deploy to production

