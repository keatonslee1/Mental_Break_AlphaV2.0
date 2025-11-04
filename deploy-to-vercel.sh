#!/bin/bash
# Deploy Unity WebGL Build to Vercel
# This script assumes webgl-build/ directory exists with Unity WebGL build

set -e

echo "Deploying Mental Break to Vercel..."

# Check if webgl-build exists
if [ ! -d "webgl-build" ]; then
    echo "ERROR: webgl-build/ directory not found!"
    echo "Please build Unity WebGL project first. See BUILD_INSTRUCTIONS.md"
    exit 1
fi

# Check if index.html exists
if [ ! -f "webgl-build/index.html" ]; then
    echo "ERROR: webgl-build/index.html not found!"
    echo "The Unity build may be incomplete. Please rebuild in Unity Editor."
    exit 1
fi

echo "Build directory found. Deploying..."

# Check if --prod flag is passed
if [ "$1" == "--prod" ]; then
    echo "Deploying to production..."
    vercel --prod
else
    echo "Deploying to preview..."
    vercel
fi

echo ""
echo "Deployment complete!"
echo "Next steps:"
echo "1. Test the deployment URL"
echo "2. Configure domain in Vercel dashboard: Settings > Domains > Add mentalbreak.io"

