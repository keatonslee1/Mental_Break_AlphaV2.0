# Deployment Guide

Complete guide for deploying Mental Break to Vercel and configuring the domain.

## Prerequisites

- Unity WebGL build completed (see [Build Instructions](BUILD.md))
- Build output located at: `webgl-build/`
- Vercel account (sign up at [vercel.com](https://vercel.com) if needed)
- Domain `mentalbreak.io` (or use Vercel default domain for testing)

## Quick Deployment (5 minutes)

### Option A: Vercel Dashboard (Recommended - No CLI Required)

1. Go to [vercel.com/dashboard](https://vercel.com/dashboard)
2. Click **"Add New Project"**
3. Choose **"Import Git Repository"** (if repo is on GitHub) OR **"Upload"** (to drag & drop)

**If importing from Git:**
- Select your repository
- **Root Directory**: `Mental_Break_AlphaV2.0/webgl-build` (or manually type the path)
- **Framework Preset**: Other
- **Build Command**: (leave blank)
- **Output Directory**: `.` (current directory)

**If uploading:**
- Navigate to `Mental_Break_AlphaV2.0/webgl-build/` folder
- Drag and drop the entire folder

4. Click **"Deploy"**
5. Project will be created as `mental-break` (or name of your choice)
6. You'll get a URL like: `mental-break-xxx.vercel.app`

### Option B: Vercel CLI (For Future Deployments)

**First-time setup:**

1. Install Node.js from [nodejs.org](https://nodejs.org/) (LTS version)
2. Install Vercel CLI:
   ```powershell
   npm install -g vercel
   ```
3. Login:
   ```powershell
   vercel login
   ```
4. Deploy:
   ```powershell
   cd Mental_Break_AlphaV2.0\webgl-build
   vercel --prod
   ```

**When prompted:**
- "Set up and deploy?": Yes
- "Which scope?": Select your account or team (`team_w3aTDuy3oozvmAKuhIdJCGq3`)
- "Link to existing project?": No (first time) or Yes (if updating)
- "Project name?": `mental-break`
- "Directory?": Press Enter (current directory)
- "Override settings?": No (unless you want to customize)

**For future deployments:**
```powershell
.\deploy-to-vercel.ps1 -Prod
```

## Domain Configuration

After deployment, configure your domain:

1. Go to Vercel Dashboard → Your Project → **Settings** → **Domains**
2. Click **"Add Domain"**
3. Enter: `mentalbreak.io`
4. Follow DNS configuration instructions:
   - **CNAME record**: `@` → `cname.vercel-dns.com`
   - **OR A record**: Use Vercel's IP (shown in dashboard)
5. Wait for DNS propagation (few minutes to 48 hours)
6. SSL certificate auto-provisions

## Verification Checklist

After deployment:
- [ ] Deployment URL works and game loads
- [ ] Domain `mentalbreak.io` is added in Vercel dashboard
- [ ] DNS records configured
- [ ] HTTPS works (SSL certificate provisioned)
- [ ] Game loads correctly at `https://mentalbreak.io`
- [ ] No console errors in browser
- [ ] Test across browsers: Chrome, Firefox, Safari, Edge
- [ ] Leaderboard, engagement, and sanity UI appear on first load without requiring a page refresh

## Deployment Scripts

The project includes deployment scripts for convenience:

- **Windows**: `deploy-to-vercel.ps1`
- **Unix/Mac**: `deploy-to-vercel.sh`

Usage:
```powershell
# Preview deployment
.\deploy-to-vercel.ps1

# Production deployment
.\deploy-to-vercel.ps1 -Prod
```

## Vercel Configuration

The project includes `vercel.json` with:
- Correct routes for Unity WebGL files
- Proper MIME types (`.unityweb`, `.wasm`, `.data`, `.js`)
- Compression headers (Gzip/Brotli)
- Cache control headers
- SPA fallback to `index.html`

## Team/Project Information

- **Team ID**: `team_w3aTDuy3oozvmAKuhIdJCGq3`
- **Team Name**: Keaton Lee's projects
- **Project Name**: `mental-break`
- **Domain**: `mentalbreak.io`

## Troubleshooting

See [Troubleshooting Guide](TROUBLESHOOTING.md) for common deployment issues.

### Common Issues

**Build not found:**
- Ensure `webgl-build/` directory exists
- Verify `webgl-build/index.html` exists
- Rebuild in Unity Editor if needed

**Domain not working:**
- Check DNS records are correctly configured
- Wait for DNS propagation (can take up to 48 hours)
- Verify domain is added in Vercel dashboard

**Game won't load:**
- Check browser console for errors
- Verify file paths are correct
- Ensure using HTTPS (not HTTP)
- Check that compression format matches build (Gzip/Brotli)

## Future Deployments

After initial setup:
1. Build Unity WebGL in Unity Editor (see [Build Instructions](BUILD.md))
2. Run: `.\deploy-to-vercel.ps1 -Prod`
3. Vercel will automatically deploy to production

## Notes

- Unity WebGL builds are client-side only. No server-side processing needed.
- PlayerPrefs work in WebGL but are browser-local (cleared with browser data).
- Consider implementing cloud save if needed (separate service).
- Test across browsers: Chrome, Firefox, Safari, Edge.

