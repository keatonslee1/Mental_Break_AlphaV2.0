1. swap-canvas-dimensions — In `mobile-play.html`, set the Unity canvas pixel dimensions to 600×960 (swap the native width/height) before rotation so the game renders into a portrait buffer, then rotate the wrapper back to display landscape.
2. adjust-scaling — Recalculate wrapper transforms/scaling to accommodate the swapped canvas so it fills the viewport without letterboxing.
3. verify-overlay — Reload metrics to confirm the module canvas now reports 600×960 (or equivalent) and that the game visuals use the full space.
4. tidy-up — After confirming, remove or comment the on-page debug overlay (optional per user confirmation).
