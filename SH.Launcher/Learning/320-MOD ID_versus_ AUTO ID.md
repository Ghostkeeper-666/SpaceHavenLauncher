# Modding Basics: The MOD-ID Conflict Problem

This section answers the most important question:

**What IDs should be used for game objects in a new mod?**

The answer is:

**Just use the AUTO-ID system.**

# The AUTO-ID System: A Quick Summary

To solve **MOD ID** conflicts, **Space Haven Launcher** implements the **AUTO-ID** system:

* Use **MOD ID** only for older mods that already define one
* The **AUTO-ID** system is strongly recommended for all new mods
* **Space Haven Launcher** assigns an **AUTO-ID** to each mod when **MOD ID** is set to **0** (zero)
* This is done when the mod is loaded. The **AUTO-ID** should **not** be stored in **info.xml**
* Use the **{id}** variable as the ID prefix everywhere in the mod's internal IDs
* After the **{id}** prefix, the mod must use a **4-digit number** to distinguish its internal IDs

# Why have an AUTO-ID System ?

**MOD ID** was originally proposed as a way to uniquely identify mods, but without central coordination, this strategy was doomed to fail

**What modders did before:**

* Many modders used their **Discord ID** as a prefix for all mod IDs
* However, this could still cause conflicts because Discord IDs are not guaranteed to be unique forever
* Furthermore, some people do not actively use Discord, and usernames or identifiers can change over time

**The concerning reality:**

* Most mods were not designed with automatic ID assignment in mind
* Modders had to verify that the IDs they chose were not already used by other mods
* As the number of mods increased, this process became increasingly painful
* This was made worse by the tendency of modders to choose appealing round numbers, increasing the chance of collisions

**Restrictions on proposed solutions:**

* Nobody wanted a central server responsible for managing **MOD ID** assignments
* Historically, BugByte has shown little interest in providing official support for modding-related issues
* Any solution needed to remain compatible with existing mods

**Final proposed solution — use a deterministically generated ID:**

* The ID must remain constant for a given mod
* The ID must depend only on unique mandatory properties of the mod
* The ID should never need to be stored online or published in mod metadata
* The ID should have a very low probability of colliding with existing **MOD IDs**

# How exactly does the AUTO-ID System work ?

* Set **MOD ID** to `0` (zero)
* Whenever you define a new internal ID in your mod, use the `{id}` prefix
* Combine the `{id}` variable with a 4-digit number, for example: `mid="{id}0125"`
* Do not use fewer than 4 digits or more than 4 digits. It **must be exactly 4 digits**
* **Space Haven Launcher** replaces all occurrences of `{id}` with the **AUTO-ID** assigned to your mod

What happens if an **AUTO-ID** conflicts with another mod's **MOD ID**, or by extreme coincidence with another mod's **AUTO-ID**?

In this rare case, the fallback solution is the **CUSTOM-ID** system:

* This should only be used as a last resort
* The player chooses a **CUSTOM-ID** number to replace either the conflicting **AUTO-ID** or **MOD ID**
* The chosen **CUSTOM-ID** must remain constant for a given savegame series
* For mods using the **AUTO-ID** system, this replacement has no negative impact
* For mods using the older **MOD ID** system, **Space Haven Launcher** attempts to replace the **MOD ID** with the **CUSTOM-ID** wherever possible
* All of this information is clearly explained to players who choose to modify **CUSTOM-ID** values

# How does Space Haven Launcher generate the AUTO-ID ?

**AUTO-ID uniqueness**

* The only **true source of uniqueness** for a mod is its **mod name**, as defined in the **info.xml** file
* **Space Haven Launcher** uses the mod name as the input for generating the **AUTO-ID**
* Therefore, duplicate mod names are not allowed in **Space Haven Launcher**

Provided that the assigned **{id}** value does not collide with another mod's ID, and that all mods use **4-digit internal ID suffixes**, the resulting ID format **{id}XXXX** has a very low probability of colliding with IDs used by other mods

**AUTO-ID generation algorithm**

* **SHA-256** is used as the hashing algorithm, and a modulo operation is used to limit the range of generated **AUTO-IDs**
* Since **BugByte** uses lower ID ranges for its own definitions, all IDs in the range **(0 - 69,999)** are reserved exclusively for **BugByte**
* Since all final game IDs must remain within the range **(0 – 2,147,483,647)**, the **AUTO-ID** range is limited to **(7 – 214,747)**
