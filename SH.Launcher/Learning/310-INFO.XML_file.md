# Modding Basics: INFO.XML

The **info.xml** file contains basic information about the mod - without it your mod won't even be listed.

It also defines the mod variables and their default / suggested values which you can later use in your mod.

# INFO.XML template

Use the following template for your **info.xml**, and take some time to read what are all those fields at least once:

```
<mod>
  <name>Your unique mod name</name>
  <author>Preferably your Discord nickname</author>
  <version>1.0.0</version>
  <modid>0</modid>
  <description>
  A detailed description of your mod
  Space Haven Launcher supports description.md file
  If you provide a description.md file, then this description here will be ignored
  </description>
  <spacehaven op=">=" v="1.0.0" />
  <spacehaven op="<" v="2.0.0" />
  <modconflict n="Name of other incompatible mod 1" op="<" v="1.0" />
  <modconflict n="Name of other incompatible mod 2" op="<" v="1.1" />
  <moddependency n="Name of required mod 1" op=">=" v="1.0" />
  <moddependency n="Name of required mod 2" op=">=" v="0.7.5-alphaBetaWhatever" />
  <foregroundcolor>#7F7FFF</foregroundcolor><!-- Reminder: don't forget to add a background.jpg file too :) -->
  <config>
    <var default="1"    value="2"   name="{YourVariableName1}">A very short description for variable 1 </var>
    <var default="High" value="Low" name="{YourVariableName2}">A very short description for variable 2 </var>
  </config>
</mod>
```

# INFO.XML fields

This sections describes all info.xml fields and their purpose

### &lt;name&gt;   (mandatory)

`<name>Your unique mod name</name>`

The unique name of your mod among all other possible mods
- Best practice is to use only alphabetical and numerical characters
- If this name conflicts with another mod, you won't be able to load both at the same time
- Since the name is the only true source of uniqueness, it is also used for generating intermediary files by modding applications
- Valid characters are: whitespace, `[a-z]`, `[A-Z]`, `[0-9]`, `_`, `-`,`(`, `)`
- If other characters in the mod name are found, they are replaced by '_'

**WHY ARE THE MOD NAME CHARACTERS RESTRICTED ?**
- The root folder of each mod in Steam Workshop is just a numeric ID
- The only way to uniquely identify a mod is by its **mod name** stated in the **info.xml** file
- Modding tools use the **mod name** for unique identification purposes and for intermediate filenames
- Therefore the character set had to be restricted


### &lt;author&gt;   (mandatory)

`<author>Author Name</author>`

The mod author's name
- Try to use the same nickname among your mods
- Use your discord nickname or whatever, just don't let it empty

### &lt;version&gt;   (mandatory)

`<version>1.0.0</version>`

The current version of the mod
- It does NOT have to follow Space Haven version
- Best practice is to use simple standard versioning numbers

### &lt;description&gt;   (mandatory)

`<description>1.0.0</description>`

Describes whatever the author wants to say about the mod
- Try describe the important part first; most people read just the first line
- If you want to show well-fomatted text, provide a description.md file too

### &lt;modid&gt;

`<modid>0</modid>`

A base identification number is required if the mod defines new game objects:
- You should prefer the new **AUTO-ID** system of **Space Haven Launcher**
- This ID is a prefix for all IDs within your mod
- All internal IDs defined by a a mod must have 4 digits, plus MOD ID as prefix

If you insist using a fixed MOD ID over the SUTO-ID system:
- **Beware**: your mod will probably cause mod conflicts in the future
- You are unnecessarily hard-coding an ID value which someone else may use for a very popular mod

The MOD ID must obey this rules:
- Have a maximum of 6 digits
- Be within the range (7 <= modID <= 214747)
- It must be unique among all existing Space Haven mods!
- Otherwise your mod is doomed to fail in the long run

**Use Space Haven Launcher's AUTO ID system isntead**

- You set: `<modid>0</modid>`
- You use the variable **{id}** as prefix everywhere in your internal IDs
- Use exactly 4 digits after the **{id}** prefix for internal IDs, e.g.

`mid="{id}0201"`

- **Space Haven Launcher** then auto-generates the '{id}' variable value in **ALL mod files**
- **Space Haven Launcher** allows the player to change the ID in case of ID conflicts

### &lt;config&gt;   (optional)

This section defines the mod variables:
- Mod variables are used to allow the player to customize mod parameters
- A mod variable is defined by the `<var>` tag
- A mod variable name must always be enclosed by curly brackets, e.g. {MyVariable001}
- Each variable has a `value` attribute used as the mod's suggested value
- Each variable has a `default` attribute used as the mod's original value
- Each variable has a description text, which is what the player will see!

**Space Haven Launcher** aliases:
- Node `<config>` can be safely replaced by `<variables>` or simply `<vars>`
- Attribute `value` can be safely replaced by `suggested`
- Attribute `default` can be safely replaced by `original`

### &lt;modconflict&gt;

`<modconflict n="Mod Name" op=">=" v="1.0.0" />`

This lists known incompatibilities with conflicting mods:
- Values for attribute `n` or `name` are: the name of the other mod
- Values for attribute `op` or `operator` are: `==`, `>=`, `>`, `<`, `<=`, `any`
- Values for attribute `v` or `version` are: any valid version number (not required for operator `any`)
- If not operator is provided, the default version operator is `any`
- You may repeat `<modconflict ... />` entries as many times as required

### &lt;moddependency&gt;

`<moddependency n="Mod Name" op=">=" v="1.0.0" />`

This defines which other mods are required to run your mod:
- Values for attribute `n` or `name` are: the name of the other mod
- Values for attribute `op` or `operator` are: `==`, `>=`, `>`, `<`, `<=`, `any`
- Values for attribute `v` or `version` are: any valid version number (not required for operator `any`)
- If not operator is provided, the default version operator is `any`
- You may repeat `<moddependency ... />` entries as many times as required

### &lt;spacehaven&gt;

`<spacehaven op=">=" v="1.0.0" />`

This defines the required version of **Space Haven**:
- Values for attribute `op` or `operator` are: `==`, `>=`, `>`, `<`, `<=`
- Values for attribute `v` or `version` are: any valid version number

### &lt;spacehavenlauncher&gt;

`<spacehavenlauncher op=">=" v="1.0.0" />`

This defines the required version of **Space Haven Launcher**:
- Values for attribute `op` or `operator` are: `==`, `>=`, `>`, `<`, `<=`
- Values for attribute `v` or `version` are: any valid version number

### &lt;foregroundcolor&gt;

`<foregroundcolor>`#7F7F7F`</foregroundcolor>`

Sets the accent color of your mod page in **Space Haven Launcher**
- You can also provide a **backgroung.jpg** file for the mod page's background
- Make the background image darker by using an image editor
- Then just compare it to the other mod's background darkness

**That's all !**
