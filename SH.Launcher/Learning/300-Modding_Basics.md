# Modding Basics

Space Haven mods change the content of the game.

There are 3 types of Space Haven mods:
- **XML** mod
- **JAVA** mod
- **XML**+**JAVA** mod

# How are mod files organized?

Valid Space Haven mods must follow this exact directory structure:
- The mod's **Root Directory** contains the files **info.xml**, **description.md**, **background.png**, **\*.jar files**
- The **audio** subdirectory contains **\*.mp3** or **\*.ogg** files
- The **textures** subdirectory contains **\*.png** image files
- The **library** subdirectory contains big chnks of XML to be merged into the game's XML files
- The **patches** subdirectory contains patch operations described in XML format to add, remove or delete stuff in the game's XML files

A quick overview of these files:
- **info.xml** is a mandatory file for all mods
- **description.md** and **background.png** are optional files
- The **\*.jar** files is the core part of a JAVA mod, but the modders may also add other miscellaneous files
- everything else in the above listed subdirectories are part of a XML mod

For instance, a complete **XML**+**JAVA** mod looks like this:

```
└── MyNewFacility
    ├── audio
    │   └── facilities_NewFacility_Noise.ogg
    ├── library
    │   ├── audio_NewFacility.xml
    │   ├── haven_NewFacility.xml
    │   ├── textures_NewFacility.xml
    │   └── texts_NewFacility.xml
    ├── patches
    │   ├── audio_NewFacility.xml
    │   ├── haven_NewFacility.xml
    │   ├── textures_NewFacility.xml
    │   └── texts_NewFacility.xml
    ├── textures
    │   ├── NewFacility-FrontView.png
    │   └── NewFacility-BackView.png
    ├── background.jpg
    ├── description.md
    ├── info.xml
    └── MyNewFacility.jar
```


# How do I install mods on Steam?

- Subscribe to the mod on Space Haven's Steam Workshop page
- **IMPORTANT**: Do it in the Steam Application, NOT in your browser!
- Steam "SHOULD" synchronize your subscribe mods to its latest version
- **KNOW ISSUE**: sometimes Steam fails to synchronize mods

On Steam workshop, the directories are just a bunch of IDs (numbers):
- The root directory name for **Space Haven** workshop items is **979110**
- The root folder of each mod is also a number
- This makes it hard to manually find the mods: the only way is by checking the info.xml file

This has direct consequences for mods:
- The **mod name** must be correctly stated in the **info.xml** file
- Mods should **NOT** use any special character in their **mod name** (see INFO.XML learning topic about this)

```
Steam
└── steamapps
    └── workshop
        └── content
            └── 979110
                └── 0123456789
                    ├── audio
                    │   └── facilities_NewFacility.ogg
                    ├── library
                    │   ├── audio_NewFacility.xml
                    │   ├── haven_NewFacility.xml
                    │   ├── textures_NewFacility.xml
                    │   └── texts_NewFacility.xml
                    ...
```

# How do I install mods from Nexus Mods?

From **Nexus Mods** website:
- Download the mod's compressed **\*.zip** file
- In case a mod uses the compressed **\*.rar** format (WinRAR) you can use 7-zip to extract it
- Create your **Classic Mods Directory**: this is the **mods** subdirectory under the same directory of **spacehaven.jar**
- Extract the mod to your **Classic Mods Directory**
- It should look like thie:

On **Windows** and **Linux**

```
SpaceHaven
├── spacehaven.jar
└── mods
    └── MyNewFacility (mod)
        ├── audio
        │   └── facilities_NewFacility.ogg
        ├── library
        │   ├── audio_NewFacility.xml
        │   ├── haven_NewFacility.xml
        │   ├── textures_NewFacility.xml
        │   └── texts_NewFacility.xml
        ...
```

On **macOS**
- The OS may warn you about changes in the application

```
spacehaven.app
└── Contents
    └── Resources
        ├── spacehaven.jar
        └── mods
            └── MyNewFacility (mod)
                ├── audio
                │   └── facilities_NewFacility_Noise.ogg
                ├── library
                │   ├── audio_NewFacility.xml
                │   ├── haven_NewFacility.xml
                │   ├── textures_NewFacility.xml
                │   └── texts_NewFacility.xml
                ...
```


# What can a XML mod do?

**What it can do:**
- Define new entities or modify game entities (and all their parameters)
- Define which textures and audio are used in the game
- Define or modify texts used in the game
- Allow the player to parametrize your mod by using mod variables

**What it can NOT do:**
- Define a game mechanic
- Create new user interfaces
- Modify the behavior of the artifical intelligence
- Create new game effects


# What can a JAVA mod do?

**What it can do:**
- Define a game mechanic
- Create new user interfaces
- Modify the behavior of the artifical intelligence
- Create new game effects
- Since it is a programming language, there are technically no limits!

**What it usually SHOULD NOT do:**
- Define new game entities which could be added using XML modding
- Define which textures and audio are be used in the game
- Allow the player to parametrize your mod by using mod variables


# Why not write everything in JAVA mods, since XML modding is limited?

JAVA mods could:
- break easily with every new released game version
- potentially cause unknown side-effects to the game
- possibly have never been tested against other JAVA mods
- contain harmful software

So the best strategy for JAVA modding is to touch just what is required to make the mod work!
