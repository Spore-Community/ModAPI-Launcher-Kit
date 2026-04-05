---
title: Update Check Failed
description: Download the latest version of the Launcher Kit from the website. Follow the instructions to update your existing Launcher Kit.
---
This error occurs on Launcher Kit versions prior to 1.5.0.0. There are two ways to fix this error, you can perform either one.

## Fix by downloading latest Launcher Kit version directly
Download the latest version of the Launcher Kit from the homepage. Follow the instructions to update your existing Launcher Kit.

## Fix using `overrideUpdatePath.info` file
1. Make sure you have installed the Launcher Kit and have run the ModAPI Launcher at least once (so that you see the "Update Check Failed" message).
2. Open the ModAPI Easy Uninstaller and check the Launcher Kit version in the bottom left corner. It must be 1.4.1.0 or lower. If it is 1.5.0.0 or higher, please ask for help before continuing. You can ignore the DLLs build version.
3. Open the folder `%appdata%\Spore ModAPI Launcher`. You can type/copy this into Windows search, the File Explorer address bar, or pressing Win+R and typing/copying it there. You should see files like `current.info` and `path.info` here.
4. Download [this file](https://update.launcherkit.sporecommunity.com/overrideUpdatePath.info) and place it in the folder from the previous step. The file name must remain exactly as-is (`overrideUpdatePath.info`).
5. Open the ModAPI Launcher. You should no longer get the error, and you will be prompted to update to the latest version.

The `overrideUpdatePath.info` file will be removed automatically upon updating to Launcher Kit 1.5.0.0 or newer. This is normal - the fix has been built directly into the newer version, so a separate file is no longer needed. Do not put the file back in.