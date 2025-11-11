# Pause Menu UI Setup Instructions

This document explains how to set up the pause menu UI in Unity for the MVPScene.

## Prerequisites

- MVPScene should be open in Unity
- The scene should have a Canvas (if not, Unity will create one automatically)

## Step 1: Create or Locate the Canvas

1. In the Hierarchy, check if there's already a Canvas GameObject
2. If not:
   - Right-click in Hierarchy → **UI** → **Canvas**
   - This creates a Canvas with EventSystem automatically

## Step 2: Create the Pause Hint Text

1. Right-click on the Canvas in Hierarchy → **UI** → **Text - TextMeshPro** (or **Text** if TextMeshPro isn't available)
2. Rename it to "PauseHintText"
3. In the Inspector:
   - **Text**: "Press ESC to pause"
   - **Alignment**: Top-Right (or use RectTransform to position it)
   - **Anchor Presets**: Top-Right (hold Alt+Shift when setting anchor)
   - **Position**: Set to top-right corner (e.g., X: -20, Y: -20 from top-right)
   - **Font Size**: Adjust as needed (e.g., 14-16)
   - **Color**: White or light gray

## Step 3: Create the Pause Menu Panel

1. Right-click on Canvas → **UI** → **Panel**
2. Rename it to "PauseMenuPanel"
3. In the Inspector:
   - **Image Component**:
     - **Color**: Set alpha to 200-230 for semi-transparent dark background (e.g., R:0, G:0, B:0, A:200)
   - **RectTransform**:
     - **Anchor Presets**: Stretch-Stretch (hold Alt+Shift)
     - This makes it fill the entire screen

## Step 4: Add Vertical Layout Group to Panel

1. Select the PauseMenuPanel
2. In Inspector, click **Add Component**
3. Search for "Vertical Layout Group" and add it
4. Configure:
   - **Padding**: Left: 50, Right: 50, Top: 50, Bottom: 50
   - **Spacing**: 20 (space between buttons)
   - **Child Alignment**: Middle Center
   - **Child Force Expand**: 
     - Width: Unchecked
     - Height: Unchecked

## Step 5: Create Menu Buttons

For each button below, create it as a child of PauseMenuPanel:

1. Right-click on PauseMenuPanel → **UI** → **Button - TextMeshPro** (or **Button**)
2. Configure each button's text and name

### Button 1: Resume
- **Name**: "ResumeButton"
- **Button Text**: "Resume"
- **OnClick**: Will be assigned in PauseMenuManager

### Button 2: Save Game
- **Name**: "SaveGameButton"
- **Button Text**: "Save Game"

### Button 3: Load Game
- **Name**: "LoadGameButton"
- **Button Text**: "Load Game"

### Button 4: Main Menu
- **Name**: "MainMenuButton"
- **Button Text**: "Main Menu"

### Button 5: Exit to Desktop
- **Name**: "ExitButton"
- **Button Text**: "Exit to Desktop"

### Optional: Settings Button
- **Name**: "SettingsButton"
- **Button Text**: "Settings"
- (This will be hidden by default in code)

## Step 6: Style the Buttons (Optional)

For each button:
1. Select the button
2. Adjust size in RectTransform (e.g., Width: 200, Height: 50)
3. Customize colors, fonts, etc. as desired

## Step 7: Add PauseMenuManager Component

1. Find or create a persistent GameObject in the scene (e.g., "GameManager" or create a new "PauseMenuManager" GameObject)
2. Add Component → Search for "PauseMenuManager"
3. In the Inspector, assign all references:

### UI References:
- **Pause Menu Panel**: Drag "PauseMenuPanel" from Hierarchy
- **Pause Hint Text**: Drag "PauseHintText" from Hierarchy (select the Text component)

### Menu Buttons:
- **Resume Button**: Drag "ResumeButton" from Hierarchy
- **Save Game Button**: Drag "SaveGameButton" from Hierarchy
- **Load Game Button**: Drag "LoadGameButton" from Hierarchy
- **Settings Button**: Drag "SettingsButton" from Hierarchy (if created)
- **Main Menu Button**: Drag "MainMenuButton" from Hierarchy
- **Exit Button**: Drag "ExitButton" from Hierarchy

### Settings:
- **Main Menu Scene Name**: Set to your main menu scene name (e.g., "MainMenu")

### References:
- **Save Load Manager**: Drag the GameObject with SaveLoadManager component (or leave None to auto-find)
- **Dialogue Runner**: Drag the DialogueSystem GameObject (or leave None to auto-find)

## Step 8: Verify Setup

1. Make sure PauseMenuPanel is **inactive** by default (uncheck the checkbox at top of Inspector)
2. Make sure PauseHintText is **active** (checked)
3. Run the game and press ESC to test

## Hierarchy Structure Example

```
Canvas
├── PauseHintText
└── PauseMenuPanel (inactive by default)
    ├── ResumeButton
    ├── SaveGameButton
    ├── LoadGameButton
    ├── SettingsButton (optional)
    ├── MainMenuButton
    └── ExitButton
```

## Troubleshooting

- **Menu doesn't appear on ESC**: Check that PauseMenuManager component is in the scene and all button references are assigned
- **Hint text not visible**: Check that PauseHintText is active and positioned correctly
- **Buttons don't work**: Verify all button references are assigned in PauseMenuManager
- **Game doesn't pause**: Check that Time.timeScale is being set correctly (should be 0 when paused)

## Notes

- The pause menu will automatically pause/unpause the game using Time.timeScale
- Save/Load will use the SaveLoadManager system
- The hint text will hide when the menu is open
- All UI elements should be children of the Canvas for proper rendering

