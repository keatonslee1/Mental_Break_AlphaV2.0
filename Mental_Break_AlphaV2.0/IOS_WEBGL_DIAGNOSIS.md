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

## Testing Checklist

After implementing the fix:
- [ ] Power Preference changed to Balanced/Default in Unity Player Settings
- [ ] WebGL build completed successfully
- [ ] `webgl-build.loader.js` shows `powerPreference:1` or `powerPreference:2`
- [ ] Build deployed to Vercel preview (`v3.1` branch)
- [ ] Tested on iOS Safari (latest version)
- [ ] Tested on iOS Chrome (if available)
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

