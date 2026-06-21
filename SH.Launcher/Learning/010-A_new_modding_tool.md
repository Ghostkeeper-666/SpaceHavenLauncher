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

**For Players**

- **No more flashbangs** at night-time - instead of it you get fan art, game screenshots, and mod backgrounds
- **Responsive UI**: ultra-fast loading times, progress bars everywhere, and a visible log let you know what's going on
- **Mod variable values**: they are stored for the current and all previous versions of the mod - it's easy merge them
- **MOD ID conflicts** can now be resolved by the player too
- **Fast and reliable detection of mod build rebuilding** - no need to clear stuff manually anymore
- No pollution in Space Haven's and Mods' folders - all **intermediate files** are written elsewhere (good for Steam sync)
- Support and validation for **mod dependencies**, "**known mod incompatibilities**", Space Haven version, etc
- Valuable log feedback: **right clicking a log line** opens the related file, directory, program tab, or internet link

**For Modders**

- First of all, **a lot of modding help** is available - check **Learning Computer** tab - there is a full XML mod tutorial
- The new **AUTO-ID** system stops **MOD ID** conflicts
- **XML mods**: it provides a high-performance, robust, verbose XML mod build
- **JAVA mods**: it generates a super useful **mods.json** in Space Haven's JAR folder, with all evaluated variables and stuff!
- In the **Work Directory** of Space haven Launcher, there is plenty of information on how mods where joined together
- The **original spacehaven.jar is never touched**: a new **modifiedspacehaven.jar** is generated instead
- Optimized texture generation: **CIM files are smaller**
- **Improved XML Annotation**: exported game assets provide a **separate haven file** with XML annotations in your language of choice

# Roadmap - Future Work

- Finish the 'haven' file learning topic
- Provide a complete JAVA mod tutorial
- Preview of new facilities (and save it to *.webp files too)
- Export of any in-game animation to animated *.webp files
