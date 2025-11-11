# Deploy Unity WebGL Build to Vercel
# This script assumes webgl-build/ directory exists with Unity WebGL build

param(
    [switch]$Prod,
    [string]$TeamId = "team_w3aTDuy3oozvmAKuhIdJCGq3"
)

Write-Host "Deploying Mental Break to Vercel..." -ForegroundColor Cyan

# Check if webgl-build exists (try both root and Mental_Break_AlphaV2.0 subdirectory)
$buildPath = $null
if (Test-Path "webgl-build") {
    $buildPath = "webgl-build"
} elseif (Test-Path "Mental_Break_AlphaV2.0/webgl-build") {
    $buildPath = "Mental_Break_AlphaV2.0/webgl-build"
} else {
    Write-Host "ERROR: webgl-build/ directory not found!" -ForegroundColor Red
    Write-Host "Please build Unity WebGL project first. See docs/BUILD.md" -ForegroundColor Yellow
    exit 1
}

# Check if index.html exists
if (-not (Test-Path "$buildPath/index.html")) {
    Write-Host "ERROR: $buildPath/index.html not found!" -ForegroundColor Red
    Write-Host "The Unity build may be incomplete. Please rebuild in Unity Editor." -ForegroundColor Yellow
    exit 1
}

Write-Host "Found build at: $buildPath" -ForegroundColor Green

Write-Host "Build directory found. Deploying..." -ForegroundColor Green

# Check if Vercel CLI is installed
$vercelCmd = Get-Command vercel -ErrorAction SilentlyContinue
if (-not $vercelCmd) {
    Write-Host "Vercel CLI not found. Checking for Node.js..." -ForegroundColor Yellow
    $nodeCmd = Get-Command node -ErrorAction SilentlyContinue
    if (-not $nodeCmd) {
        Write-Host "ERROR: Node.js and Vercel CLI are not installed!" -ForegroundColor Red
        Write-Host "Please install Node.js from https://nodejs.org/" -ForegroundColor Yellow
        Write-Host "Then install Vercel CLI: npm install -g vercel" -ForegroundColor Yellow
        Write-Host "`nAlternatively, deploy via Vercel Dashboard:" -ForegroundColor Cyan
        Write-Host "1. Go to https://vercel.com/dashboard" -ForegroundColor White
        Write-Host "2. Click 'Add New Project'" -ForegroundColor White
        Write-Host "3. Upload or connect the '$buildPath' directory" -ForegroundColor White
        Write-Host "4. Configure: Framework='Other', Output Directory='.'" -ForegroundColor White
        exit 1
    } else {
        Write-Host "Node.js found. Installing Vercel CLI..." -ForegroundColor Cyan
        npm install -g vercel
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Failed to install Vercel CLI. Please install manually: npm install -g vercel" -ForegroundColor Red
            exit 1
        }
    }
}

# Navigate to build directory and deploy
Push-Location $buildPath
try {
    if ($Prod) {
        Write-Host "Deploying to production..." -ForegroundColor Cyan
        vercel --prod
    } else {
        Write-Host "Deploying to preview..." -ForegroundColor Cyan
        vercel
    }
} finally {
    Pop-Location
}

Write-Host "`nDeployment complete!" -ForegroundColor Green
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Test the deployment URL" -ForegroundColor White
Write-Host "2. Configure domain in Vercel dashboard: Settings > Domains > Add mentalbreak.io" -ForegroundColor White

