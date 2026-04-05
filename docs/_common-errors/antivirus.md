---
title: Defender/SmartScreen/Virus Detections
description: When a new Launcher Kit version is released, for a short period of time, the new version may be blocked by Windows Defender, SmartScreen, or your web browser. This is a false positive due to how Microsoft's filters work. Please read the full page for more details on how we keep the Launcher Kit safe.
---
When a new Launcher Kit version is released, for a short period of time, the new version may be blocked by Windows Defender, SmartScreen, or your web browser. This is a false positive due to how Microsoft's filters work.

## SmartScreen
Microsoft SmartScreen in Windows Defender and Microsoft Edge automatically blocks recently-released versions of software that does not come from large companies, which includes the Launcher Kit. You may encounter this if you are one of the first people to download a new version of the Launcher Kit.

This does not indicate there is any threat to your computer. It simply means that the software is very recent.

### In your browser
When downloading the Launcher Kit, you may be prompted by your browser to delete the download. Look for the option to "Keep" the download.

### When running the Launcher Kit setup
After downloading the Launcher Kit, you may receive a "Windows protected your PC" prompt when opening it. Click "More info" and then "Run anyway".

## Windows Defender Antivirus
Microsoft uses AI, which needs a period of time to learn about new software, including new versions of the Launcher Kit. Until the AI has learned, it may flag the Launcher Kit apps as a threat.

Open Windows Security, go to "Virus & threat protection", then "Protection history". Look for the threat removed, and check the following:
- Detected: ends with `ml` (this indicates that the Machine Learning AI failed to understand the file) - for example "Detected: Trojan:Script/Wacatac.C!**ml**"
- Affected items: `Spore ModAPI Easy Installer`, `Spore ModAPI Easy Uninstaller`, and/or `Spore ModAPI Launcher`

If this is correct, you can safely "restore" these items.

If "Detected" does *not* end in `ml`, please [ask for help](/support).

## Other antivirus
We do not recommend using antivirus other than the built-in Windows Defender. There is no need to, and you may encounter issues if you choose to use other antivirus options.

---

## Launcher Kit and safety
### How the Launcher Kit is kept safe
We take many precautions to ensure the Launcher Kit is safe and free from viruses.

The full source code of the Launcher Kit is available to the public [here]({{ site.github.repository_url }}), so anyone can view the code to ensure it is safe.

All downloads are built on secure servers that are owned and operated by GitHub, a subsidiary of Microsoft, and cannot practically be tampered with, ensuring that the downloaded apps are identical to this source code. You can view the script for the build process [here]({{ site.github.repository_url }}/blob/main/.github/build.yml).

All changes to code or the build script are monitored by multiple people, ensuring malicious changes cannot be made.

The Launcher Kit itself does not run in the background or perform any functionality without direct user action. Mods that execute code are only loaded when explicitly using the Launcher app, and the game's own executable is not modified, and remains digitally signed by Electronic Arts. The Easy Installer and Easy Uninstaller prompt the user for permission to add/remove files, and cannot modify any files without this explicit permission.

### Download safety
The Launcher Kit is officially available from:
- [{{ site.github.url }}]({{ site.github.url }})
- [{{ site.github.repository_url }}]({{ site.github.repository_url }})

Launcher Kit updates may be downloaded from the update service at `https://update.launcherkit.sporecommunity.com` (note that this URL does not open in your browser) or from GitHub as listed above.

All of these use HTTPS to ensure that your connection to the website has not been tampered with.

**Do not download the Launcher Kit from any other website.** We do not maintain downloads on other websites, and cannot ensure that they have not been tampered with. For your safety, only download from the official locations.

### Safety when using mods
Please note that while the Launcher Kit itself is safe, we cannot guarantee that individual mods have been checked for quality or safety. Only install mods from trusted sources. Use caution when installing unfamiliar mods.

Do not run the Launcher as administrator, as this is a security risk because it removes certain safeguards, allowing mods to have full unrestricted access to your computer.

If in doubt, please [ask for help](/support) before downloading a mod that you are unsure about.