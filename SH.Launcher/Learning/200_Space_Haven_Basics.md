# Space Haven Basics: Overview

From now on, most topics have a higher importance for modders, but not for players

Here we introduce how Space Haven was implemented

# How is Space Haven built ?

- Space Haven was developed in Java, and uses XML files to define game objects, textures, audio files
- The game delivers and runs on **Java Runtime Environment 8.0.4720.8** - be aware of its limitations!
- The **spacehaven.jar** file contains the Java classes, XML files, CIM texture files, and MP3/OGG audio files, font files, among other miscellaneous stuff
- Almost everything relevant to you is inside the **spacehaven.jar** file
- The **spacehaven.jar** is actually a simple **ZIP file** - you can see its content with tools like 7-zip

# Important Space Haven files

The most important Space Haven files for modders are:
- **config.json**: for JAVA modders
- **spacehaven.jar**: for all modders
- Both are located in the game's JAR directory
- The game's JAR directory is different for each supported platform (Windows, Linux, macOS)

# XML files

These the most important XML files inside the **spacehaven.jar** file:
- **library**/**haven**: definitions of game entities and game mechanics parameters
- **library**/**texts**: definitions of most of the user interface texts used in the game
- **library**/**audio**: definitions of music and sound effects
- **library**/**textures**: definitions of texture files and sprite regions
- **library**/**animations**: combines diverse textures, in static or animated form
- **library**/**files**/**spacehaven.xml**: contains the newest global settings for Space Haven

# Audio

Audio files are located in the follwoing **spacehaven.jar**'s inner subdirectories:
- **library**/**music**/**mp3** - game music as "\*.mp3" files
- **library**/**music**/**ogg** - game music as "\*.ogg" files
- **library**/**sound**/**mp3** - UI sounds and game sound effects as "\*.mp3" files
- **library**/**sound**/**ogg** - UI sounds and game sound effects as "\*.ogg" files

# Textures

Textures are packed into Space Haven's **CIM** files:
- Texture images are stored as **library**/**\*.cim** inside **spacehaven.jar**
- **CIM** is a raw image format used for all **Space Haven** textures
