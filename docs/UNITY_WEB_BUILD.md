# Unity Web (Unity 6) Build Guide

Unity 6 replaces the legacy WebGL target with the "Unity Web" platform. Older WebGL1-era instructions will cause broken builds (for example attempting to force WebGL1 on iOS). Follow the checklist below whenever we update player settings, produce a new build, or upgrade Unity.

## 1. Player Settings Checklist

Edit ➜ Project Settings ➜ Player ➜ Web:

- **Graphics API / Rendering Backends**: leave on Auto. Unity 6 chooses WebGPU when possible and falls back to WebGL2. Do *not* try to add WebGL1 back into the list.
- **Power Preference**: set to `Low Power (1)` to keep iOS Safari from rejecting the context. This must match the JavaScript bootstrap expectations in `webgl-build/index.html`.
- **Compression Format**: `Gzip` to emit `.unityweb` bundles. If you switch to Brotli or Disable compression, update `webgl-build/index.html` URLs and `vercel.json` MIME/encoding rules at the same time.
- **Threads / SIMD**: keep disabled unless you also configure the COOP/COEP headers. (We currently ship with threads off, so no extra headers are required.)

## 2. Build Output Expectations

A successful build writes the following into `webgl-build/Build/`:

- `*.data.unityweb`
- `*.framework.js.unityweb`
- `*.wasm.unityweb`
- `*.loader.js` (left uncompressed)

If Unity emits non-`.unityweb` extensions, double-check the compression setting and rebuild before deploying.

## 3. JavaScript Bootstrap Rules

`webgl-build/index.html` is our custom template. Key rules:

- Do **not** call `canvas.getContext(...)` before Unity runs. Unity 6 decides between WebGPU and WebGL2 at runtime.
- The script clamps mobile DPR to `1` and, for iOS, sets `config.webglContextAttributes.powerPreference = "low-power"`. Keep those attributes aligned with the Player Settings noted above.
- We no longer ship any WebGL1 fallback. Removing that legacy code fixed the `GL.contexts[contextHandle]` crashes on iOS.
- Context loss is surfaced through banners and console logs so QA can report issues without digging into minified loader code.

When modifying the bootstrap, read the inline comments (tagged "Unity 6") and update this document if behaviour changes.

## 4. Deployment Workflow

1. Build via **File ➜ Build Settings ➜ Web ➜ Build** and target `Mental_Break_AlphaV2.0/webgl-build`.
2. Commit the regenerated `Build/` artifacts, `index.html`, and any template assets.
3. Push to your branch and wait for the Vercel preview.
4. Verify `vercel.json` contains the gzip `Content-Encoding` overrides for the `.unityweb` files.

## 5. QA Checklist

Run these smoke tests after every deployment:

- iOS Safari: load the preview, ensure no `GL.contexts` errors, check for `[Unity Web]` logs if a context loss occurs.
- iOS Chrome: repeat the same load test.
- Desktop Chrome / Safari: confirm no regressions.
- If the loading bar hangs, inspect the browser network panel and confirm `.unityweb` responses return with `Content-Encoding: gzip`.

## 6. Troubleshooting References

- `docs/IOS_WEB_DIAGNOSIS.md` – specific iOS crash analysis and history.
- `docs/TROUBLESHOOTING.md` – general web build issues, caching guidelines, and Vercel tips.
- Unity manual: <https://docs.unity3d.com/6000.0/Documentation/Manual/web-platform.html>

Keep this guide up to date whenever Unity releases new Unity Web requirements or when we change hosting infrastructure.
