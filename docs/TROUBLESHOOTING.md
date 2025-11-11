# Troubleshooting Guide

Common issues and solutions for building and deploying Mental Break.

## Build Issues

### Build Fails
- Check Unity Console for errors
- Ensure all scenes are added to Build Settings > Scenes In Build
- Verify Unity Editor version is 6.2 with WebGL support installed

### Build Too Large
- Enable Gzip compression (recommended) or Brotli
- Strip engine code
- Reduce texture sizes (import settings)
- Compress audio files
- Remove unused assets
- Monitor build size (Vercel free tier: 100MB per file limit)

### Game Doesn't Run Locally
- Check browser console for errors
- Ensure using HTTP server (not `file://` protocol)
- Verify all required files are present in `webgl-build/`
- Test with: `python -m http.server 8000` in `webgl-build/` directory

## Deployment Issues

### webgl-build Directory Not Showing in Vercel

**Solution 1: Manually Type the Path (Easiest)**
- When Vercel asks for "Root Directory", don't use the selector
- Manually type: `Mental_Break_AlphaV2.0/webgl-build`
- Press Enter or click Continue

**Solution 2: Select Parent Directory**
- Select `Mental_Break_AlphaV2.0` in the directory selector
- After deployment starts, go to **Settings** → **General**
- Change **Root Directory** to: `Mental_Break_AlphaV2.0/webgl-build`
- Save and redeploy

**Solution 3: Use Vercel CLI**
- Install Node.js and Vercel CLI
- Navigate to `webgl-build/` directory
- Run: `vercel --prod`

### Domain Not Working
- Check DNS records are correctly configured
- Wait for DNS propagation (can take up to 48 hours)
- Verify domain is added in Vercel dashboard
- Ensure domain is assigned to Production environment (not a specific deployment)

### Game Won't Load After Deployment
- Check browser console for errors
- Verify file paths are correct
- Ensure using HTTPS (not HTTP)
- Check that compression format matches build (Gzip/Brotli)
- Verify `vercel.json` has correct routes and headers

## Cache Issues

### Dark Theme Not Showing on mentalbreak.io

**Solution 1: Clear Browser Cache**
- Press `Ctrl + Shift + Delete` (or `Cmd + Shift + Delete` on Mac)
- Select "Cached images and files"
- Time range: "All time"
- Click "Clear data"
- Or hard refresh: `Ctrl + Shift + R` (or `Cmd + Shift + R` on Mac)

**Solution 2: Purge Vercel CDN Cache**
- Go to Vercel Dashboard → Your Project → **Deployments**
- Find the latest deployment
- Click **"..."** menu → **"Redeploy"**
- Or use CLI: `vercel --prod --force`

**Solution 3: Verify Domain Assignment**
- Go to Vercel Dashboard → Your Project → **Settings** → **Domains**
- Make sure `mentalbreak.io` is assigned to **Production** environment
- If assigned to a specific deployment, change it to "Production"

**Prevention:**
- Always use `vercel --prod --force` when deploying important changes
- Or use Vercel's "Redeploy" option which clears cache
- Consider adding a version query parameter to force cache bust: `?v=2.0`

## iOS WebGL Issues

### iOS Crash: `GL.contexts[contextHandle]` Undefined

**Root Cause:**
Unity WebGL build is configured with Power Preference = High Performance, which iOS Safari cannot reliably initialize.

**Solution:**

1. **Change Power Preference in Unity:**
   - Open Unity Editor
   - Go to **Edit > Project Settings > Player**
   - Select **Web** platform tab
   - Expand **Publishing Settings**
   - Find **Power Preference** dropdown
   - Change to **"Low Power"** (value `1`) or **"Default"** (value `2`)
   - Avoid `0` (High Performance) for iOS compatibility
   - **IMPORTANT**: Save the project after changing
   - **IMPORTANT**: Rebuild the WebGL player for the change to take effect

2. **Rebuild WebGL Player:**
   - **File > Build Settings**
   - Select **WebGL** platform
   - Click **Build**
   - Build to: `Mental_Break_AlphaV2.0/webgl-build`

