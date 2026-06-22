# Space Haven Launcher

This is a launcher program made for Space Haven. It starts the original game as well as a modified one.

## Installation

**Everyone**:
- make sure Space Haven Launcher has write permissions on Space Haven folder

**Windows**:
- Place the app anywhere, run the app, then on the app's System Core tab you can create a desktop icon

**Linux**:
- check whether the application has execute permissions:
```chmod 755 ./SpaceHavenLauncher```

**macOS**:
- place the application here:
```/Applications/SpaceHavenLauncher.app```
- remove the "downloaded" app flag:
```xattr -dr com.apple.quarantine SpacehavenLauncher.app```
- due to temporary lack of notarization, codesign the application too:
```codesign --force --deep --sign - *```

## Mod Installation

- First of all, you can always reinstall the game if everything fails
- Space Haven Launcher should autodetect changes to Space Haven
- Some mods are indeed incompatible: work is on progress to detect/map conflicts
- Save your games frequently: some mods are unstable (especially the JAVA ones)
- Reinstalling the game via Steam should NOT delete your savegames
- Steam sometimes does NOT update subscribed workshop mods to their latest version
- You may also get mods from the NEXUS MODS webpage, just put them under the game's "mods" folder
- e.g. you may download the the 'My Storage' mod and unzip it to:

Windows: `C:\Program Files (x86)\Steam\steamapps\common\SpaceHaven\mods\MyStorage`

Linux: `~/.steam/steam/steamapps/common/SpaceHaven/spacehaven/mods/MyStorage`

macOS: `~/Library/Application Support/Steam/steamapps/common/SpaceHaven/spacehaven.app/Contents/Resources/mods/MyStorage`

## TODO

- Export libraries with XML annotation/comments

## Main Features

- Launcher for XML and JAVA mods
- Exporting of game data
- Display of fan-art
- Advanced XML/JAVA mod build system
- Detailed error detection and reporting
- Improved quality of life for players and modders!

## Performance and UX

- This program makes advanced use of GPU and multi-threading in order to render the best user experience
- Comprehensive LOG: lines can be right-clicked to jump to related file/directory/link
- Space Haven Launcher background images can be made darker or completely disabled for best performance
- Caching and hashing of intermediate files is extensively used
- The responsive user-friendly interface resembles the game art
- Display of fan art (please fill a defect formular if you want your art to be removed)
- Valuable feedback for mod developers through logs and intermediate files
- Customizable persistent application settings

## Directories and File Paths

- Automatic detection of relevant directories and file paths
- Manual user input of desired paths (also a fallback if autodetection of paths fails)
- The application remembers path settings
- Automatic backup of game files
- All cached files are stored under the application work directory

## Initialization

- Auto backups the original game file for build/restore
- Prepares template files for optimized build and for exporting assets
- Re-initialization is possible by clicking buttons on 'Navigation Console'

## Mod Loading

- Supported mod types: XML, JAVA, or XML+JAVA
- Easy enabling and disabling of mods without writing to mod directory
- Detailed logging of mod loading
- Remembers mod loading sequence
- Detects malformed info.xml
- Detects missing mandatory fields in info.xml
- Allows modder to define a backgroung.jpg/png file as backgroung for the mod in the mod launcher
- Allows modder to define a backgroung.jpg/png file as backgroung for the mod in the mod launcher
- Detects MOD ID Conflicts
- Allows assigning a new CUSTOM ID to replace the MOD ID
- Allows mods without MOD ID, by assigning AUTO ID
- To use AUTO ID you must declare your internal IDs like this: mid="{id}0001"
- The {id} variable is reserved, don't use it!

## XML Mods

- Allow merging XML or patching XML without writing to mod directory
- Persistence of XML mod variables without writing to mod directory
- Persistence of XML mod variables for previous versions of the mod
- Allow the player to choose variable values from: ORIGINAL, SUGGESTED, or PREVIOUS MOD VERSION
- Supported target XML files: haven, texts, audio, textures, animations, spacehavensettings.xml

## XML Mod Variables

- Variables can be searched by their description
- Variables can be individually set or set ALL at once with ORIGINAL, SUGGESTED, or PREVIOUS MOD VERSION VALUES
- Detection of duplicate declared variables in info.xml
- ORIGINAL variable values should make the mod resemble the original game as much as possible
- SUGGESTED variable values should make the game resemble the modders intention
- ORIGINAL variable value is read from the first existing attribute: 'original', 'default', or 'value'
- SUGGESTED variable value is read from the first existing attribute: 'suggested', or 'value'
- CURRENT variable value defaults to SUGGESTED if never set before

## XML Mod Build

- Rebuild: happens when previous build has failed, or by re-initializing
- Validation of XML merge/patch syntax errors
- Validation of missing mod variables
- Validation of maformed XPATH
- XML Build Process: separate log for each mod
- XML Build Process: Stores intermediate results for later analysis
- Audio Merge: validation of duplicate IDs
- Audio Merge: intermediate file 'audio' for each mod
- Audio Merge: Better tracking of modified XML nodes by adding additional atributes
- Textures Merge: validation of duplicate IDs
- Textures Merge: Intermediate files 'textures' and 'animations' for each mod <<<<
- Textures Merge: Better tracking of modified XML nodes by adding additional atributes
- XML Merge: Autogenerates texture IDs (mod self-overriding its XML nodes)
- XML Merge: Validation of one mod overriding XML nodes from other mods
- XML Merge: Better tracking of modified XML nodes by adding additional atributes
- XML Patch: intermediate files for later analysis
- Version Info: generation of version.txt
- Version Info: set version of 'haven' file

## JAVA Mods

- **ATTENTION**: The game is currently shiped with java 1.8.0_461
- Make sure you understand what are the relevant Java technologies available for that version of java!
- aspectjweaver-1.9.19.jar and aspectj-1.9.19.jar are copied over to Space Haven directory
- The 2 libraries above and the mods' JAR files are added to config.json
- "vmArgs" are adjusted accordingly (macOS needs additional "-XstartOnFirstThread")

## Exporting assets

- **ATTENTION**: You must agree with BugByte EULA and with modders' LICENSES in order to export assets
- Exporting of ORIGINAL game libraries and exploded textures
- Exporting of MODIFIED game libraries and exploded textures

## License and Usage

Currently this is a private, proprietary project developed by Ghostkeeper666.
You are not allowed to copy, redistribute, or publish any part of this code (including uploading it to another repository or sharing it publicly) without Ghostkeeper666's explicit public written permission, which may be revoked at any time.
You are also not allowed to copy, redistribute, or publish any files of this application without Ghostkeeper666's explicit public written permission, which may be revoked at any time.
