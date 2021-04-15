# RACErs Ledger

RACErs Ledger -- a mod for Hardspace: Shipbreaker to save salvage data in real time to enable visualization and analysis of salvaging strategies!

Right now it can:
 - Save shift summaries (time started, ended, duration, value you salvaged, value you destroyed, probably more in the future!) after every shift!
 - Save salvage ledgers (lists of everything you salvaged and destroyed) after every shift!

 In the future it will be able to:
  - Do magic
  - Read minds
  - Shit gold and spawn butterflies
  - Probably other things if you suggest them!

## Requirements

Hardspace Shipbreaker.

BepInEx: https://github.com/BepInEx/BepInEx/releases (You want the x64 .zip.)

This mod (click [here or on the Releases tab](https://git.sariya.dev/sariya/racers-ledger/releases) and download the `.dll` file of the latest release)

## Installation

# Windows

Extract BepInEx to the root Hardspace Shipbreaker folder so `winhttp.dll` is in the same folder as `Shipbreaker.exe`.

Extract this mod so `RACErsLedger.dll` is placed like this: `Hardspace Shipbreaker\BepInEx\plugins\RACErsLedger.dll`.

# Linux

Follow the Windows instructions. Afterwards you need to enable the DLL override.

For this either follow [the official instructions of BepInEx](https://bepinex.github.io/bepinex_docs/master/articles/advanced/steam_interop.html?tabs=tabid-1#protonwine) or do it manually.

In order to manually enable the override open `steamapps/compatdata/1161580/pfx/user.reg` in a text editor and go to section `[Software\\Wine\\DllOverrides]`.
Add a line to it:

```ini
[Software\\Wine\\DllOverrides]
…
"winhttp"="native,builtin"
```

Afterwards you can start the game as regular.

## Viewing the BepinEx console to see logging info in realtime, if you're into that

After following Installation instructions (above), run `Shipbreaker.exe` once. You can close it after you get to the main menu.

After this, navigate to `Hardspace Shipbreaker\BepinEx\config\` and edit `BepinEx.cfg`. Under `Logging.Console`, set `Enabled = true`.

Next time you run `Shipbreaker.exe` (directly or through steam), a console window should pop up too. If it didn't, I dunno, ping me on Discord and I can try to help debug or something.

## Mod configuration
The mod's config file will be in `Hardspace Shipbreaker\BepInEx\config\dev.sariya.racersledger.cfg` after you've run `Shipbreaker.exe` with `RACErsLedger.dll` installed properly at least once. 

Config options:

|      key     |                                              description                                             | default                             |
|:------------:|:----------------------------------------------------------------------------------------------------:|-------------------------------------|
| `DataFolder` | Where to store the CSVs of your salvage summaries ~or whatever other bullshit i decide to put there~ | `HardspaceShipbreaker\RACErsLedger` |

## Updating

To update to a new RACErs Ledger version, navigate to [the releases tab](https://git.sariya.dev/sariya/racers-ledger/releases), download the `.dll` from the latest release, and overwrite `RACErsLedger.dll` in your install location.
If Hardspace: Shipbreaker is already running, you will need to close it to be able to overwrite the file.

## Un-installation

BepInEx uses `winhttp.dll` as an injector/loader. Renaming or deleting this file is enough to disable both this mod and the loader.

Or just remove `RACErsLedger.dll` from the plugins folder.

## Support

This mod is provided on an AS-IS basis, with no implied warranty or guarantee that it will work at all. It might fuck up, it might accidentally delete your save, it might destroy spacetime, it might punch you in the face. 

It probably won't do any of those things, since I test every release myself before giving it to anyone else, but it might, and you should be prepared for that harsh reality. Back up your saves, etc.

If the mod doesn't work in some way, ping me on the HSSB Discord in `#modding-discussion` or something. Feature requests can go there too. I might not want to deal with the thing, or might not be able to deal with it, but I'll 
definitely at least read your plight ;)

## Credits

Thank you to Synthlight for making the [Furnace Performance Improvements mod](https://github.com/Synthlight/Hardspace-Shipbreaker-Furnace-Performance-Improvement-Mod) -- 
I've cribbed the structure and README of this mod heavily from them, so thank you Synthlight for helping get me started on this route!
Before this I was binary editing the DLLs with dnSpy and that was no fun whatsoever, and hard to source control :-)
