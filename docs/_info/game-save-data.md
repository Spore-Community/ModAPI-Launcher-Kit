---
title: Backup Spore Save Data
description: Spore saves all save data, including your galaxy (with all saved games), personal creations, downloaded creations, settings, saved login info, and cached data in one folder.
---
Spore saves all save data, including your galaxy (with all saved games), personal creations, downloaded creations, settings, saved login info, and cached data in one folder.

---

## Locating Spore's save data
Press Win+R and type in `%appdata%\Spore`. This folder contains all Spore save data.

---

## Backing up data
Make a copy of this entire `Spore` folder in a safe location.

If the backup is too large, you may optionally exclude any `GraphicsCache` files, as these are not critical data and can be regenerated when needed.

Do not separate any other files. In particular, the following files are necessary to ensure your creations and galaxy are kept intact, and *cannot* be mixed and matched with other copies of save data:
- `Games` folder
- `Planets.package`
- `EditorSaves.package`
- `Pollination.package` (there may be multiple)

---

## Restoring data
To restore a backup, you must first remove or rename an existing `%appdata%\Spore` folder, if one exists. Merging or combining data is not supported and may result in corruption.

Place the `Spore` folder back in its original location. When the game is launched, all data should be restored.

---

## The "My Spore Creations" folder
While the *primary* copy of creations is stored in the above location, the game also exports PNG copies of your own creations to the `My Spore Creations` folder in `Documents`. This folder may be backed up to OneDrive on Windows.

You can save a backup of this folder to have an extra copy of your creations.

It is *not* recommended to restore this folder to its original location. Instead, if needed, you should drag individual PNGs from the backup of this folder directly into the game while it is running. This is a more reliable way to import creations and will ensure they are sorted properly in the Sporepedia.

---

## Tips for transferring data to a new computer
- Make backups of the `%appdata%\Spore` and `My Spore Creations` folders (as described above) onto an external drive.
- Keep copies of all `.sporemod` and `.package` mods.
- On the new computer, install Spore and Galactic Adventures.
- Install the Launcher Kit, and use the Easy Installer to install all mods you had.
- Restore the backup by placing the copied folder back at `%appdata%\Spore`. If you have not yet launched the game, there will be no Spore folder here yet.
- Launch the game, and everything should be exactly as you left it.