# Unity WebGL Build Instructions

Complete guide for building the Unity WebGL project for deployment.

## Prerequisites

- Unity Editor 6.2 with WebGL Build Support module installed
- Project located at: `Mental_Break_AlphaV2.0`

## Build Configuration

### Step 1: Configure Build Settings

1. Open Unity Editor with the project: `Mental_Break_AlphaV2.0`
2. Go to: **File > Build Settings**
3. Select **WebGL** platform (if not selected, click "Switch Platform" and wait)
4. Click **Player Settings**

### Step 2: Player Settings Configuration

**General:**
- Company Name: `Mental Break`
- Product Name: `Mental Break`
- Default Icon: (optional, set if you have one)

**Resolution and Presentation (WebGL tab):**
- Default Canvas Width: `1920`
- Default Canvas Height: `1080`
- Run In Background: ✅ (checked)
- Display Resolution Dialog: `Disabled`

**Publishing Settings (expand WebGL section):**
- **Compression Format**: **Gzip** ⚠️ (Recommended for iOS compatibility)
  - Alternative: **Brotli** (better compression, but may have iOS issues)
- Code Optimization: **Size**
- Exception Support: `None` (or `Explicitly Thrown Exceptions Only`)
- Data Caching: ✅ Enabled
- **Power Preference**: **Low Power** or **Default** (for iOS compatibility)
  - Avoid "High Performance" as it can cause iOS crashes

**Other Settings:**
- Strip Engine Code: ✅ Enabled
- Managed Stripping Level: `Low` or `Medium` (test if game works)
- WebGL Memory Size: `16` (increase if you get out-of-memory errors)

### Step 3: Setup Store UI (Required before building)

1. Open the main game scene (e.g., `MVPScene.unity`) that will be built
2. Go to: **Tools > Setup Store UI**
3. Verify that `StorePanel` appears in the Hierarchy under `DontDestroyOnLoad/Dialogue System/Canvas`
4. **IMPORTANT**: Save the scene after setup to ensure StorePanel persists in the build
5. If StorePanel is missing, the store command will fail in WebGL builds

### Step 4: Build

1. In Build Settings window, click **Build**
2. Choose output folder: Create/select `webgl-build` in the project root
3. Wait for build to complete (may take 5-15 minutes depending on project size)

### Step 5: Verify Build Output

After building, check that `webgl-build/` folder contains:
- `index.html` - Unity loader
- `Build/` folder - Contains `.unityweb`, `.wasm`, `.data` files (or `.js`, `.wasm`, `.data` if uncompressed)
- `TemplateData/` folder - UI assets

**For Gzip compression**, files will have `.unityweb` extension:
- `webgl-build.data.unityweb`
- `webgl-build.framework.js.unityweb`
- `webgl-build.wasm.unityweb`

**For uncompressed builds**, files will have standard extensions:
- `webgl-build.data`
- `webgl-build.framework.js`
- `webgl-build.wasm`

### Step 6: Test Locally

Before deploying, test the build locally:

**Option 1: Python HTTP Server**
```bash
cd webgl-build
python -m http.server 8000
# Or for Python 3:
python3 -m http.server 8000
```
Visit: `http://localhost:8000`

**Option 2: Node.js serve**
```bash
npm install -g serve
cd webgl-build
serve -l 8000
```
Visit: `http://localhost:8000`

**Option 3: VS Code Live Server**
Install "Live Server" extension in VS Code, right-click `index.html` → "Open with Live Server"

## Build Optimization Tips

### Reduce Build Size
- Enable Gzip compression (recommended) or Brotli
- Strip engine code
- Reduce texture sizes (import settings)
- Compress audio files (use compressed formats)
- Remove unused assets

### Performance
- Target appropriate WebGL memory size
- Test on target browsers
- Monitor build size (Vercel free tier: 100MB per file limit)

## Common Issues

### Build Fails
- Check Unity Console for errors
- Ensure all scenes are added to Build Settings > Scenes In Build

### Build Too Large
- Optimize assets, enable compression
- Reduce texture sizes
- Remove unused assets

### Game Doesn't Run Locally
- Check browser console
- Ensure using HTTP server (not `file://` protocol)
- Verify all required files are present

### Missing Files
- Ensure all scenes are added to Build Settings > Scenes In Build
- Check that `webgl-build/` directory structure is correct

### Store Command Fails
- Ensure StorePanel exists in the scene (run `Tools > Setup Store UI` and save the scene before building)

### Favicon 404 Error
- If you see `/TemplateData/favicon-48x48.png` 404 errors, copy `TemplateData/old/favicon-48x48.png` to `TemplateData/` (non-critical, `favicon.ico` should work)

### iOS Crashes
- Ensure Power Preference is set to "Low Power" or "Default" (not "High Performance")
- Use Gzip compression instead of Brotli for better iOS compatibility
- See [Troubleshooting Guide](TROUBLESHOOTING.md#ios-webgl-issues) for detailed iOS fixes

## Next Steps

After building successfully, proceed to [Deployment Guide](DEPLOYMENT.md) to deploy to Vercel.

