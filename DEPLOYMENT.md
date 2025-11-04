# Mental Break - WebGL Deployment Guide

## Prerequisites

- Unity Editor 6.2 (Web Unity 6.2) with WebGL Build Support module installed
- Vercel account (free tier works)
- Domain: mentalbreak.io (or use Vercel's default domain for testing)

## Build Configuration

### Step 1: Configure Unity WebGL Build Settings

1. Open the Unity project: `Mental_Break_AlphaV2.0`
2. Go to **File > Build Settings**
3. Select **WebGL** platform (if not selected, click "Switch Platform")
4. Click **Player Settings** button
5. Configure the following:

   **Player Settings:**
   - Company Name: `Mental Break` (or your preferred name)
   - Product Name: `Mental Break`
   - Default Canvas Width: `1920` (or your target width)
   - Default Canvas Height: `1080` (or your target height)

   **Publishing Settings (WebGL):**
   - Compression Format: **Brotli** (recommended for Vercel)
   - Code Optimization: **Size** (for smaller builds)
   - Exception Support: **None** (smaller builds) or **Explicitly Thrown Exceptions Only**
   - Data Caching: **Enabled**

   **Other Settings:**
   - Strip Engine Code: **Enabled** (smaller builds)
   - WebGL Memory Size: Adjust based on your game (default 16MB, increase if needed)

6. Click **Build** (or **Build and Run** for testing)
7. Choose output folder: `webgl-build` (in project root)

### Step 2: Verify Build Output

After building, `webgl-build/` should contain:
```
webgl-build/
├── index.html          (Unity loader)
├── Build/              (Unity build files)
│   ├── *.unityweb      (Brotli compressed build)
│   ├── *.wasm          (WebAssembly)
│   └── *.data          (Asset data)
└── TemplateData/       (Unity UI assets)
    ├── UnityProgress.js
    └── ...
```

## Local Testing

Before deploying, test the build locally:

### Option 1: Python HTTP Server
```bash
cd webgl-build
python -m http.server 8000
# Or for Python 3:
python3 -m http.server 8000
```
Visit: `http://localhost:8000`

### Option 2: Node.js serve
```bash
npm install -g serve
cd webgl-build
serve -l 8000
```
Visit: `http://localhost:8000`

### Option 3: VS Code Live Server
Install "Live Server" extension in VS Code, right-click `index.html` → "Open with Live Server"

## Deployment to Vercel

### Method 1: Vercel CLI (Recommended)

**Quick Deploy Script:**
```powershell
# Use the provided deployment script
.\deploy-to-vercel.ps1 -Prod
```

**Manual Steps:**

1. Install Vercel CLI:
   ```powershell
   npm install -g vercel
   ```

2. Login to Vercel:
   ```powershell
   vercel login
   ```

3. Deploy from project root:
   ```powershell
   vercel --prod
   ```
   
   When prompted:
   - "Set up and deploy?": Yes
   - "Which scope?": Select your account or team (`team_w3aTDuy3oozvmAKuhIdJCGq3`)
   - "Link to existing project?": No (first time) or Yes (if updating)
   - "Project name?": `mental-break`
   - "Directory?": `webgl-build`
   - "Override settings?": No (unless you want to customize)

4. The deployment will provide a URL. Test it!

5. To link your domain:
   - Go to Vercel Dashboard → Project → Settings → Domains
   - Add domain: `mentalbreak.io`
   - Follow DNS configuration instructions

### Method 2: Vercel Dashboard (GitHub Integration)

1. Push your code to GitHub (if not already)
2. Go to [vercel.com/dashboard](https://vercel.com/dashboard)
3. Click **Add New Project**
4. Import your GitHub repository
5. Configure:
   - **Framework Preset**: Other
   - **Root Directory**: `webgl-build` (or leave blank if deploying from root)
   - **Build Command**: Leave blank (pre-built)
   - **Output Directory**: `.` (current directory)
6. Click **Deploy**

### Method 3: Deploy from webgl-build Directory

If you want to deploy only the built files:

1. Create a separate git repository in `webgl-build/`
2. Or copy `vercel.json` into `webgl-build/` and adjust paths
3. Deploy from that directory

## Domain Configuration

1. In Vercel Dashboard → Your Project → Settings → Domains
2. Add domain: `mentalbreak.io`
3. Follow DNS instructions:
   - Add CNAME record: `cname.vercel-dns.com`
   - Or A record: Vercel's IP (will be shown)
4. SSL certificate is automatically provisioned

## Troubleshooting

### Build Issues

- **Large file sizes**: Unity WebGL builds can be large. Vercel free tier allows 100MB per file. If exceeded, consider:
  - Enable compression (Brotli)
  - Reduce asset quality
  - Split builds if possible

- **Missing MIME types**: The `vercel.json` should handle this. If issues persist, check Vercel function logs.

- **CORS errors**: Unity WebGL may need CORS headers. Add to `vercel.json`:
  ```json
  "headers": {
    "Access-Control-Allow-Origin": "*"
  }
  ```

### Runtime Issues

- **Game doesn't load**: Check browser console. Common issues:
  - Incorrect file paths
  - Missing compression format
  - CORS issues

- **Audio not working**: Ensure audio files are in supported formats and check browser audio autoplay policies.

- **Performance issues**: 
  - Reduce texture sizes
  - Enable compression
  - Optimize Unity build settings

## Build Automation (Optional)

For automated builds, create `.github/workflows/deploy.yml`:

```yaml
name: Deploy to Vercel

on:
  push:
    branches: [ main ]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      
      # Note: Unity builds typically require Unity Editor
      # This workflow assumes manual build, then auto-deploy
      # For full automation, consider Unity Cloud Build or custom runner
      
      - name: Deploy to Vercel
        uses: amondnet/vercel-action@v20
        with:
          vercel-token: ${{ secrets.VERCEL_TOKEN }}
          vercel-org-id: ${{ secrets.VERCEL_ORG_ID }}
          vercel-project-id: ${{ secrets.VERCEL_PROJECT_ID }}
          working-directory: ./webgl-build
```

## Notes

- Unity WebGL builds are client-side only. No server-side processing needed.
- PlayerPrefs work in WebGL but are browser-local (cleared with browser data).
- Consider implementing cloud save if needed (separate service).
- Test across browsers: Chrome, Firefox, Safari, Edge.

