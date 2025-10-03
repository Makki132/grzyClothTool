# Changelog

All notable changes to grzyClothTool will be documented in this file.

## [Unreleased]

### Added
- **Drag-and-Drop File Import**: Simplified workflow for adding files
  - Drag .ydd, .ytd, or .meta files directly into the main window
  - Only works when a project is open (prevents accidental imports)
  - Automatically detects gender from filenames:
    - Male: `_u`, `_m_`, `mp_m_` patterns
    - Female: `_r`, `_f_`, `mp_f_` patterns
  - Prompts for gender only when detection is uncertain
  - Groups files by type and imports them in batch
  - Supports importing multiple files at once

- **Auto-Recovery System**: Complete crash protection and file backup system
  - Automatically backs up YDD/YTD files to recovery cache when added to project
  - Detects unclosed sessions on startup and offers file recovery
  - Files are stored with internal names but recovered with original names
  - Recovery dialog shows timestamp and file count from crashed session
  - Creates new project named `Recovery_YYYYMMDD_HHmmss` with all recovered files
  - Smart cleanup system:
    - Deletes empty sessions automatically on close (saves space)
    - Removes empty unclosed sessions on startup
    - Keeps last 3 closed sessions with files
    - Limits total sessions to 20 maximum
  - Debug logging to `recovery_debug.txt` for troubleshooting
  - Recovery cache stored in `%LOCALAPPDATA%\grzyClothTool\recovery\`

- **Improved Drawable Grouping**: Enhanced UI organization for drawable files
  - Added `CategoryName` property to distinguish Components, Props, and Body Parts
  - Body parts (head, torso, arms, legs, etc.) now properly categorized
  - Two-level grouping: Category → Type Name for better organization
  - Gender count display for each category group

### Fixed
- **Auto Save functionality**: Previously non-functional, now properly backs up work
  - Auto-save setting now actually protects your work from crashes
  - Properly marks sessions as closed on normal exit
  - No more losing work when app closes unexpectedly

- **UI Category Text Color**: Fixed hover state issues in drawable list
  - Category and type name text now stays dark and readable on hover
  - Removed light gray color change that made text hard to read
  - Smooth hover transitions with consistent text color
  - Added explicit foreground color triggers for both hover states

- **Drawable Name Updates After Deletion**: Tree view now properly updates names
  - When deleting a drawable, all remaining drawables now show correct names (e.g., uppr_00_u, uppr_01_u)
  - Left side tree view now updates immediately to match the numbering shown on right side
  - Fixed by adding property change notifications to the Number property
  - FormattedDisplayName and DisplayNumber now update automatically when drawable numbers change

- **Settings Window Layout**: Fixed text clipping in LOD settings
  - Increased container height to prevent High/Medium/Low LOD labels from being cut off
  - Adjusted margins and padding for better spacing
  - All labels now fully visible without overlap

### Technical Changes
- Created `AutoRecoveryHelper` class for managing file backups and recovery
- Added `RecoveryFileInfo` and `RecoverySession` models
- Integrated recovery system into `AddonManager.AddDrawable()`
- Updated `MainWindow` startup to check for unclosed sessions
- Modified window closing logic to properly mark sessions as closed
- Added recovery UI dialog with Yes/No options

### Dependencies
- No new dependencies added
- All changes use existing .NET and WPF APIs

## Notes
- To enable auto-recovery, make sure "Auto Save on Close" is enabled in Settings
- Recovery files are automatically cleaned up after normal exits
- Old closed recovery sessions are removed automatically (keeps last 3)
