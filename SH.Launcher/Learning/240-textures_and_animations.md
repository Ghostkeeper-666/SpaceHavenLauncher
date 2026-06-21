# Space Haven Basics: Textures and Animations

In this section the following topics are explained:
- Basic texture and animation concepts
- How Space Haven defines its graphic elements

# Basic Concepts

The term **texture** is broad and can be mistakenly used with different meanings. Therefore we use technical terms here:
- **Sprite**: a small image, usually with some transparency, used to decorate a part of an object at a given viewing angle
- **Sprite Sheet**: a large image, which stores several **Sprite** images inside it - it's like a big pack of minor images
- **SpriteAtlas**: a whole set of **Sprite Sheets** - from the game's original **Sprite Sheets**, or from a given mod

**Sprite Sheets** exist simply because it is much faster to load few big images to a graphics card during the gameplay over processing thousands of tiny image files

It's also necessary to define the position and the size of all **Sprites** contained inside a **Sprite Sheet**
- Since that is metadata, it is usually defined in separate data files (e.g. XML files, JSON files, etc)
- In order to uniquely identify every **Sprite**, an identification is assigned to each one in the metadata

An **Animation** is then used to combine one or more **Sprites**:
- An **Animation** is also pure metadata - it is **NOT** a video or a sequence of plain images
- An **Animation** can be static (containing a single **Frame**) or animated (containing multiple **Frames**)
- Each **Frame** is displayed for a short period of time determined by the **Frame Rate**
- A **Frame** contains the position, rotation, scale, and color mask information of each **Sprite** used within it
- **Bones** are used to combine multiple **Animations**
- Each **Animation** has at least 1 **Root Bone**
- It is called the **Root Bone** because it forms the root of a tree structure of **Bones**:
- The **Root Bone** can contain zero or more child **Bones**, which in turn can also have their own children
- This forms a **Skeleton** of **Bones**, each of which may have **Sprites** or entire **Animations** attached to it
- Since each frame can also define **Bone** positions, the **Skeleton** can move, i.e. be fully animated!

If you understand these basic concepts, you are now ready to define new graphics for your mod elements

# How Space Haven defines its graphic elements

**Space Haven** makes extensive use of XML files, including for defining metadata
- metadata about **Sprite Sheets** and their **Sprites**
- metadata about **Animations**

The **textures** XML file contained in the **spacehaven.jar** file defines:
- what are the **Sprite Sheets**
- how are the **Sprites** stored in each **Sprite Sheets**
- it defines the **Sprite Repository** for the whole game

The **animations** XML file contained in the **spacehaven.jar** file defines:
- metadata about all **Animations** used in the game
- defines a unique **name** for each **Animation**

The **haven** XML file contained in the **spacehaven.jar** file defines:
- game entities (facilities, ships, items, trade prices, etc)
- it maps **Animations** to game objects using **Animation** names

Space Haven uses **dimetric projection** (NOT isometric projection):
- each tile in the game is 64 x 32 (width x height) => 2:1 proportion
- Therefore the view angle is 26.56505118° (i.e. atan(0.5)), and **NOT** 30° as in isometric projection
- This is a common standard used in pixel-art video games

The coordinate system used in the game is:
- X grows from left to right, Y grows upwards
- This could cause some confusion with the standard computer image system, where Y grows downwards

The **Tiles** are those small diamond-shape forms in the game's **Tile Grid**, on which game objects can be positioned
- each **Tile** is 64 x 32 pixels big

A game object is positioned over the **Tile Grid** and is subdivided into minor pieces:
- a game object uses a small area of the **Tile Grid**, e.g. 3x3 **Tiles**
- each game object is defined as a set of smaller parts in the **haven** file
- a smaller part can contain one or more references to **Animations**
- a smaller part can flip its **Animations** horizontally (i.e. mirror them)
- therefore a smaller part can be used to define the visuals of multiple **Tiles** of the same game object

# The "textures" XML file

The big image of a **Sprite Sheet** is stored in **CIM** format in a **\*.cim** file:
- The **CIM** format stores the image itself — its format is discussed later
- All **Sprite Sheet** and **Sprite** metadata is stored in the **textures** file like below:

Space Haven file: **textures**

```
<AllTexturesAndRegions>
  
  <textures>
    <t i="0" w="2048" h="2048"/>
    <t i="1" w="1024" h="1024" f="1" min="1" max="1"/>
    <t i="2" w="2048" h="2048"/>
    <t i="3" w="2048" h="2048" f="1" min="0" max="0"/>
    <t i="4" w="2048" h="2048" f="1" min="0" max="0"/>
    ...
  </textures>

  <regions>
    <re n="0" t="0" x="2" y="2" w="88" h="144" id="10117"/>
    <re n="1" t="0" x="2" y="148" w="80" h="136" id="10760"/>
    ...
	<re n="3514" t="4" x="880" y="509" w="72" h="58" id="9828"/>
	<re n="3515" t="4" x="954" y="509" w="72" h="58" id="9829"/>
	<re n="3516" t="4" x="1028" y="509" w="72" h="58" id="9946"/>
    ...
  </regions>

</AllTexturesAndRegions>
```

