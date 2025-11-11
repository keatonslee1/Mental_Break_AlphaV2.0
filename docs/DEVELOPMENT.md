# Development Guide

Unity development setup and feature documentation for Mental Break.

## Project Structure

```
Mental_Break_AlphaV2.0/
├── Assets/
│   ├── Scripts/          # C# scripts
│   ├── Scenes/           # Unity scenes
│   ├── Audio/            # BGM and SFX
│   ├── Dialogue/         # Yarn Spinner dialogue files
│   ├── Graphics/         # Sprites and textures
│   └── ...
├── webgl-build/          # WebGL build output (deployed to Vercel)
└── ...
```

## Unity Setup

- **Unity Version**: 6.2 (Web Unity 6.2)
- **Platform**: WebGL
- **Dialogue System**: Yarn Spinner
- **UI Framework**: Unity UI (uGUI) with TextMeshPro

## Pause Menu Setup

See [Pause Menu UI Setup](PAUSE_MENU_UI_SETUP.md) for detailed instructions on setting up the pause menu in Unity.

### Quick Setup Steps

1. **Create Canvas** (if not exists):
   - Right-click in Hierarchy → **UI** → **Canvas**

2. **Create Pause Hint Text**:
   - Right-click Canvas → **UI** → **Text - TextMeshPro**
   - Rename to "PauseHintText"
   - Set text: "Press ESC to pause"
   - Position: Top-Right corner

3. **Create Pause Menu Panel**:
   - Right-click Canvas → **UI** → **Panel**
   - Rename to "PauseMenuPanel"
   - Set semi-transparent dark background
   - Add Vertical Layout Group component

4. **Create Menu Buttons** (as children of PauseMenuPanel):
   - ResumeButton
   - SaveGameButton
   - LoadGameButton
   - MainMenuButton
   - ExitButton
   - SettingsButton (optional)

5. **Add PauseMenuManager Component**:
   - Find or create a persistent GameObject
   - Add Component → PauseMenuManager
   - Assign all UI references in Inspector

6. **Verify Setup**:
   - PauseMenuPanel should be inactive by default
   - PauseHintText should be active
   - Test by pressing ESC in Play mode

## Store System

The game includes a store system that must be set up before building:

1. Open the main game scene (e.g., `MVPScene.unity`)
2. Go to: **Tools > Setup Store UI**
3. Verify that `StorePanel` appears in the Hierarchy under `DontDestroyOnLoad/Dialogue System/Canvas`
4. **IMPORTANT**: Save the scene after setup to ensure StorePanel persists in the build
5. If StorePanel is missing, the store command will fail in WebGL builds

## Dialogue System

The game uses Yarn Spinner for dialogue:

- **Dialogue Files**: Located in `Assets/Dialogue/`
- **Format**: `.yarn` files
- **Dialogue Wheel**: Custom implementation in `Assets/Dialogue Wheel for Yarn Spinner/`
- **Speech Bubbles**: Custom implementation in `Assets/Speech Bubbles for Yarn Spinner/`

## Build Process

See [Build Instructions](BUILD.md) for complete build setup.

**Key Settings:**
- Compression Format: **Gzip** (recommended for iOS)
- Power Preference: **Low Power** or **Default** (for iOS compatibility)
- Code Optimization: **Size**
- Strip Engine Code: **Enabled**

## Testing

### Local Testing
- Build WebGL project
- Test locally using HTTP server (see [Build Instructions](BUILD.md))
- Test pause menu functionality
- Test store system
- Test dialogue system

### Browser Testing
- Test on Chrome, Firefox, Safari, Edge
- Test on mobile devices (iOS and Android)
- Check browser console for errors
- Verify UI elements appear correctly

## Common Development Issues

### Store Command Fails
- Ensure StorePanel exists in the scene
- Run `Tools > Setup Store UI` before building
- Save the scene after setup

### Pause Menu Doesn't Work
- Check that PauseMenuManager component is in the scene
- Verify all button references are assigned
- Check that ESC key is properly configured in Input System

### Dialogue Not Showing
- Verify Yarn Spinner dialogue files are in `Assets/Dialogue/`
- Check Dialogue Runner component is configured
- Verify dialogue system UI is set up correctly

## Additional Resources

- [Pause Menu UI Setup](PAUSE_MENU_UI_SETUP.md) - Detailed pause menu setup instructions
- [iOS WebGL Diagnosis](IOS_WEBGL_DIAGNOSIS.md) - iOS-specific WebGL issues and solutions
- [Build Instructions](BUILD.md) - Complete build guide
- [Deployment Guide](DEPLOYMENT.md) - Deployment instructions
- [Troubleshooting Guide](TROUBLESHOOTING.md) - Common issues and solutions

