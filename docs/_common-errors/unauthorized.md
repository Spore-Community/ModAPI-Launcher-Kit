---
title: Unauthorized access exception
---
This error is known to occur when Spore is installed from EA App, and will show
```
System.UnauthorizedAccessException
Access to the path '<path>' is denied.
```

- Open the folder where the Launcher Kit is installed, the path should be shown in the error message
  - By default this is `C:\ProgramData\Spore ModAPI Launcher Kit`
  - Press Win+R and type this path in (not including `mLibs` or anything following it, nor `Spore ModAPI Launcher.exe`) to open it
- You should see an `mLibs` folder
- Right click the `mLibs` folder > Properties > Security tab > Edit
- Select "Users" in the list at the top
- In the list at the bottom, check the "Allow" box next to "Full control"
- Click "OK" to save and close both windows