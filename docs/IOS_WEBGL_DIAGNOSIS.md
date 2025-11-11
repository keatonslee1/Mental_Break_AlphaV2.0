# iOS WebGL Crash Diagnosis Report

## Error Summary
**Error**: `TypeError: undefined is not an object (evaluating 'GL.contexts[contextHandle].GLctx')`  
**Platform**: iOS (Chrome for iOS, likely Safari as well)  
**Build**: Unity 6.2 WebGL  
**Date**: January 2025

## Root Cause Analysis

### Primary Issue: Power Preference Setting
The Unity WebGL build is configured with **Power Preference = High Performance (0)**, which iOS Safari cannot reliably initialize. When Safari fails to create a high-performance WebGL2 context, Unity attempts to destroy a context that was never successfully created, leading to the `GL.contexts[contextHandle]` undefined error.

### Evidence
1. **ProjectSettings.asset** (line 588): `webGLPowerPreference: 0` (High Performance)
2. **webgl-build.loader.js**: Hardcoded `powerPreference:0` in WebGL context attributes
3. **Previous builds**: Older builds used `powerPreference:2` (Default), which worked better on iOS

### Verified Settings (Correct)
- ✅ **Threads Support**: `webGLThreadsSupport: 0` (disabled) - Correct
- ✅ **SIMD Support**: Not explicitly found, but threads disabled implies SIMD is also off
- ✅ **COOP/COEP Headers**: Not required (threads are disabled)
- ✅ **WebGL 1.0 Fallback**: Unity 6.2 removed WebGL 1.0 support (not available)

### Additional Findings
- **No COOP/COEP headers needed**: Since threads are disabled, SharedArrayBuffer is not used, so Cross-Origin-Opener-Policy and Cross-Origin-Embedder-Policy headers are not required.
- **Build configuration**: The build uses uncompressed files (no `.gz`), which is correctly configured in `vercel.json`.
- **Font MIME types**: Properly configured in `vercel.json` (no font loading issues detected).

## Recommended Solution

### Step 1: Change Power Preference in Unity
1. Open Unity Editor
2. Go to **Edit > Project Settings > Player**
3. Select **Web** platform tab
4. Expand **Publishing Settings**
5. Find **Power Preference** dropdown
6. Change to **"Low Power"** (value `1`) for maximum iOS compatibility
   - If "Low Power" doesn't exist, try **"Balanced"** (value `1`) or **"Default"** (value `2`)
   - Avoid `0` (High Performance) for iOS compatibility
7. **IMPORTANT**: Save the project (Ctrl+S / Cmd+S) after changing the setting
8. **IMPORTANT**: You must rebuild the WebGL player for the change to take effect

### Step 2: Rebuild WebGL Player
1. **File > Build Settings**
2. Select **WebGL** platform
3. Click **Build** (or **Build and Run**)
4. Build to: `Mental_Break_AlphaV2.0/webgl-build`

### Step 3: Verify Build Output
After rebuilding, verify that `webgl-build/Build/webgl-build.loader.js` contains:
```javascript
powerPreference:1  // Low Power (preferred for iOS) or powerPreference:2 (Default), NOT 0
```

**Note**: If Unity shows "Default" but the build still has `powerPreference:0`, you may need to:
- Ensure you saved the project after changing the setting
- Rebuild the WebGL player (the setting only applies to new builds)
- If "Default" still fails, try "Low Power" (value `1`) instead

### Step 4: Deploy and Test
1. Commit and push the new build to the `v3.1` branch
2. Wait for Vercel preview deployment
3. Test on iOS device (Safari and Chrome for iOS)
4. Clear browser cache/site data before testing if needed

## Implementation Details

The shipped template now relies on Unity's WebGL2 bootstrap while nudging iOS hardware into a low-power graphics path instead of forcing a WebGL 1 fallback.