3. **Verify Build Output:**
   - Check `webgl-build/Build/webgl-build.loader.js`
   - Should contain: `powerPreference:1` or `powerPreference:2` (NOT `0`)

4. **Use Gzip Compression:**
   - Gzip is more compatible with iOS than Brotli
   - See [Compression Configuration](#compression-configuration) below

**Alternative Solutions:**
- If Power Preference change doesn't work, see [iOS WebGL Diagnosis](IOS_WEBGL_DIAGNOSIS.md) for detailed fallback solutions
- Consider testing with Unity 2022 LTS if issues persist (last resort)

### iOS Game Won't Load

- Ensure Power Preference is set to "Low Power" or "Default"
- Use Gzip compression instead of Brotli
- Test on actual iOS device (Safari and Chrome for iOS)
- Clear browser cache/site data before testing
- Check browser console for errors

## Compression Configuration

### Gzip Compression (Recommended for iOS)

**Unity Build Settings:**
- **Compression Format**: **Gzip**
- Build creates `.unityweb` files:
  - `webgl-build.data.unityweb`
  - `webgl-build.framework.js.unityweb`
  - `webgl-build.wasm.unityweb`

**Required Configuration:**
- `index.html` must reference `.unityweb` files
- `vercel.json` must serve `.unityweb` files with `Content-Encoding: gzip`
- Files are automatically handled by `vercel.json` configuration

### Brotli Compression

**Unity Build Settings:**
- **Compression Format**: **Brotli**
- Better compression than Gzip (~15-20% smaller)
- May have iOS compatibility issues

**Required Configuration:**
- `index.html` must reference `.br` files
- `vercel.json` must serve `.br` files with `Content-Encoding: br`
- Files are automatically handled by `vercel.json` configuration

### Uncompressed Builds

**Unity Build Settings:**
- **Compression Format**: **Disabled**
- Build creates standard files:
  - `webgl-build.data`
  - `webgl-build.framework.js`
  - `webgl-build.wasm`

**Required Configuration:**
- `index.html` must reference uncompressed filenames
- `vercel.json` serves files without compression headers
- Files are automatically handled by `vercel.json` configuration

### Switching Compression Modes

**To use Gzip compression (recommended for iOS):**
1. Unity: **Player Settings > Web > Publishing Settings > Compression Format** → **Gzip**
2. Build WebGL player
3. Verify files have `.unityweb` extensions
4. Ensure `index.html` references `.unityweb` files
5. Deploy

**To use Brotli compression:**
1. Unity: **Player Settings > Web > Publishing Settings > Compression Format** → **Brotli**
2. Build WebGL player
3. Verify files have `.br` extensions
4. Ensure `index.html` references `.br` files
5. Deploy

**To use uncompressed (for debugging):**
1. Unity: **Player Settings > Web > Publishing Settings > Compression Format** → **Disabled**
2. Build WebGL player
3. Verify files have `.js`, `.wasm`, `.data` extensions (no `.unityweb` or `.br`)
4. Update `index.html` to reference uncompressed filenames
5. Deploy

## Runtime Issues

### Audio Not Working
- Ensure audio files are in supported formats
- Check browser audio autoplay policies
- Verify audio files are included in build

### Performance Issues
- Reduce texture sizes
- Enable compression
- Optimize Unity build settings
- Test on target browsers and devices

### UI Elements Not Appearing
- Leaderboard, engagement, and sanity UI should appear on first load
- If not, check browser console for errors
- Verify all UI components are properly initialized
- Test with hard refresh (`Ctrl + Shift + R`)

## Getting Help

If issues persist:
1. Check browser console for errors
2. Check Vercel deployment logs
3. Verify Unity build settings match documentation
4. Test locally before deploying
5. Review [Build Instructions](BUILD.md) and [Deployment Guide](DEPLOYMENT.md)

For detailed iOS WebGL diagnosis, see: [iOS WebGL Diagnosis](IOS_WEBGL_DIAGNOSIS.md)

