# XML Modding: Overview

XML modding basically consists of:
- Merging XML
- Patching XML
- Adding Textures
- Adding or Replacing Audio

**This is just a quick summary, for detials go through the next learning topics**

# XML Merge

All files located under the mod's **library** subdirectory must be XML files:
- These follow the same XML structure of the game's XML files
- They are used to add or replace chunks of XML on **haven**, **texts**, and **animations** files
- They are used for merging metadata definitions of Textures
- They are used for merging metadata definitions of Audio

# XML Patch

All files located under the mod's **patches** subdirectory must be XML files:
- These follow the XML Patch Operation definitions
- They are used to patch the game's XML files
- They are not used for including new textures or audio

# Textures

Textures are added by placing **\*.png** files in the mod's **textures** subdirectory:
- The file format must be PNG
- These should be individual **Sprites**, not **Sprite Sheets**
- The textures files are referenced by **assetPos** XML nodes in **library**/**animations\*.xml** files

# Audio

Audio is added by placing **\*.mp3** and **\*.ogg** files in the mod's **audio** subdirectory:
- The file format must be MP3 or OGG
- The audio files are referenced by XML nodes in **library**/**audio\*.xml** files
- There are important rules to be followed, otherwise the game sound system stops working!