- `webgl-build/index.html` adds the mobile viewport meta tag, toggles the Unity container into mobile layout, clamps the device pixel ratio to `1`, and, on iOS, sets `config.webglContextAttributes` with `powerPreference: "low-power"`, disabled antialiasing, and conservative buffer flags. It intentionally does **not** set `config.majorVersion` or call `canvas.getContext(...)` ahead of Unity, keeping the runtime in charge of context creation.
- The script attaches `webglcontextlost` / `webglcontextrestored` listeners that log to the console and raise a banner via `unityShowBanner` so QA can see when the browser drops the context.
- Error handling in the `createUnityInstance(...).catch(...)` path now logs the original failure object before surfacing the banner, which simplifies remote debugging.
- Unity's generated `webgl-build/Build/webgl-build.loader.js` is left untouched, so future WebGL builds do not need manual repatching.

## Gzip Compression Configuration

The project uses **Gzip compression** (Unity's `.unityweb` format) to reduce file sizes for iOS compatibility. This requires specific configuration:

### Build Output Files

When Unity builds with **Gzip compression** enabled, it creates:
- `webgl-build.data.unityweb` (compressed data file)
- `webgl-build.framework.js.unityweb` (compressed framework)
- `webgl-build.wasm.unityweb` (compressed WebAssembly)
- `webgl-build.loader.js` (uncompressed - must remain uncompressed)

### Required Configuration

**1. `index.html` must reference `.unityweb` files:**
```javascript
const config = {
  dataUrl: buildUrl + "/webgl-build.data.unityweb",
  frameworkUrl: buildUrl + "/webgl-build.framework.js.unityweb",
  codeUrl: buildUrl + "/webgl-build.wasm.unityweb",
  // ... other config
};
```

**2. `vercel.json` must serve `.unityweb` files with correct headers:**
- `.framework.js.unityweb` → `Content-Type: application/javascript` + `Content-Encoding: gzip`
- `.wasm.unityweb` → `Content-Type: application/wasm` + `Content-Encoding: gzip`
- `.data.unityweb` → `Content-Type: application/octet-stream` + `Content-Encoding: gzip`

**3. After each Unity rebuild with compression:**
- Verify `index.html` references `.unityweb` files (not `.js`, `.wasm`, `.data`)
- Ensure `vercel.json` has the correct `.unityweb` routes
- Smoke test the iOS flow to confirm the low-power context helpers still load as expected

### Switching Between Compression Modes

**To use Gzip compression (recommended for iOS):**
1. Unity: **Player Settings > Web > Publishing Settings > Compression Format** → **Gzip**
2. Build WebGL player
3. Verify files have `.unityweb` extensions
4. Ensure `index.html` references `.unityweb` files
5. Deploy

**To use uncompressed (for debugging):**
1. Unity: **Player Settings > Web > Publishing Settings > Compression Format** → **Disabled**
2. Build WebGL player
3. Verify files have `.js`, `.wasm`, `.data` extensions (no `.unityweb`)
4. Update `index.html` to reference uncompressed filenames
5. Deploy

## Testing Checklist

After implementing the fix:
- [ ] `webGLPowerPreference` remains set to `1` (low-power friendly) in `ProjectSettings/ProjectSettings.asset`
- [ ] The WebGL build succeeds and produces updated `.unityweb` artifacts
- [ ] `webgl-build/index.html` shows the iOS-specific `webglContextAttributes` without any `config.majorVersion` override
- [ ] Deploy to a Vercel preview (e.g., branch `v3.1`) and load on iOS Safari
- [ ] Repeat on iOS Chrome to confirm the absence of `GL.contexts` errors
- [ ] Watch the console for `[Unity Web]` logs when testing context loss; verify the in-page banner surfaces warnings instead of crashing
- [ ] Run desktop regression tests on Chrome and Safari

## Additional Notes

### Why iOS Safari Struggles with High Performance Mode
- iOS Safari has stricter memory and power management
- High Performance WebGL contexts require more GPU resources
- Safari may reject high-performance context creation to preserve battery/thermal limits
- When context creation fails, Unity's cleanup code tries to destroy a non-existent context

### Why Balanced/Default Works Better
- Less aggressive GPU resource requests
- Better compatibility with mobile browser constraints
- Still provides adequate performance for most games
- More reliable context initialization across devices

## References
- Unity 6.2 Player Settings documentation
- WebGL Power Preference API: https://developer.mozilla.org/en-US/docs/Web/API/WebGLRenderingContext/getContext
- Unity Issue Tracker: https://issuetracker.unity3d.com/

