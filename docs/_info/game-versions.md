---
title: Supported Game Versions
description: You must have a supported game version to use mods. Please read the page for more details.
---
The following game versions are supported for mods:
- 3.0.0.2818 (July 2009) if installed from disc, with patch 5.1 manually installed (see below)
- 3.1.0.22 (March 2017) if installed from EA App, Steam, or GOG
- 3.1.0.29 (October 2024) if installed from EA App, Steam, or GOG

Any other game version that is not listed here is not supported for modding.

*Note: To use Spore's online features, you must have the latest game version (3.1.0.29), OR the [SporeFixOnline mod]({{ page.fixonline_mod_url }}).*

### Checking your game version
Open the folder where Spore Galactic Adventures is installed and open the `SporebinEP1` folder. Hover over `SporeApp` to see the game version.

### Do not install multiple copies of the game
Installing multiple copies of the game is known to cause a variety of problems. For example, do not install the game from EA App if you already have it installed from Steam or GOG. 

### Galactic Adventures requirement
Due to major changes and technical improvements introduced in Galactic Adventures, the Launcher Kit and most mods cannot work without Galactic Adventures.

### Pirated copies are not supported
Ethical issues aside, pirated versions are often modified in such a way that they are incompatible with regular mods. In short, if it's not an official version of the game, we have no way of knowing what it contains, and therefore cannot effectively support it. Sharing links, asking for pirated copies or CD keys, or otherwise encouraging piracy is strictly prohibited in all partnered communities.

---

## Updating the game
### EA App, Steam, and GOG Galaxy
You may safely update their game using these apps. No further steps are required. Mods will not be affected in any way.

### GOG offline installer
You would need to reinstall the latest version of the game, downloaded from GOG.com. However, there is little reason to update if you already have 3.1.0.22, as there are no differences between 3.1.0.22 and 3.1.0.29 aside from online functionality, which can be restored with the [SporeFixOnline mod]({{ page.fixonline_mod_url }}).

### Disc
Disc users have two options.
#### Updating to 3.0.0.2818 via patch
While this is not the latest version of the game, it has no apparent differences from newer versions, aside from online functionality, which can be restored with the [SporeFixOnline mod]({{ page.fixonline_mod_url }}).

Note that you must own both Spore and Galactic Adventures on disc, and have both installed. You cannot combine disc and download versions.

Download [Patch 5.1]({{ page.patch151_url }}) and install it. It will update your game to 3.0.0.2818.

Do *NOT* install patch 6. This patch was never officially released and has multiple game-breaking issues.

#### Updating to 3.1.0.29 via EA App
Alternatively, you may be able to obtain a free copy of the game on EA App. You must be the original purchaser of the disc (i.e. cannot be a used copy).

If you have used Spore's online features, download and launch EA App, sign in with the same account used for Spore, and see if Spore and Galactic Adventures are available to download. If not, choose Add a Game and enter the code from the back of each manual. If the codes do not work, you will need to contact EA Help to have them replaced.

*IMPORTANT*: Uninstall the disc versions before downloading the game from EA App.

---

## Downgrading the game
It is not necessary to downgrade your game. However, Steam users may *optionally* downgrade their game to use the 4GB patch.

### Downgrading to 3.1.0.22 on Steam
- Press Windows Key (Win) + R, and type in `steam://open/console/` to open Steam's console.
- From the Steam console, enter the command `download_depot 24720 24721 7407510787032991484` to prompt Steam to download the old version.
- When it's installed, it will not be in the default location but instead in `Steam/Steamapps/Content/`, go there and, assuming it exists, navigate to `app_24720/depot_24721/SporeBinEP1`.
- Copy the `SporeApp.exe` contained in that folder, and replace the existing one in the installed game's directory, usually `Steam/Steamapps/Common/Spore/SporebinEP1`, with the newly-downloaded version from the depot.

It is not possible or necessary to manually downgrade if the game was purchased elsewhere.