### The &lt;textures&gt; section

This section defines **Sprite Sheet** metadata:
- Attribute **i** defines the name of the CIM file containing the **Sprite Sheet** image
- Attribute **w** is the width, and attribute **h** is the height in pixels of the **Sprite Sheet** image
- Attribute **f** means enable filtering: "**0**" for yes, "**1**" for no
- Attribute **min** is the filter interpolation applied when shrinking the image: "**0**" for **Nearest**, "**1**" for **Linear**
- Attribute **max** is the filter interpolation applied when growing the image: "**0**" for **Nearest**, "**1**" for **Linear**

### The &lt;regions&gt; section

This section defines **Sprite** metadata within the **Sprite Sheet**:
- Attribute **n** defines the "name" of the **Sprite** (yes, a numeric name)
- Attribute **t** defines in which **Sprite Sheet** are the **Sprite** pixels contained
- Attributes **x** and **y** define the top left point of rectangle containing the **Sprite** pixels in the **Sprite Sheet**
- Attributes **w** and **h** define the width and height of the rectangle containing the **Sprite** pixels
- Attribute **id** is intended to uniquely identify **Sprites**, but the original game data contains duplicates
- So the **n** attribute ("name") is actually the unique identifier of each **Sprite**
- The "name" is also used in the animations file to refer to a specific **Sprite** image

Although the **Sprite**'s **name** seems to be a strictly sequential number, it is **NOT**:
- Valid values only need to be less than the maximum 32-bit signed integer value, i.e. **(0 - 2,147,483,647)**

Therefore:
- mods use the lower ID digits of an ID number to distinguish internal sprites
- mods use the higher ID digits of an ID number to distinguish themselves from other mods
- mods refer to existing game **Sprites** by their **name** (which is a number)

# The "animations" XML file

Each **Animation** is defined by a **&lt;ba&gt;** XML node

```
<AllAnimations>
  <animations>
...
    <ba n="door1" f="16" ks="1,4,6,8" id="111">
      <bones>
        <b id="0">
          <pos>
            <p f="1" x="0" y="24" sx="1" sy="1" r="0" col="-1"/>
            <p f="4" x="13.71" y="17.14" sx="1" sy="1" r="0" col="-1"/>
            <p f="6" x="22.85" y="12.57" sx="1" sy="1" r="0" col="654311423"/>
            <p f="8" x="32" y="8" sx="1" sy="1" r="0" col="16777215"/>
          </pos>
        </b>
      </bones>
      <items>
        <assetPos vf="1:1" bi="0" x="0" y="0" sx="1" sy="1" r="0" a="3921"/>
      </items>
    </ba>
...
    <ba n="fire1" f="30" ks="1,2,3,4,5,6,7,8,9,10,11" id="115">
      <bones>
        <b id="0">
          <pos>
            <p f="1" x="0" y="0" sx="1" sy="1" r="0" col="-1"/>
          </pos>
        </b>
      </bones>
      <items>
        <assetPos vf="1:1,2:0" bi="0" x="2" y="-7" sx="1" sy="1" r="0" a="2954"/>
        <assetPos vf="1:0,2:1,3:0" bi="0" x="2" y="-7" sx="1" sy="1" r="0" a="2955"/>
        <assetPos vf="1:0,3:1,4:0" bi="0" x="2" y="-7" sx="1" sy="1" r="0" a="2956"/>
        <assetPos vf="1:0,4:1,5:0" bi="0" x="2" y="-7" sx="1" sy="1" r="0" a="2957"/>
        <assetPos vf="1:0,5:1,6:0" bi="0" x="2" y="-7" sx="1" sy="1" r="0" a="2958"/>
        <assetPos vf="1:0,6:1,7:0" bi="0" x="2" y="-7" sx="1" sy="1" r="0" a="2959"/>
        <assetPos vf="1:0,7:1,8:0" bi="0" x="2" y="-7" sx="1" sy="1" r="0" a="2960"/>
        <assetPos vf="1:0,8:1,9:0" bi="0" x="2" y="-7" sx="1" sy="1" r="0" a="2961"/>
        <assetPos vf="1:0,9:1,10:0" bi="0" x="2" y="-7" sx="1" sy="1" r="0" a="2962"/>
        <assetPos vf="1:0,10:1,11:0" bi="0" x="2" y="-7" sx="1" sy="1" r="0" a="2963"/>
        <assetPos vf="1:0,11:1" bi="0" x="2" y="-7" sx="1" sy="1" r="0" a="2964"/>
      </items>
    </ba>
...
    <ba n="assembler2B" f="30" ks="1" id="5564">
      <bones>
        <b id="0">
          <pos>
            <p f="1" x="0" y="0" sx="1" sy="1" r="0" col="-1"/>
          </pos>
        </b>
      </bones>
      <items>
        <assetPos vf="1:1" bi="0" x="0" y="0" sx="1" sy="1" r="0" a="700"/>
        <assetPos vf="1:1" bi="0" x="2" y="17" sx="1" sy="1" r="0" a="272"/>
      </items>
    </ba>

    <ba n="rename_me_5566" f="30" ks="1" id="5566">
      <bones>
        <b id="0">
          <pos>
            <p f="1" x="0" y="0" sx="1" sy="1" r="0" col="-1"/>
          </pos>
        </b>
      </bones>
      <items>
        <assetPos vf="1:1" bi="0" x="32" y="48" sx="1" sy="1" r="0" l="0" sf="1" se="1" an="assembler1B"/>
        <assetPos vf="1:1" bi="0" x="-32" y="48" sx="1" sy="1" r="0" l="0" sf="1" se="1" an="assembler4B"/>
        <assetPos vf="1:1" bi="0" x="0" y="32" sx="1" sy="1" r="0" l="0" sf="1" se="1" an="assembler2B"/>
        <assetPos vf="1:1" bi="0" x="64" y="32" sx="1" sy="1" r="0" l="0" sf="1" se="1" an="assembler3FH0"/>
        <assetPos vf="1:1" bi="0" x="32" y="16" sx="1" sy="1" r="0" l="0" sf="1" se="1" an="assembler2F"/>
        <assetPos vf="1:1" bi="0" x="-32" y="16" sx="1" sy="1" r="0" l="0" sf="1" se="1" an="assembler3B0"/>
        <assetPos vf="1:1" bi="0" x="0" y="0" sx="1" sy="1" r="0" l="0" sf="1" se="1" an="assembler1FH"/>
        <assetPos vf="1:1" bi="0" x="64" y="0" sx="1" sy="1" r="0" l="0" sf="1" se="1" an="assembler4F"/>
      </items>
    </ba>
...
  </animations>
</AllAnimations>
```

