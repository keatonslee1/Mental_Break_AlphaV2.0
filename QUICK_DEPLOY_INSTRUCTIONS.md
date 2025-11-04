# Quick Deploy Instructions - Choose Your Path

## OPTION A: Use GitHub (If your code is already on GitHub)

1. Go to https://vercel.com/dashboard
2. Click **"Add New Project"**
3. Click **"Import Git Repository"** (NOT "Upload")
4. Select your GitHub repository
5. Configure:
   - **Root Directory**: `Mental_Break_AlphaV2.0/webgl-build`
   - **Framework Preset**: Other
   - **Build Command**: (leave blank - empty)
   - **Output Directory**: `.` (just a dot)
6. Click **"Deploy"**

**That's it!** Your game will be live in 1-2 minutes.

---

## OPTION B: Install Node.js + Vercel CLI (If you want to deploy from your computer)

### Step 1: Install Node.js
1. Go to: **https://nodejs.org/**
2. Download the LTS version (big green button)
3. Run the installer, click Next through everything
4. **Close and reopen Command Prompt after installing**

### Step 2: Open Command Prompt
- Press `Windows Key + R`
- Type: `cmd`
- Press Enter

### Step 3: Install Vercel CLI
Copy and paste this into Command Prompt:
```
npm install -g vercel
```

### Step 4: Login
Copy and paste this:
```
vercel login
```
(Your browser will open - log in there)

### Step 5: Deploy
Copy and paste these commands one at a time:
```
cd Mental_Break_AlphaV2.0\webgl-build
vercel --prod
```

Answer the questions:
- "Set up and deploy?" → Type `Y`
- "Which scope?" → Select your account
- "Link to existing project?" → Type `N`
- "Project name?" → Type `mental-break`
- "Directory?" → Press Enter (don't type anything)
- "Override settings?" → Type `N`

**Done!** You'll get a URL like `https://mental-break-xxx.vercel.app`

---

## Which Option Should You Use?

**Use Option A (GitHub)** if:
- Your code is already on GitHub
- You want the fastest deployment
- You don't want to install anything

**Use Option B (CLI)** if:
- You want to deploy without GitHub
- You want to update deployments easily
- You're comfortable with Command Prompt

---

## After Deployment: Add Your Domain

1. Go to https://vercel.com/dashboard
2. Click your project
3. Go to **Settings** → **Domains**
4. Click **Add Domain**
5. Type: `mentalbreak.io`
6. Follow the DNS instructions shown

