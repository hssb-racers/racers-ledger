﻿# RACErs Ledger

RACErs Ledger -- a mod for Hardspace: Shipbreaker to save salvage data in real time to enable visualization and analysis of salvaging strategies! 

## Requirements

Hardspace Shipbreaker.
BepInEx: https://github.com/BepInEx/BepInEx (You want the x64 version.)
This mod (click on the Releases tab and download the dll file)

## Installation

Extract BepInEx to the root Hardspace Shipbreaker folder so `winhttp.dll` is in the same folder as `Shipbreaker.exe`.

Extract this mod so `racers-ledger.dll` is placed like this: `Hardspace Shipbreaker\BepInEx\plugins\racers-ledger.dll`.

## (temporary) Viewing the BepinEx console to see salvage logs while file/stream output isn't implemented

After following Installation instructions (above), run `Shipbreaker.exe` once. You can close it after you get to the main menu.

After this, navigate to `Hardspace Shipbreaker\BepinEx\config\` and edit `BepinEx.cfg`. Under `Logging.Console`, set `Enabled = true`.
Next time you run `Shipbreaker.exe` (directly or through steam), a console window should pop up too. If it didn't, I dunno, ping me on Discord and I can try to help debug or something.

## Un-installation

BepInEx uses `winhttp.dll` as an injector/loader. Renaming or deleting this file is enough to disable both my mod and the loader.

Or just remove `racers-ledger.dll` from the plugins folder.

## Support

This mod is provided on an AS-IS basis, with no implied warranty or guarantee that it will work at all. It might fuck up, it might accidentally delete your save, it might destroy spacetime, it might punch you in the face. 
It probably won't do any of those things, since I test every release myself before giving it to anyone else, but it might, and you should be prepared for that harsh reality. Back up your saves, etc.
If the mod doesn't work in some way, ping me on the HSSB Discord in `#modding-discussion` or something. Feature requests can go there too. I might not want to deal with the thing, or might not be able to deal with it, but I'll 
definitely at least read your plight ;)

## Credits

Thank you to Synthlight for making the [Furnace Performance Improvements mod](https://github.com/Synthlight/Hardspace-Shipbreaker-Furnace-Performance-Improvement-Mod) -- 
I've cribbed the structure and README of this mod heavily from them, so thank you Synthlight for helping get me started on this route!
Before this I was binary editing the DLLs with dnSpy and that was no fun whatsoever, and hard to source control :-)