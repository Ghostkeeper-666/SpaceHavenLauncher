# Welcome to Space Haven Launcher!

Let's go through this FAQ before asking questions...

**Space Haven Launcher** mimics the game's theme:
- [Learning Computer](app://LearningComputer): the right place to **understand** the basics
- [System Core](app://SystemCore): the **settings** central - check this page if any directory was not auto-detected
- [Navigation Console](app://NavigationConsole): **launches original** or **modified games**, re-initializes Space Haven Launcher, exports game assets
- [Airlock](app://Airlock): a relaxing **screensaver**, with some fan art and screenshots from early stages of the game

# Paths

This app only works the paths are correctly set in [System Core](app://SystemCore)

# Mod list

The mod list defines the **mod loading order** into the game:
- The mod list can be reloaded by clicking on the **LEFT BUTTONS** of [Navigation Console](app://NavigationConsole)
- Reorder the mods by using the **up/down arrow buttons** located above the mod list

# What can mods do to the game ?

- A mod can change game settings, game entities, game mechanics, etc
- Each mod becomes tags like LIBRARY, PATCH, TEXTURE, AUDIO, JAVA, ...
- These tags give you a clue about what the mod does

# How to install mods ?

- **Reliable Method**: Download mods from [Nexus Mods](https://www.nexusmods.com/games/spacehaven/mods?sort=createdAt&timeRange=allTime) webpage and extract them to the classic mods directory
- **Alternative Way**: Subcribe them on [Steam Workshop](https://steamcommunity.com/app/979110/workshop/) - You must do it from the Steam application, otherwise it may not sync correctly!
- Make sure you don't download the same mod from both sources, since duplicate mod entries are not allowed!
- The **Classic Mods Directory** and the **Steam Directory** are defined in [System Core](app://SystemCore)
- Use mods from [My Series](https://www.nexusmods.com/games/spacehaven/mods?sort=uniqueDownloads&timeRange=allTime&author=Ghostkeeper666) to adjust most basic game settings

# How to ENABLE or DISABLE mods ?

- Click on the corresponding **★** STAR icon in the mod list (at the **left side of the screen**)
- Click on the **MOD TITLE** on the mod page (at the **right side of the screen**)
- To **enable/disable several mods**, use the two **★** STAR icons on the **top left area of the screen** 
- To restrict the mods you want to enable/disable, use the **search box** (at the **top left area of the screen**)

# How to start a modified game ?

Go to [Navigation Console](app://NavigationConsole) and click on the **(M) Lever** to the game modifed by mods

# How to start the original "vanilla" game ?

Go to [Navigation Console](app://NavigationConsole) and click on the **(V) Lever** to run the original game

# Can the Launcher be closed while playing Space Haven ?

Yes, you can safely close it

# What if a MOD is DISABLED and a modified SAVEGAME is loaded ?

This is usually not recommended:

- In principle, unless the mod author specifies otherwise, the game should not crash
- Entities defined by mods should simply disappear
- All texture and audio modifications should be reset to original

** Known Mod Removal Issues**

- If you use a mod which changes floors, you must first set original game floors before loading the savegame
- If characters get newly added skins (not ones which override existing ones), the game could crash

# When updating mods, are mod variables automatically updated ?

- The Launcher does its best to **KEEP** old variable values to the newly updated mod
- Check the mod's page and look at the **OLD MOD VALUES** column in the mod variables section

# How to solve MOD ID errors ?

- They are resolved by replacing the **mod ID** with a **custom ID**
- It's usually best to change the **mod ID** of mods tagged **only** with **JAVA** or **JAVA AOP** tags
- Mods using **AUTO ID** should not care about which **custom ID** is set

# How to solve MOD DEPENDENCY errors ?

- The mod dependencies with the correct version must be installed
- The dependencies must be placed at a higher position in the mod list, so they get loaded first

# How to solve MOD CONFLICTS ?

It's not possible, since the conflicting mods are performing incompatible actions
- It's best to choose between one mod or another
- If you proceed with conflicting mods anyway, there is a high chance of something going wrong

# How to solve mods requiring a specific SPACE HAVEN VERSION ?

Some mods are incompatible with specific newer versions of **Space Haven**
- Check whether there is a new version of the mod
- Old mods will potentially not work anymore
- Check the mod's last Space Haven version requirement to get a clue how old it is
- Also check the Nexus Web page to see how long ago the mod was last updated

# Now you are ready to go!
