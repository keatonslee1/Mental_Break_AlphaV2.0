# Rebuild Unity with Gzip Compression for iOS Support

## Why This Is Needed

The JavaScript Brotli decompression fallback isn't working reliably on iOS. Gzip compression is natively supported by all browsers including iOS Safari, so rebuilding with Gzip will fix the mobile loading issue.

## Steps to Rebuild

1. **Open Unity Editor 6.2**
   - Open the project: `Mental_Break_AlphaV2.0`

2. **Change Compression Format**
   - Go to: `File > Build Settings`
   - Select **WebGL** platform
   - Click **Player Settings**
   - Expand **Publishing Settings** (WebGL section)
   - Change **Compression Format**: **Brotli** → **Gzip**
   - Keep other settings the same:
     - Code Optimization: Size
     - Data Caching: Enabled
     - Strip Engine Code: Enabled

3. **Rebuild**
   - Click **Build** in Build Settings
   - Output to: `webgl-build/` (overwrite existing)
   - Wait for build to complete

4. **Update vercel.json**
   - After rebuild, update routes from `.br` to `.gz`
   - Update `Content-Encoding` from `br` to `gzip`

5. **Update index.html**
   - Change file references from `.br` to `.gz`
   - Remove JavaScript Brotli fallback code (no longer needed)

6. **Deploy**
   - Commit and push changes
   - Vercel will auto-deploy

## Benefits

- ✅ Works on ALL browsers (including iOS Safari)
- ✅ No JavaScript fallback needed
- ✅ Simpler, more reliable
- ✅ Slightly larger files (~15-20% bigger than Brotli) but universal compatibility

## Note

Gzip files will be slightly larger than Brotli, but the universal compatibility is worth it for mobile support.

