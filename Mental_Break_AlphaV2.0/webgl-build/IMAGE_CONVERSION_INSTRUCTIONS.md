# Image Conversion Instructions for Web Icons

## Overview

This document explains how to convert the provided images into the required formats for web icons and thumbnails.

## Required Files

### 1. Favicon Files (from closeup image)

The **closeup image** (pixel art portrait with teal skin and purple hair) needs to be converted to:

- **favicon.ico** - Multi-size ICO file (16x16, 32x32, 48x48)
  - Location: `TemplateData/favicon.ico`
  - Format: ICO (multi-size)

- **favicon-16x16.png** - 16x16 pixels
  - Location: `TemplateData/favicon-16x16.png`
  - Format: PNG

- **favicon-32x32.png** - 32x32 pixels
  - Location: `TemplateData/favicon-32x32.png`
  - Format: PNG

- **favicon-48x48.png** - 48x48 pixels
  - Location: `TemplateData/favicon-48x48.png`
  - Format: PNG

- **apple-touch-icon.png** - 180x180 pixels
  - Location: `TemplateData/apple-touch-icon.png`
  - Format: PNG
  - Note: Apple devices use this for home screen icons

### 2. Thumbnail Image (from wordmark image)

The **wordmark image** (full scene with "MENTAL BREAK" text) needs to be converted to:

- **og-image.png** - 1200x630 pixels (recommended Open Graph size)
  - Location: `TemplateData/og-image.png`
  - Format: PNG
  - Used for: Social media sharing, link previews, Open Graph, Twitter Cards

## Conversion Tools

### Online Tools:
1. **Favicon Generator**: https://www.favicon-generator.org/
   - Upload your closeup image
   - Generates all favicon sizes automatically
   - Downloads as a zip with all files

2. **ICO Converter**: https://convertio.co/png-ico/
   - Converts PNG to ICO format
   - Can create multi-size ICO files

3. **Image Resizer**: https://imageresizer.com/
   - Resize wordmark image to 1200x630px for og-image.png

### Desktop Tools:
1. **GIMP** (Free):
   - Open image → Image → Scale Image → Set dimensions
   - File → Export As → Choose format (PNG/ICO)

2. **Photoshop**:
   - Image → Image Size → Set dimensions
   - File → Save As → Choose format

3. **ImageMagick** (Command line):
   ```bash
   # Resize to 32x32
   convert input.png -resize 32x32 favicon-32x32.png
   
   # Create ICO with multiple sizes
   convert input.png -define icon:auto-resize=16,32,48 favicon.ico
   ```

## File Locations Summary

Place all converted files in: `Mental_Break_AlphaV2.0/webgl-build/TemplateData/`

- `favicon.ico` (multi-size ICO)
- `favicon-16x16.png`
- `favicon-32x32.png`
- `favicon-48x48.png`
- `apple-touch-icon.png` (180x180)
- `og-image.png` (1200x630)

## Verification

After conversion:
1. Check that all files are in the correct location
2. Verify image dimensions are correct
3. Test favicon displays in browser
4. Test Open Graph preview using: https://www.opengraph.xyz/url/https://mentalbreak.io

## Notes

- The closeup image should be cropped/resized to square format for icons
- The wordmark image should maintain aspect ratio but fit within 1200x630px
- Use high-quality source images for best results
- All PNG files should be optimized for web (compressed but not lossy)

