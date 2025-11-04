# Vercel Deployment - webgl-build Not Showing

## The Problem
The `webgl-build` folder is in your GitHub repo at `Mental_Break_AlphaV2.0/webgl-build`, but Vercel's directory selector isn't showing it.

## Solutions

### Solution 1: Manually Type the Path (Easiest)

When Vercel asks for "Root Directory", **don't use the selector**. Instead:

1. Click on the text field where it says "Root Directory"
2. **Manually type** this exact path:
   ```
   Mental_Break_AlphaV2.0/webgl-build
   ```
3. Press Enter or click Continue

This should work even if the directory doesn't show in the list.

### Solution 2: Select Parent Directory and Configure

1. In the directory selector, select: `Mental_Break_AlphaV2.0`
2. Click Continue
3. After deployment starts, go to **Settings** → **General**
4. Change **Root Directory** to: `Mental_Break_AlphaV2.0/webgl-build`
5. Save and redeploy

### Solution 3: Refresh/Retry

1. Cancel the current deployment
2. Refresh the Vercel page (F5)
3. Try importing again
4. The directory might appear after a refresh

### Solution 4: Use Vercel CLI Instead

If the web interface keeps failing:

1. Install Node.js: https://nodejs.org/
2. Open Command Prompt
3. Run:
   ```
   npm install -g vercel
   vercel login
   cd Mental_Break_AlphaV2.0\webgl-build
   vercel --prod
   ```

## Recommended: Solution 1 (Manual Path)

Just type `Mental_Break_AlphaV2.0/webgl-build` in the Root Directory field - it should accept it even if it's not in the dropdown list.

