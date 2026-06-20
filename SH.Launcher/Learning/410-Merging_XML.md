# XML Modding: Merge Operations

When creating new game entities you probably want a fast way of add a big block of XML code:
- to the **haven** file
- to the **texts** file

Or maybe you want to add:
- new **textures**
- new **audio**

For these purposes we use XML merging:
- Space Haven Launcher adds new XML entries
- Space Haven Launcher overrides existing XML entries
- Space Haven Launcher detects it should add or override based on the main ID attribute of each added block of XML

**Files used for XML merge must be in the library subdirectory of the mod**

# What can be done with XML Merge?

- Merge **animations**: see the **Modding Textures** topic
- Merge **audio**: see the **Modding Audio** topic
- Merge **text**
- Merge **game objects**

# Merge Text

To merge text new entries, create as many **library**/**texts_\*.xml** files in your mod as you need:
- Assuming you use the **AUTO-ID** system of **Space Haven Launcher**, you just set the ID as in the example below
- Copy the **pid** value from another similar text entry
- You translate the text from your language to all other languages using AI or the best of your languages knowledge
- Avoid overriding at all costs - do not use same IDs as other existing text entries, unless you are fixing bad translations
- To add text avoiding ID conflicts against other mods, use the **AUTO-ID** system of **Space Haven Launcher**

Example of **library**/**texts_\*.xml** file using the **AUTO-ID** system:

```
<t>
  <t id="{id}0906" pid="905">
    <EN>New Jukebox</EN>
    <ES>Nueva gramola</ES>
    <DE>Neue Musikbox</DE>
    <PL>Nowa szafa grająca</PL>
    <KO>새 주크박스</KO>
    <IT>Nuovo jukebox</IT>
    <CN>新点唱机</CN>
    <FR>Nouveau juke-box</FR>
    <CS>Nový jukebox</CS>
    <PTBR>Novo tocador de música</PTBR>
    <TR>Yeni Müzik Kutusu</TR>
    <RU>Новый музыкальный автомат</RU>
    <JA>新しいジュークボックス</JA>
  </t>
</t>
```

# Merge Game Objects

To merge new game object definitions, create as many **library**/**haven_\*.xml** files in your mod as you need:
- Each game object has a specific ID attribute name
- The table below lists the corresponding ID attribute name for each type of game object **supported by XML merge**
- The merge of the XML node (add / replace) will be performed **at the path level** given by the table below
- If the **ID attribute matches** with another exiting one, then the existing game object will be overriden!
- Be careful when overriding, because BugByte or other mods may expect specific game objects to exist in a specific form!
- To add stuff avoiding ID conflicts against other mods, use the **AUTO-ID** system of **Space Haven Launcher**

```
┌─────┬─────────────────────────────────────────┐
│ ID  │ Game Object Path                        │
╞═════╪═════════════════════════════════════════╡
│ id  │ /data/Accident/accident                 │
│ id  │ /data/AccidentList/list                 │
│ id  │ /data/Augmentation/augment              │
│ mid │ /data/BackPack/item                     │
│ id  │ /data/BackStory/backstory               │
│ id  │ /data/CelestialObject/celestialObject   │
│ cid │ /data/Character/character               │
│ id  │ /data/CharacterCondition/condition      │
│ cid │ /data/CharacterSet/characters           │
│ id  │ /data/CharacterTrait/trait              │
│ id  │ /data/CostGroup/group                   │
│ cid │ /data/Craft/craft                       │
│ id  │ /data/DataLog/dataLog                   │
│ id  │ /data/DataLogFragment/fragment          │
│ id  │ /data/DefaultStuff/stuff                │
│ id  │ /data/DefinedRoomType/definedRoom       │
│ id  │ /data/DialogChoice/choice               │
│ id  │ /data/DifficultySettings/settings       │
│ id  │ /data/Effect/effect                     │
│ mid │ /data/Element/me                        │
│ id  │ /data/Encounter/encounter               │
│ id  │ /data/ExodusFleetEvent/event            │
│ id  │ /data/ExodusMissionDialog/missionDialog │
│ id  │ /data/Explosion/explosion               │
│ id  │ /data/Faction/faction                   │
│ id  │ /data/FloorExpPackage/expPackage        │
│ id  │ /data/GameScenario/game                 │
│ id  │ /data/GOAPAction/action                 │
│ id  │ /data/IdleAnim/an                       │
│ id  │ /data/IsoFX/fx                          │
│ mid │ /data/Item/item                         │
│ id  │ /data/MainCat/cat                       │
│ cid │ /data/Monster/monster                   │
│ id  │ /data/Notes/stuff                       │
│ nid │ /data/ObjectiveCollection/collection    │
│ id  │ /data/PersonalitySettings/settings      │
│ id  │ /data/Plan/plan                         │
│ eid │ /data/Product/product                   │
│ id  │ /data/Randomizer/randomizer             │
│ id  │ /data/RandomShip/ship                   │
│ cid │ /data/Robot/robot                       │
│ id  │ /data/RoofExpPackage/expPackage         │
│ rid │ /data/Room/data                         │
│ id  │ /data/Sector/bg                         │
│ rid │ /data/Ship/data                         │
│ id  │ /data/ShipStarMapData/data              │
│ id  │ /data/SubCat/cat                        │
│ id  │ /data/Tech/tech                         │
│ id  │ /data/TechTree/tree                     │
│ eid │ /data/TradingValues/trade/t             │
└─────┴─────────────────────────────────────────┘
```

Example of **library**/**haven_\*.xml** file using the **AUTO-ID** system to add new product entries:

```
<data>
  <Product>

  <!-- Compost: Mild Alcohol => Water + Raw Chemicals + Carbon -->
  <product eid="{id}3366" type="Process" interactive="true" startOnly="false" itemScrapper="false" smelter="false" scrapper="false" composter="false" processValue="100" customPriority="-1">
    <needs>
    <l element="3366" howMuch="1" consumeEvery="1"/>
    </needs>
    <products>
    <l element="16" howMuch="0" produceEvery="1"/>
    <l element="171" howMuch="0" produceEvery="1"/>
    <l element="170" howMuch="0" produceEvery="1"/>
    </products>
    <difficulty skill="Industry" level="1"/>
    <desc/>
  </product>

  <!-- Compost: Grains and Hops => Water + Raw Chemicals + Carbon -->
  <product eid="{id}3378" type="Process" interactive="true" startOnly="false" itemScrapper="false" smelter="false" scrapper="false" composter="false" processValue="100" customPriority="-1">
    <needs>
    <l element="3378" howMuch="1" consumeEvery="1"/>
    </needs>
    <products>
    <l element="16" howMuch="0" produceEvery="1"/>
    <l element="171" howMuch="0" produceEvery="1"/>
    <l element="170" howMuch="0" produceEvery="1"/>
    </products>
    <difficulty skill="Industry" level="1"/>
    <desc/>
  </product>
  
  </Product>
</data>
```
