# What are Space Haven's main modding tools ?

Historically, "**Mod Loader**" was the first tool developed for modding Space Haven
- Developed using the Python script-like interpreted programming language
- Requires a python installation to run
- One way around this is to "freeze" or "bundle" Python as an application
- Windows users get an application, while macOS and Linux users get to run the python code

There was a high demand for:
- detailed logs
- mod validation features
- user friendly and highly-responsive user interface
- a modding tool capable of loading lots of mods
- a solution for mod ID conflicts
- game version validation
- support for list of mod dependencies
- support for list of known mod conflicts
- allow itself to be closed during gameplay
- provide some minimal help for its users
- do not swallow possible execution errors
- too many intermediate files are persisted in the Space Haven directory
- too many intermediate files are persisted in the mod directory


# Why is Space Haven Launcher a next generation modding tool ?

What motivated the development of a complete new tool?
- **Mod Loader** is known as being a raw unforgiving tool
- It has a very long list of desired features to be addressed
- Some requirements are constrained by its implementation language
- One person was motivated enough to bring quality of life features for modders and players!

# Why is Space Haven Launcher a next generation modding tool ?

**Space Haven Launcher** capabilities:
- It provides a very useful **mods.json** file for JAVA mods
- It validates mods during load time: conflicts, dependencies, ID issues
- It allows the player to resolve mod ID conflicts
- It provides an easy and responsive user interface for displaying many mods with many mod variables
- It provides valuable log feedback for players AND moddders
- It provides help and information about modding
- It is configurable and persists user settings
- It does not touch the original spacehaven.jar file in order to run a modded game
- It does not write intermediary files to mod directories 
- It perfectly detects when the modded game needs to be rebuilt
- It provides a high-performance XML mod build system
- It does not  during night-time gameplays
- Instead of flash-banging your eyes, it shows mod backgrounds, fan art, and screenshots of early stages of the game!

# Roadmap - Future Work

- Implement an improved XML annotation system
- Finish the 'haven' file learning topic
- Provide a complete JAVA mod tutorial
- Preview of new facilities (and save it to *.webp files too)
- Export of any in-game animation to animated *.webp files
