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

## Alternative Solutions (If Above Doesn't Work)

### Option A: User-Agent Detection Fallback
Modify `index.html` to detect iOS and force WebGL 1.0 context creation (if Unity 6.2 supports it):
```javascript
if (/iPhone|iPad|iPod/i.test(navigator.userAgent)) {
  const gl = canvas.getContext("webgl") || canvas.getContext("experimental-webgl");
  // Force WebGL 1.0 on iOS
}
```
**Note**: Unity 6.2 removed WebGL 1.0 support, so this may not be viable.

### Option B: Downgrade Unity Version
If Power Preference change doesn't resolve the issue, consider:
- Testing with Unity 2022 LTS (which still supports WebGL 1.0 fallback)
- This is a significant change and should be a last resort

### Option C: Contact Unity Support
If the issue persists after changing Power Preference:
- File a bug report with Unity Issue Tracker
- Include: Unity version (6.2), iOS version, browser version, error stack trace
- Reference: `GL.contexts[contextHandle]` undefined on iOS WebGL2 initialization

## Implemented Fallback Solution

Since changing Power Preference alone didn't resolve the issue, a JavaScript fallback has been implemented to intercept WebGL2 context creation on iOS and automatically fall back to WebGL1 if WebGL2 fails.

### Implementation Details

**1. Modified `webgl-build/index.html`:**
- Added iOS detection and WebGL context attributes configuration
- Sets `powerPreference: "low-power"` and `antialias: false` for iOS devices
- Attempts to request WebGL1 via `config.majorVersion = 1` (though Unity 6.2 may not support this)

**2. Patched `webgl-build/Build/webgl-build.loader.js`:**
- Added a wrapper function at the beginning of the file that intercepts `HTMLCanvasElement.prototype.getContext`
- On iOS devices, when WebGL2 context creation is requested:
  1. First attempts WebGL2 with low-power settings
  2. If WebGL2 fails, automatically falls back to WebGL1 with low-power settings
  3. Prevents the `GL.contexts[contextHandle]` error by ensuring a valid context is always returned

**Code Location:**
- `webgl-build/index.html` (lines ~345-381): iOS detection and config setup
- `webgl-build/Build/webgl-build.loader.js` (lines 1-33): Canvas context wrapper

### Maintenance Notes

**⚠️ IMPORTANT: These patches will be overwritten when Unity rebuilds the WebGL player!**

After each Unity rebuild, you must reapply these patches:

1. **Re-patch `webgl-build/Build/webgl-build.loader.js`:**
   - The file is minified and will be regenerated by Unity
   - Prepend the iOS WebGL fallback wrapper code (see lines 1-33 of current file)
   - The wrapper must be added BEFORE the `function createUnityInstance` declaration

2. **Verify `webgl-build/index.html` changes persist:**
   - Unity may regenerate `index.html` if you're using a custom template
   - Ensure the iOS detection and `config.webglContextAttributes` code remains
   - Check that `config.majorVersion = 1` is set for iOS devices

3. **Automation Option:**
   - Consider creating a post-build script to automatically reapply these patches
   - Or document the manual steps in your build process

### How the Fallback Works

1. **Context Creation Interception:**
   - The wrapper intercepts all `canvas.getContext("webgl2")` calls on iOS
   - Ensures low-power settings are applied before context creation
   - Catches any exceptions during WebGL2 initialization

2. **Automatic Fallback:**
   - If WebGL2 fails (returns null or throws), the wrapper automatically tries WebGL1
   - Uses the same low-power attributes for WebGL1
   - Returns a valid context or null (preventing the undefined error)

3. **Error Prevention:**
   - By ensuring a context is always returned (or null), Unity's cleanup code won't try to destroy a non-existent context
   - This prevents the `GL.contexts[contextHandle]` undefined error

### Limitations

- **Unity 6.2 WebGL1 Support:** Unity 6.2 officially removed WebGL1 support. The fallback attempts WebGL1, but Unity's runtime may not fully support it. If WebGL1 fallback doesn't work, the game may still fail to load on iOS.
- **Performance Impact:** WebGL1 has fewer features than WebGL2, which may affect rendering quality or performance.
- **Manual Maintenance:** Patches must be reapplied after each Unity rebuild.

## Testing Checklist

After implementing the fix:
- [ ] Power Preference changed to Low Power/Balanced/Default in Unity Player Settings
- [ ] WebGL build completed successfully
- [ ] `webgl-build.loader.js` shows `powerPreference:1` or `powerPreference:2`
- [ ] iOS WebGL fallback wrapper code added to `webgl-build.loader.js` (lines 1-33)
- [ ] iOS detection and context attributes configured in `index.html`
- [ ] Build deployed to Vercel preview (`v3.1` branch)
- [ ] Tested on iOS Safari (latest version)
- [ ] Tested on iOS Chrome (if available)
- [ ] Check browser console for `[iOS WebGL Fallback]` messages
- [ ] Tested on desktop Chrome (regression test)
- [ ] Tested on desktop Safari (regression test)
- [ ] No console errors on iOS
- [ ] Game loads and plays correctly on iOS

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

