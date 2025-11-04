# Quick Start - Deploy Mental Break to mentalbreak.io

## Prerequisites Checklist

- [ ] Unity Editor 2022.3+ with WebGL Build Support installed
- [ ] Vercel account (sign up at vercel.com if needed)
- [ ] Domain `mentalbreak.io` (or use Vercel default domain for testing)

## Deployment Steps (5 minutes)

### 1. Build Unity WebGL (2-3 minutes)

1. Open Unity project: `Mental_Break_AlphaV2.0`
2. **File > Build Settings** → Select **WebGL** → Click **Player Settings**
3. Configure:
   - Compression Format: **Brotli** ⚠️ (Critical!)
   - Code Optimization: **Size**
   - Company Name: Already set to "Mental Break"
   - Product Name: Already set to "Mental Break"
4. Click **Build** → Choose folder: `webgl-build` (in project root)
5. Wait for build (5-15 min)

### 2. Test Locally (30 seconds)

```bash
cd webgl-build
python -m http.server 8000
```

Visit `http://localhost:8000` - verify game loads.

### 3. Deploy to Vercel (1 minute)

**Option A: Vercel CLI (Fastest)**
```bash
npm install -g vercel
vercel login
vercel --prod
# When prompted: Directory = webgl-build
```

**Option B: Vercel Dashboard**
1. Go to vercel.com/dashboard
2. "Add New Project" → Import GitHub repo
3. Settings:
   - Root Directory: `webgl-build`
   - Build Command: (leave blank)
   - Output Directory: `.`
4. Deploy!

### 4. Connect Domain (if ready)

In Vercel Dashboard → Project Settings → Domains:
- Add domain: `mentalbreak.io`
- Follow DNS setup instructions

## Files Created

✅ `vercel.json` - Vercel deployment configuration  
✅ `DEPLOYMENT.md` - Detailed deployment guide  
✅ `BUILD_INSTRUCTIONS.md` - Unity build steps  
✅ `.gitignore` - Updated to exclude webgl-build/

## Next Steps After Deployment

- Test game playthrough (Run 1 → Run 4)
- Monitor Vercel analytics for errors
- Test across browsers (Chrome, Firefox, Safari)
- Optimize if build size is too large

## Troubleshooting

**Game doesn't load?** Check browser console for errors.

**Build too large?** Enable Brotli compression, reduce asset sizes.

**CORS errors?** Already handled in `vercel.json`.

**Need help?** See `DEPLOYMENT.md` for detailed troubleshooting.

---

**Ready?** Start with Step 1: Build Unity WebGL! 🚀

