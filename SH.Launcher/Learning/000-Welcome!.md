# Welcome to Space Haven Launcher!

Let's go through this FAQ before asking questions...

**Space Haven Launcher** mimics the game's theme:
- [Learning Computer](app://LearningComputer): the right place to **understand** the basics
- [System Core](app://SystemCore): the **settings** central - check this page if any directory was not auto-detected
- [Navigation Console](app://NavigationConsole): **launches original** or **modified games**, re-initializes Space Haven Launcher, exports game assets
- [Airlock](app://Airlock): a relaxing **screensaver**, with some fan art and screenshots from early stages of the game

# Directories and paths

The list of mods is presented after Space Haven is successfully found. If not found automatically, you can:
- Go to [System Core](app://SystemCore) and set the directories manually

# The MOD list

The mod list presents the mods and sets the mod build order too:
- The mod list can be reloaded by clicking on the left side buttons of [Navigation Console](app://NavigationConsole)
- Auto-detected: Mod conflicts, mod dependency issues, incompatibilities, ID issues
- You can change the order of your a mod by using the arrows at the top left area

# What can a MOD do to my game ?

- A mod can modify game settings, add or modify game entities, change game mechanics, etc
- In the mod page you can see tags like LIBRARY, PATCH, TEXTURE, AUDIO, JAVA, etc
- These tags give you a clue what the mod does
- If you see a LIBRARY tag or a PATCH tag, it means this is a XML mod

# How do I install mods ?

- **Reliable Method**: Download mods from [Nexus Mods](https://www.nexusmods.com/games/spacehaven/mods?sort=createdAt&timeRange=allTime) webpage and extract them to the classic mods directory
- **Alternative Way**: Subcribe them on [Steam Workshop](https://steamcommunity.com/app/979110/workshop/) - You must do it from the Steam application, otherwise it may not sync correctly!
- Make sure you don't download the same mod from both sources, since duplicate mods will cause issues
- The **Classic Mods Directory** and the **Steam Directory** must be correctly defined on [System Core](app://SystemCore) tab
- Use the mods from the [My Series](https://www.nexusmods.com/games/spacehaven/mods?sort=uniqueDownloads&timeRange=allTime&author=Ghostkeeper666) to adjust basic game settings and much more

# How do I enable or disable mods ?

- Click on the corresponding **★** STAR icon in the mod list (at the **left side of the screen**)
- Click on the **MOD TITLE** on the mod page (at the **right side of the screen**)
- To **enable/disable several mods**, use the two **★** STAR icons on the **top left area of the screen** 
- To restrict the mods you want to enable/disable, use the **search box** (at the **top left area of the screen**)

# How do I start a modified game ?

Go to [Navigation Console](app://NavigationConsole) and click on the **(M) Lever** to the game modifed by mods

# How do I reset to original "vanilla" game ?

Go to [Navigation Console](app://NavigationConsole) and click on the **(V) Lever** to run the original game

# Can I close Space Haven Launcher while Space Haven is running ?

Yes, you can safely close it.

There is one caveat if you use JAVA mods, and you want to reset **Space Haven** back to its original form:
- You must first run a vanilla game with **Space Haven Launcher** in order to reset the **config.json** file

# What happens if I disable a MOD and load a modded savegame ?

The game usually is robust enough to successfully load your saved game
- Unless the mod owner says otherwise, the game should not crash
- Any behavior defined by the mod should stop happening
- Entities defined by the mod should disappear
- Texture and audio modifications should be reset as well

# I updated my mods, are my changes to mod variables saved ?

Yes, and you decide what variable values you want to merge from the old version
- Just go to the mod's variables page and look at the **OLD MOD VALUES** column
- Play a little bit by clicking on mod variable cells to understand how you can easily set up what you need

# How to solve mod ID errors ?

You can solve most **mod ID** issues by chosing a **custom ID**
- You should preferably change the IDs of pure **JAVA mods**
- Mods which use **AUTO ID** feature don't care which **custom ID** you set

# How to solve mod dependency errors ?

- You must download the right mod dependency required by each the mod
- This includes donwloding the correct version of the dependency
- You must place the mod dependency at a higher position in the mod list
- Because mods are loaded from top to bottom

# How to solve conflicting mods ?

Short answer? You don't...
- You must choose between one or another
- If you use conflicting mods, there is a good chance of something going wrong during gameplay

# How to solve mods requiring a specific Space Haven version ?

Some mods are incompatible with specific versions of **Space Haven**
- XML mods could address the wrong entities
- JAVA mods could cause crashes during the gameplay
- The best you can do about it is to make sure you have all mods updated

# Now you are ready to go!
