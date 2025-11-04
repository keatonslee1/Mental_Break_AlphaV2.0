# Exact Windows Instructions - No Coding Experience Needed

## Step 1: Install Node.js (Required First)

1. Go to: **https://nodejs.org/**
2. Click the big green button that says "Download Node.js (LTS)" 
3. Run the downloaded file (it will be something like `node-v20.x.x-x64.msi`)
4. Click "Next" through all the screens (don't change anything)
5. Click "Install" 
6. Click "Finish"

**IMPORTANT**: Close and reopen Command Prompt after installing Node.js!

## Step 2: Open Command Prompt

1. Press the **Windows key** + **R**
2. Type: `cmd`
3. Press **Enter**

A black window will open - this is Command Prompt.

## Step 3: Check Node.js is Installed

Copy and paste this into Command Prompt, then press Enter:
```
node --version
```

You should see something like `v20.11.0` - this means it worked!

## Step 4: Install Vercel CLI

Copy and paste this EXACT command into Command Prompt, then press Enter:
```
npm install -g vercel
```

Wait for it to finish (it will say "added X packages" when done).

## Step 5: Login to Vercel

Copy and paste this into Command Prompt, then press Enter:
```
vercel login
```

Your web browser will open - log in to Vercel there. Then come back to Command Prompt.

## Step 6: Go to Your Build Folder

Copy and paste this into Command Prompt, then press Enter:
```
cd Mental_Break_AlphaV2.0\webgl-build
```

## Step 7: Deploy Your Game

Copy and paste this into Command Prompt, then press Enter:
```
vercel --prod
```

**Answer the questions when asked:**
- "Set up and deploy?" → Type `Y` and press Enter
- "Which scope?" → Use arrow keys to select your account, press Enter
- "Link to existing project?" → Type `N` and press Enter  
- "Project name?" → Type `mental-break` and press Enter
- "Directory?" → Just press Enter (don't type anything)
- "Override settings?" → Type `N` and press Enter

## Step 8: Wait for Deployment

It will take 1-2 minutes. When done, you'll see a URL like:
`https://mental-break-xxx.vercel.app`

**That's your deployed game!** Copy that URL and open it in a browser to test.

## Step 9: Add Your Domain (mentalbreak.io)

1. Go to: **https://vercel.com/dashboard**
2. Click on your project: **mental-break**
3. Click **Settings** (at the top)
4. Click **Domains** (on the left)
5. Click **Add Domain**
6. Type: `mentalbreak.io`
7. Press Enter
8. Follow the DNS instructions shown (add CNAME or A record at your domain registrar)

## Troubleshooting

**"node is not recognized"**
- You need to install Node.js first (Step 1)
- Close and reopen Command Prompt after installing

**"npm is not recognized"**  
- Same as above - install Node.js

**"vercel is not recognized"**
- Make sure you completed Step 4 (npm install -g vercel)
- Close and reopen Command Prompt

**Permission errors**
- Right-click Command Prompt → "Run as Administrator"
- Then try the commands again

## What Each Command Does

- `node --version` - Checks if Node.js is installed
- `npm install -g vercel` - Installs Vercel CLI tool
- `vercel login` - Logs you into your Vercel account
- `cd Mental_Break_AlphaV2.0\webgl-build` - Goes to your build folder
- `vercel --prod` - Deploys your game to the internet

## Need Help?

If something doesn't work:
1. Copy the exact error message
2. Check which step you're on
3. Make sure you're typing commands in Command Prompt (the black window), not in a text editor

