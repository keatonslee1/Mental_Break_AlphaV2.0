# Fix Dark Theme Not Showing on mentalbreak.io

## The Problem
The dark theme works on preview URLs but not on `mentalbreak.io` because of caching.

## Solutions (Try in Order)

### Solution 1: Clear Browser Cache (Easiest)

**For Chrome/Edge:**
1. Press `Ctrl + Shift + Delete` (or `Cmd + Shift + Delete` on Mac)
2. Select "Cached images and files"
3. Time range: "All time"
4. Click "Clear data"
5. Visit `mentalbreak.io` again

**Or Hard Refresh:**
- Press `Ctrl + Shift + R` (or `Cmd + Shift + R` on Mac) on the `mentalbreak.io` page

### Solution 2: Purge Vercel CDN Cache

1. Go to https://vercel.com/dashboard
2. Click your project: `mental-break`
3. Go to **Deployments** tab
4. Find the latest deployment (should have your dark theme changes)
5. Click the **"..."** menu (three dots)
6. Click **"Redeploy"** - this will create a new deployment and clear cache

**Or use Vercel CLI:**
```bash
vercel --prod --force
```

### Solution 3: Verify Domain Assignment

1. Go to Vercel Dashboard → Your Project → **Settings** → **Domains**
2. Make sure `mentalbreak.io` is assigned to the **Production** environment
3. If it's assigned to a specific deployment, change it to "Production" so it always uses the latest

### Solution 4: Check Which Deployment the Domain Points To

1. Go to Vercel Dashboard → Your Project → **Deployments**
2. Look for the latest deployment with dark theme
3. Click on it
4. Check if `mentalbreak.io` is listed under "Domains"
5. If not, or if it points to an older deployment, reassign it

### Solution 5: Force Cache Clear via Vercel Dashboard

1. Go to Vercel Dashboard → Your Project → **Settings** → **Domains**
2. Click on `mentalbreak.io`
3. Click **"Remove"** (temporarily)
4. Wait 30 seconds
5. Click **"Add Domain"** again and add `mentalbreak.io`
6. This forces a fresh cache

## Quick Test

After clearing cache, check if the dark theme is working:
1. Visit `mentalbreak.io`
2. Right-click → "Inspect" (or F12)
3. Go to "Network" tab
4. Check "Disable cache"
5. Refresh the page
6. Look for `style.css` - it should have the dark theme styles

## If Still Not Working

1. Check the Vercel deployment logs to see if the latest deployment is live
2. Verify the domain DNS is pointing to Vercel correctly
3. Wait 5-10 minutes for CDN propagation (can take time)

## Prevention

To avoid this in the future:
- Always use `vercel --prod --force` when deploying important changes
- Or use Vercel's "Redeploy" option which clears cache
- Consider adding a version query parameter to force cache bust: `?v=2.0`

