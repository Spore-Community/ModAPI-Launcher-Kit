---
title: Updating Launcher Kit
description: The Launcher Kit will automatically check for updates hourly when you open the Launcher, Easy Installer, or Easy Uninstaller. If an update is available, you will be prompted to install it.
---
The Launcher Kit will automatically check for updates every hour when you open the Launcher, Easy Installer, or Easy Uninstaller. If an update is available, you will be prompted to install it.

The latest Launcher Kit version is **{{ site.github.latest_release.name }}**. You can check your current version by opening the Easy Uninstaller and looking in the bottom left corner.

It should not normally be necessary to manually install updates, unless you encounter an error while updating. If you see "Update Check Failed" please see [here](update-check-failed).

---

## Manually updating
If you need to manually update, for example on a computer with no internet connection, you may download the latest Launcher Kit from the homepage. When running the setup app, it will ask you if the Launcher Kit is already installed, choose this option to perform a manual update.

---

## About Updates
The Launcher Kit and the ModAPI DLLs are updated separately. The Launcher Kit is used to install, manage, and load mods, while the ModAPI DLLs are loaded into the game itself to enable additional capabilities for mods. Therefore, you may be prompted to update both.

The Launcher Kit connects to the internet and attempts to update both. Updates for the Launcher Kit itself may be downloaded from the Spore Community Hub servers, or from GitHub. Updates for the ModAPI DLLs are downloaded from GitHub.

No identifying information is sent to the servers to perform the update check. We do not track you or collect any telemetry data.

---

## Advanced topics
The content below is intended for **advanced users**. These options are unsupported and you may not receive help if you use them. Use at your own risk.

### Forcing update check
The Launcher Kit stores the last update time in a file called `lastUpdateCheckTime.info` in `%appdata%\Spore ModAPI Launcher`. This is used to limit update checks to once per hour. If you need to update immediately, you may delete this file.

When attempting to install a mod that requires a newer Launcher Kit or ModAPI DLLs version, this timer is reset automatically.

### Disabling update check
If you need to disable the update check, for example on a computer that is always offline and thus unable to connect to the internet, place a file called `noUpdateCheck.info` into `%appdata%\Spore ModAPI Launcher`. **Make sure you are checking the website and manually updating if needed.** You will not receive help if you are not using the latest Launcher Kit version.

When updates are disabled, you will be prompted to re-enable them periodically, as well as when installing a mod that requires a newer Launcher Kit or ModAPI DLLs version.