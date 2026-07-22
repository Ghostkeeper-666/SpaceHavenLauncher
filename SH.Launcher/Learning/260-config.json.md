# Basics of Space Haven: the "config.json" file

The **config.json** file defines how the parameters used by the executable file of Space Haven for starting the game

The **config.json** file:
- defines the classPaths to JAR files
- defines the entry point
- defines additional arguments passed to the JRE

How is it used by **Space Haven** ?
- The game runs on **Java Runtime Environment (JRE)**, version **8.0.4720.8**
- The game's executable calls the **JRE** passing arguments defined in **config.json**

The **Space Haven**'s original **config.json** file looks like this:

```
{
  "classPath": [
    "spacehaven.jar"
  ],
  "mainClass": "fi.bugbyte.spacehaven.steam.SpacehavenSteam",
  "vmArgs": [
    "-Xmx4G"
  ]
}
```

**Space Haven Launcher** does **NOT** use **config.json**, it runs the game by directly calling the JRE
