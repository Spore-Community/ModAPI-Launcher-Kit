---
title: Entry Point Not Found
description: This error means that a mod requires an update to work with the latest version of the ModAPI. Check that your mods are up-to-date.
---
This error means that a mod requires an update to work with the latest version of the ModAPI.

## For users
Check that your mods are up-to-date.

The error message will usually indicate which mod needs to be updated. If you are unsure which mod it is, you can [ask for help](/support).

You may need to contact the mod developer and ask them to update their mod.

## For mod developers
If you are getting this error for your own mod, you will need to update it.

This error occurs when the name of a function has changed in the SDK. This typically happens with functions that have placeholder names of the format `funcXXX`.

While you *can* use these functions (for calling or detouring), you should be aware that the name is likely to change once the proper name or purpose is found. When the function name is changed in the ModAPI, **your mod will break** and show this error on startup.

To fix your mod, first ensure that you have the latest version of the ModAPI SDK by pulling the latest changes using Git. Then, you will need to replace all instances of the placeholder name with the new name.

For future reference, if you have found the purpose of a function with a placeholder name like `funcXXX` and wish to use it in a mod, it is best to document it first. You can request that a better name be added to the SDK in the [ModAPI issue tracker](https://github.com/emd4600/Spore-ModAPI/issues), or if you know how to do so, you can add a new name directly to the code and submit a pull request. This means that a more-permanent name can be added to the SDK, which you can then safely use in your own mod.