### The &lt;ba&gt; node

- Attribute **n** is the **name** of the **Animation** - used as unique identification
- Attribute **f** is the **Frame Rate**
- Attribute **ks** is a list of **Key Frames**
- Attribute **id** is just a number which uniquely identifies the **Animation**

The **haven** file refers to **Animations** through their **name** attribute

### The &lt;assetPos&gt; node (asset)

These nodes define which asset (a **Sprite** or another **Animation**) will be used for the current animation
- Attribute **vf** defines in which frames the referenced asset is visible
- e.g. `vf="1:0,2:1,3:0"` means "set asset as invisible on frame 1, set asset as visible on frame 2, set asset as invisible on frame 3"
- Attribute **bi** defines to which **Bone** ID is the asset attached to
- Attributes **x** and **y** defines the offset in pixels when drawing the asset
- Attributes **sx** and **sy** define the scale applied to the asset
- Attribute **r** defines the rotation in degrees (0° - 360°) applied to the asset
- Attribute **a** is used to refer to the "name" of a single **Sprite** image
- Attribute **an** is used to refer to the "name" of a single **Animation**
- Attribute **l** defines whether the referenced **Animation** should play continuously (loop="1") or not (loop="0")
- Attribute **sf** is the first frame in which the **Animation** starts playing
- Attribute **se** is last frame in which the **Animation** stops playing
- If the referenced **Animation** is set to loop, then the whole animation must be played within start and end frame
- If the referenced **Animation** is not set to loop, then it starts playing at the start frame (using its own original frame rate) and stops playing at the given end frame

### The &lt;b&gt; node (bone)

Assets are attached to bones
- The **id** attribute of the <b> node is the unique identifier of the **Bone**

Bones define position, scale, rotation, and color mask of each asset attached to it at a given frame
- Attribute **f** defines the frame to which the data applies
- Attributes **x** and **y** define the position offset
- Attributes **sx** and **sy** define the scale
- Attribute **r** defines the rotation in degrees (0° - 360°)
- Attribute **col** defines the color mask used to colorize the asset
- `col="-1"` means do not colorize
- other values of **col** can be converted to hexadecimal to understand what are the color components

# Animation Rendering

Since an animation can be a composition of several other animations:
- Different **Animations** may use different **Frame Rates**
- A looping child **Animation**'s frame rate must be adjusted accordingly to the start/end frame
- It is **NOT** possible to pre-render all possible frame combinations for complex **Animation** compositions
- A target frame rate is chosen, and each **Animation** is asked to render itself at a given point in time
- Typically frame rates used in games are 30 fps or 60 fps
- Space Haven appears to use a 30 FPS **Frame Rate** (*NOT CONFIRMED*)
- **Sprite** assets are cropped to their content during animation rendering
- This can be the root cause for textures being wrongly offset by a few pixels in custom tools

# The CIM file format

**CIM** is **NOT** a standard image format - in **Space Haven**, it is essentially raw uncompressed image data:
- header: width, height, and pixel color format
- payload: raw pixel color data

Header:
- **width** and **height** are stored as 32-bit integer variables, using Big Endian binary serialization
- The **pixel format** expected for Space Haven is always = 4 (i.e. RGBA format, 1 byte per color channel)

Pixels are stored:
- by traversing the image rows from top to bottom (Y axis grows downwards)
- and each pixel in a row from left to right (X axis grows from left to right)
- each color channel of a pixel is stored in this order: Red, Green, Blue, Alpha
