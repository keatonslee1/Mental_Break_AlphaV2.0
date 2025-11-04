# Deployment Checklist for mentalbreak.io

Use this checklist to track deployment progress.

## Prerequisites
- [x] Unity Editor 6.2 with WebGL Build Support installed
- [x] Vercel account configured
- [x] Domain mentalbreak.io purchased
- [x] `vercel.json` configured correctly
- [x] Deployment scripts created

## Step 1: Unity WebGL Build (Manual - Required)
- [ ] Open Unity Editor: `Mental_Break_AlphaV2.0`
- [ ] Configure WebGL build settings:
  - [ ] Compression Format: **Brotli**
  - [ ] Code Optimization: **Size**
  - [ ] Data Caching: Enabled
  - [ ] Strip Engine Code: Enabled
- [ ] Build to `webgl-build/` directory
- [ ] Verify build output:
  - [ ] `webgl-build/index.html` exists
  - [ ] `webgl-build/Build/` directory exists
  - [ ] `webgl-build/TemplateData/` directory exists
- [ ] Test build locally (optional but recommended)

## Step 2: Vercel Setup
- [ ] Install Vercel CLI: `npm install -g vercel`
- [ ] Login to Vercel: `vercel login`
- [ ] Verify `webgl-build/` directory exists

## Step 3: Initial Deployment
- [ ] Run deployment: `.\deploy-to-vercel.ps1 -Prod` or `vercel --prod`
- [ ] When prompted:
  - [ ] Project name: `mental-break`
  - [ ] Directory: `webgl-build`
  - [ ] Override settings: No
- [ ] Verify deployment URL works
- [ ] Test game loads correctly in browser

## Step 4: Domain Configuration
- [ ] Go to Vercel Dashboard → Project → Settings → Domains
- [ ] Add domain: `mentalbreak.io`
- [ ] Configure DNS records:
  - [ ] Add CNAME: `@` → `cname.vercel-dns.com`
  - [ ] Or add A record with Vercel's IP
- [ ] Wait for DNS propagation
- [ ] Verify SSL certificate is provisioned
- [ ] Test `https://mentalbreak.io` loads correctly

## Step 5: Verification
- [ ] Game loads at deployment URL
- [ ] Game loads at `https://mentalbreak.io`
- [ ] No console errors in browser
- [ ] Game functionality works correctly
- [ ] Test across browsers: Chrome, Firefox, Safari, Edge

## Notes
- Team ID: `team_w3aTDuy3oozvmAKuhIdJCGq3`
- Team Name: Keaton Lee's projects
- Project will be created on first deployment
- DNS propagation can take up to 48 hours
- SSL certificate auto-provisions after DNS is configured

## Troubleshooting
If issues occur, check:
- [ ] Build directory structure is correct
- [ ] DNS records are properly configured
- [ ] Browser console for errors
- [ ] Vercel deployment logs
- [ ] Unity build settings (Brotli compression enabled)

