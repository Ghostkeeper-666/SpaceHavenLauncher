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

A **Space Haven Launcher**'s modified **config.json** file looks like this:

```
{
    "classPath": [
        "aspectjweaver-1.9.19.jar",
        "aspectj-1.9.19.jar",
        "modifiedspacehaven.jar",
        "C:/Program Files (x86)/Steam/steamapps/workshop/content/979110/3730262305/AutoPriorityManager.jar",
        "C:/Program Files (x86)/Steam/steamapps/workshop/content/979110/3718678068/AutoTraderMod.jar",
        "C:/Program Files (x86)/Steam/steamapps/workshop/content/979110/3727772146/CrewChatter.jar",
        "C:/Program Files (x86)/Steam/steamapps/workshop/content/979110/3729980436/CustomizerPlus-1.0.0.jar",
        "C:/Program Files (x86)/Steam/steamapps/workshop/content/979110/3722400658/EchoesThroughHaven.jar",
        "C:/Program Files (x86)/Steam/steamapps/workshop/content/979110/3735271660/FogGeneratorBehaviour-0.2.7.jar",
        "C:/Program Files (x86)/Steam/steamapps/workshop/content/979110/3735271572/FarmingExpandedBehaviour-0.1.0.jar",
    ],
    "mainClass": "fi.bugbyte.spacehaven.steam.SpacehavenSteam",
    "vmArgs": [
        "-javaagent:./aspectjweaver-1.9.19.jar",
        "-Xmx4G"
    ]
}
```
