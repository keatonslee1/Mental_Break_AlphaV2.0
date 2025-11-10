# Unity WebGL Build Instructions

## Quick Build Steps

1. **Open Unity Editor** with the project: `Mental_Break_AlphaV2.0`

2. **Configure Build Settings:**
   - Go to: `File > Build Settings`
   - Platform: Select **WebGL** (if not selected, click "Switch Platform" and wait)
   - Click **Player Settings**

3. **Player Settings Configuration:**

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
   - Compression Format: **Brotli** ⚠️ (Important for Vercel)
   - Code Optimization: **Size**
   - Exception Support: `None` (or `Explicitly Thrown Exceptions Only`)
   - Data Caching: ✅ Enabled

   **Other Settings:**
   - Strip Engine Code: ✅ Enabled
   - Managed Stripping Level: `Low` or `Medium` (test if game works)
   - WebGL Memory Size: `16` (increase if you get out-of-memory errors)

4. **Setup Store UI (Required before building):**
   - Open the main game scene (e.g., `MVPScene.unity`) that will be built
   - Go to: `Tools > Setup Store UI`
   - Verify that `StorePanel` appears in the Hierarchy under `DontDestroyOnLoad/Dialogue System/Canvas`
   - **IMPORTANT**: Save the scene after setup to ensure StorePanel persists in the build
   - If StorePanel is missing, the store command will fail in WebGL builds

5. **Build:**
   - In Build Settings window, click **Build**
   - Choose output folder: Create/select `webgl-build` in the project root
   - Wait for build to complete (may take 5-15 minutes depending on project size)

6. **Verify Build:**
   - Check that `webgl-build/` folder contains:
     - `index.html`
     - `Build/` folder (with .unityweb, .wasm, .data files)
     - `TemplateData/` folder

7. **Test Locally** (see DEPLOYMENT.md for instructions)

## Build Optimization Tips

- **Reduce Build Size:**
  - Enable Brotli compression
  - Strip engine code
  - Reduce texture sizes (import settings)
  - Compress audio files (use compressed formats)
  - Remove unused assets

- **Performance:**
  - Target appropriate WebGL memory size
  - Test on target browsers
  - Monitor build size (Vercel free tier: 100MB per file limit)

## Common Issues

- **Build fails**: Check Unity Console for errors
- **Build too large**: Optimize assets, enable compression
- **Game doesn't run locally**: Check browser console, ensure using HTTP server (not file://)
- **Missing files**: Ensure all scenes are added to Build Settings > Scenes In Build
- **Store command fails**: Ensure StorePanel exists in the scene (run `Tools > Setup Store UI` and save the scene before building)
- **Favicon 404 error**: If you see `/TemplateData/favicon-48x48.png` 404 errors, copy `TemplateData/old/favicon-48x48.png` to `TemplateData/` (non-critical, `favicon.ico` should work)

## Next Steps

After building, see `DEPLOYMENT.md` for deployment to Vercel.

