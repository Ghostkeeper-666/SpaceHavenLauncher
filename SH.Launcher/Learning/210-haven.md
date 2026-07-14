# Space Haven Basics: Game Objects

Space Haven defines its game objects in JAVA code, and serializes (stores) some of their properties in XML files
- Not every game object property value is store in XML files
- This restricts what modders can achieve with XML modding

# Exporting the "haven" file with XML annotations

**Space Haven Launcher** allows you to export of the important game data:
- Files: haven, texts, audio, textures, animations, etc
- Textures: are converted from CIM files to PNG files

However, **haven** XML file is huge...
- There are few clues of what is each block of XML code
- The name and description of game objects is referenced using IDs, and is stored in the **texts** file
- This makes it very hard to find the right modding spots!

Exporting **haven** with **XML Annotations** helps a lot:
- Choose the language of **XML Annotations** in the [System Core](app://SystemCore) settings
- Optionally enable/disable export of textures, also in the [System Core](app://SystemCore) settings
- On [Navigation Console](app://NavigationConsole) click on **right side buttons** to export assets

Example of annotated **haven** file:

```
...
<!-- Ore Processor -->
<!-- Links to: 2648, 2649, 3518, 3519, 3591, 3592, 3593, 3594, 3600 -->
<!-- Indirectly links to: 3596, 3597, 3598, 3599, 3605 -->
  <me mid="3520" ec="18" costGroup="0" offsetRot="R0" nonSymmetrical="false" alternateY="false" _names="Ore Processor" _linkedByAll="" _linksToAll="2648, 2649, 3518, 3519, 3591, 3592, 3593, 3594, 3596, 3597, 3598, 3599, 3600, 3605">
    <data />
    <linked>
      <l id="3518" eid="1" gridOffX="1" gridOffY="1" rot="R0" layer="0" damageGroup="0" />
      <l id="3519" eid="2" gridOffX="1" gridOffY="0" rot="R0" layer="0" damageGroup="0" />
      <l id="2648" eid="3" gridOffX="1" gridOffY="0" rot="R0" layer="0" damageGroup="0" />
      <l id="2649" eid="4" gridOffX="0" gridOffY="0" rot="R0" layer="0" damageGroup="0" />
...
<!-- Autoturret, Small Shield Generator, Point Defense Turret, Modified Point Defense -->
<!-- Linked by: 2785, 2788, 3262, 3263, 3678, 3679, 4307, 4631, 4640 -->
<!-- Indirectly linked by: 2783, 3264, 3638, 4306, 4629 -->
<me mid="2787" ec="3" costGroup="0" offsetRot="R0" nonSymmetrical="false" alternateY="false" _names="Autoturret, Small Shield Generator, Point Defense Turret, Modified Point Defense" _linkedByAll="2783,2785,2788,3262,3263,3264,3638,3678,3679,4306,4307,4629,4631,4640" _linksToAll="">
  <data>
    <l type="Light" eid="1" gridOffX="0" gridOffY="0" layer="0">
      <element id="0" playAnimation="false" walkGridCost="0" lightValue="None">
...
```

# The "haven" file

TODO - This is going to be a long learning topic...
