# XML mod Tutorial: New Jukebox

In XML mods, you basically use XML files to change the game's XML files

Space Haven Launcher currently accepts 2 types of XML modding:
- Merge **XML Library Files**: these add or override a big chunk of XML in Space Haven XML files
- Run **XML Patch Operations**: these add, modify, or delete content in Space Haven XML files

The modding toolset performs the necessary actions!
- This tutorial works on Space Haven Launcher, as well as other mods developed so far
- The old python mod loader program has limitations and many missing features
- You get way more feedback from the Space Haven Launcher mod build system

If you want get all tutorial mod files, you can get them from **Space Haven Launcher**'s Library subdirectory

**PS**: Checking the previous XML modding topics is strongly recommended

**Let's start!**


# (1) Create mod directory

Create the directory "XmlTutorial" anywhere on your computer
- This is referred as the root directory of your mod
- Now add the subdirectories: "audio", "library", "patches", "textures"

*Use exactly these directory names*

```
└── XmlModTutorial
    ├── audio
    ├── library
    ├── patches
    └── textures
```


# (2) info.xml

This file is always required, for all mod types.

File: [XmlModTutorial/info.xml](Learning/XmlModTutorial/info.xml)

```
<mod>
  <name>XML Mod Tutorial</name> <!-- Unique name -->
  <author>Myself</author>
  <version>1.0.0</version>
  <modid>0</modid> <!-- AUTO ID mode -->
  <description>This mod adds a new jukebox.</description>
  <spacehaven op=">=" v="1.0.0" />
  <foregroundcolor>#9F9FFF</foregroundcolor> <!-- Purple -->
  <config>
    <var default="1" value="2" name="{TechBlocksCost}">New Jukebox: Tech Blocks Cost</var>
    <var default="100" value="50" name="{NewJukeboxVolume}">New Jukebox: Audio Volume</var>
  </config>
</mod>
```


# (3) description.md

This file adds a better description on Space Haven Launcher.

Otherwise the plain text description in the info.xml will be used.

File: [XmlModTutorial/description.md](Learning/XmlModTutorial/description.md)

```
# Hello Spacefarer!

This is mod for [Space Haven](https://bugbyte.fi/spacehaven)

It is fully documented in the [Learning Computer](tab://LearningComputer) of Space Haven Launcher

Cheers,
**Ghostkeeper**
```


# (1) backgroung.jpg

Choose a large background image
- Use AI to upscale it
- Place it in the root directory of your mod

File: [XmlModTutorial/background.jpg](Learning/XmlModTutorial/background.jpg)


# (2) Library XML files

These files basically follow exactly the XML structure of the target file to be modified.


## (2.1) haven_NewJukebox.xml

Let's add a new facility to the **haven** file: a **New Jukebox** !

Notice how these XML blocks are basically a copy from the original Jukebox Facility with a few changes:
- We always use a 4 digits identification after the **AUTO ID** variable prefix: `{id}XXXX`
- We prefix with **AUTO ID** here: `mid="{id}2418"`, `mid="{id}1526"`, `tid="{id}0906"`, `tid="{id}0906"`, `auid="{id}4495"`
- We change the animation IDs: `aid="newjukebox1B"`, `aid="newjukebox1F"`, `aid="build_icon_newjukebox"`, 

File: [XmlModTutorial/library/haven_NewJukebox.xml](Learning/XmlModTutorial/library/haven_NewJukebox.xml)

```
<data>
  <Element>

    <!-- New Jukebox: Lights -->
    <me mid="{id}2418" ec="4" costGroup="0" offsetRot="R0" nonSymmetrical="false" alternateY="false">
      <data>
        <l type="Light" eid="4" gridOffX="0" gridOffY="0" layer="0">
          <element id="0" playAnimation="false" walkGridCost="0" lightValue="None">
            <definedStates>
              <l state="Standby">
                <light skipCenter="false" powerTier="Decorative" color="214013ff">
                  <ray spread="125.0" smoothEdges="true" angle="270" rotate="false" rotatePerSec="0.0" distance="4" brightness="225"/>
                </light>
              </l>
              <l state="NoPowerOrError"/>
              <l state="InUse">
                <light skipCenter="false" powerTier="Decorative" color="2e591bff">
                  <ray spread="125.0" smoothEdges="true" angle="270" rotate="false" rotatePerSec="0.0" distance="4" brightness="190"/>
                </light>
              </l>
            </definedStates>
            <stateWatchdog/>
          </element>
        </l>
      </data>
      <linked/>
      <events/>
    </me>

    <!-- New Jukebox: Facility -->
    <me mid="{id}1526" ec="3" costGroup="949" offsetRot="R0" nonSymmetrical="false" alternateY="false">
      <data>
        <l type="Object" eid="2" gridOffX="0" gridOffY="0" layer="0">
          <element id="0" renderOnFloor="false" renderOnFloorPartially="false" noMoveDismantle="false" cannotBeSelected="false" walkGridCost="255" lightValue="HighMinus">
            <definedStates>
              <l state="NoPowerOrError" playAnimation="false">
                <r0 aid="newjukebox1F" flx="false"/>
                <r90 aid="null" flx="false"/>
                <r180 aid="newjukebox1B" flx="false"/>
                <r270 aid="null" flx="false"/>
              </l>
              <l state="Standby" playAnimation="false">
                <r0 aid="newjukebox1F" flx="false"/>
                <r90 aid="null" flx="false"/>
                <r180 aid="newjukebox1B" flx="false"/>
                <r270 aid="null" flx="false"/>
              </l>
              <l state="InUse" playAnimation="true">
                <r0 aid="newjukebox1F" flx="false"/>
                <r90 aid="null" flx="false"/>
                <r180 aid="newjukebox1B" flx="false"/>
                <r270 aid="null" flx="false"/>
              </l>
            </definedStates>
            <additionaFullLit x="0" y="0" mirX="0" mirY="0" atNoPower="false" atStandby="true" atInuse="true">
              <r0 aid="jukebox1FlightPlay1" flx="false"/>
              <r90 aid="null" flx="false"/>
              <r180 aid="null" flx="false"/>
              <r270 aid="null" flx="false"/>
            </additionaFullLit>
            <features addFacilityIcons="true" noPressureDmg="false" normalState="true" produceInNormal="false" inUseState="true">
              <entertainment>
                <juke/>
                <singleUserTasks>
                  <l task="StandWork" usage="Both" gridOffX="0" gridOffY="-1" dir="D7" setStateInUse="false">
                    <activate activateOffX="0" activateOffY="-1"/>
                    <r0offset charOffX="-14" charOffY="6"/>
                    <r180offset charOffX="12" charOffY="-6"/>
                  </l>
                </singleUserTasks>
              </entertainment>
              <environment repairBlockFail="0.1" coldOffTempC="-100" coldSlowTempC="0" hotSlowTempC="60" hotOffTempC="200" coldTempSlowdown="0.8" hotTempSlowdown="1.0" cyclesForDmg="100" cyclesDmgChance="0.1" hazTimeDmg="360" hazDmgChance="0.5" waterTimeDmg="360" waterDmgChance="0.1" tempTimeDmg="360" tempDmgChance="0.1"/>
              <noiseAndComfort onlyWhenInuse="false" category="Music">
                <constantSound>
                  <atInUse auid="{id}4495"/>
                </constantSound>
                <radiusBeauty roomDrop="20" work="0" sleep="-20" leisure="20" radius="8"/>
              </noiseAndComfort>
            </features>
            <commonPower powerCategory="Facilities"/>
          </element>
        </l>
      </data>
      <linked>
        <l id="{id}2418" eid="3" gridOffX="0" gridOffY="0" rot="R0" layer="0" damageGroup="0"/>
      </linked>
      <events/>
      <objectInfo placeInMenu="11" disableRotation="false" disableBuildTest="false" buildInstant="false" disableAirlockTest="false" debugOnly="false" canDismantleOnly="false" randomRot="false" viewMode="Normal" systemPoints="0" canBuildAt="All" outdoorObject="false" indoorObject="true">
        <cu>
          <groups>
            <l rep="949" buildTools="30">
              <customPrice>
                <l elementId="930" howMuch="1"/>
              </customPrice>
            </l>
          </groups>
        </cu>
        <name tid="{id}0906"/>
        <desc tid="{id}0907"/>
        <guiIcon aid="build_icon_newjukebox"/>
        <subCat id="1507"/>
        <difficultyLevel skill="Construction" level="2"/>
        <restrictions>
          <l type="Floor" gridX="0" gridY="-1" sizeX="1" sizeY="1"/>
        </restrictions>
      </objectInfo>
    </me>

  </Element>
</data>
```


## (2.2) texts_NewJukebox.xml

Now we will add a new name and description to the facility:
- Since we are lazy, we simply use AI to translate the text to all languages
- We prefix with the AUTO ID here: `id="{id}0906"` and `id="{id}0906"`

File: [XmlModTutorial/library/texts_NewJukebox.xml](Learning/XmlModTutorial/library/texts_NewJukebox.xml)

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
  <t id="{id}0907" pid="905">
    <EN>This New Jukebox plays a huge selection of the all-time favorite human songs and lifts up the mood.</EN>
    <ES>Esta nueva gramola es capaz de reproducir una gran selección de clásicos humanos que levantan el ánimo como nada.</ES>
    <DE>Diese neue Musikbox spielt eine breite Auswahl der größten (Erden-)Hits ab und hebt so an Bord die Stimmung.</DE>
    <PL>Ta nowa szafa grająca z ogromnym katalogiem ziemskich hitów wszech czasów poprawia nastrój.</PL>
    <KO>이 새 주크박스는 시대를 초월하여 사랑받는 인류의 노래를 재생하여 분위기를 띄웁니다.</KO>
    <IT>Questo nuovo jukebox riproduce un'enorme varietà di grandi successi amati dagli umani e migliora l'umore.</IT>
    <CN>这台新点唱机可播放巨量的各时代经典人类歌曲，可以改善船员的心情。</CN>
    <FR>Ce nouveau juke-box comprend une sélection des meilleurs morceaux de l'histoire de l'humanité, et remontera le moral des troupes.</FR>
    <CS>Tento nový jukebox hraje bohatý výběr těch nejoblíbenějších hitů napříč lidskými dějinami. No a díky tomu náležitě zvedá náladu.</CS>
    <PTBR>Este novo tocador de música reproduz canções humanas de sucesso de várias épocas. Ele ajuda a dar uma animada.</PTBR>
    <RU>Этот новый музыкальный автомат с большой коллекцией популярных песен всех времен поднимет настроение экипажа.</RU>
    <JA>人間たちが好む往年の名曲の数々を選りすぐって収録した新しいジュークボックス。士気を高めることができる。</JA>
    <TR>Bu yeni müzik kutusu, tüm zamanların en sevilen insan şarkılarından oluşan geniş bir seçkiyi çalar ve ruh hâlini yükseltir.</TR>
  </t>
</t>
```


## (2.3) audio_NewJukebox.xml

Let's add a new sound effect for this jukebox:
- We prefix with the AUTO ID here: `id="{id}4495"`
- This is the length in seconds of the new audio file: `mp3l="7.440"`
- This is the volume: `vo="60"`
- Put the audio file [facility_newjukebox.ogg](Learning/XmlModTutorial/audio/facility_newjukebox.ogg) in the "audio" directory of your mod

File: [XmlModTutorial/library/audio_NewJukebox.xml](Learning/XmlModTutorial/library/audio_NewJukebox.xml)

```
<audio>
  <a n="facility_newjukebox" at="Sound" id="{id}4495" ogg="library/sound/ogg/facility_newjukebox.ogg" oggl="7.44" st="Game" vo="100"/>
</audio>
```


## (2.4) animations_NewJukebox

Let's add a new facility icon, and 2 new textures for the new jukebox:
- Notice animation names: `n="newjukebox1B"`, `n="newjukebox1F"`, `n="build_icon_newjukebox"`
- Notice texture file references like: `filename="newjukebox142.png"`

Put the texture file in the "textures" directory of your mod:
- [newjukebox142.png](Learning/XmlModTutorial/textures/newjukebox142.png)
- [newjukebox143.png](Learning/XmlModTutorial/textures/newjukebox143.png)
- [newjukebox700.png](Learning/XmlModTutorial/textures/newjukebox700.png)
- [newjukebox1064.png](Learning/XmlModTutorial/textures/newjukebox1064.png)
- [newjukebox5438.png](Learning/XmlModTutorial/textures/newjukebox5438.png)

File: [XmlModTutorial/library/animations_NewJukebox.xml](Learning/XmlModTutorial/library/animations_NewJukebox.xml)

```
<AllAnimations>
  <animations>

    <ba n="newjukebox1F" f="30" ks="1" id="4157">
      <bones>
        <b id="0">
          <pos>
            <p f="1" x="0" y="0" sx="1" sy="1" r="0" col="-1"/>
          </pos>
        </b>
      </bones>
      <items>
        <assetPos vf="1:0" bi="0" x="0" y="0" sx="1" sy="1" r="0" filename="newjukebox700.png"/>
        <assetPos vf="1:1" bi="0" x="0" y="0" sx="1" sy="1" r="0" filename="newjukebox700.png"/>
        <assetPos vf="1:1" bi="0" x="-0" y="22" sx="1" sy="1" r="0" filename="newjukebox142.png"/>
      </items>
    </ba>

    <ba n="newjukebox1B" f="30" ks="1" id="4158">
      <bones>
        <b id="0">
          <pos>
            <p f="1" x="0" y="0" sx="1" sy="1" r="0" col="-1"/>
          </pos>
        </b>
      </bones>
      <items>
        <assetPos vf="1:1" bi="0" x="0" y="0" sx="1" sy="1" r="0" filename="newjukebox700.png"/>
        <assetPos vf="1:0" bi="0" x="7" y="-4" sx="1" sy="1" r="0" filename="newjukebox1064.png"/>
        <assetPos vf="1:1" bi="0" x="1" y="22" sx="1" sy="1" r="0" filename="newjukebox143.png"/>
      </items>
    </ba>

    <ba n="build_icon_newjukebox" f="30" ks="1" id="4435">
      <bones>
        <b id="0">
          <pos>
            <p f="1" x="0" y="0" sx="1" sy="1" r="0" col="-1"/>
          </pos>
        </b>
      </bones>
      <items>
        <assetPos vf="1:1" bi="0" x="0" y="0" sx="1" sy="1" r="0" filename="newjukebox5438.png"/>
      </items>
    </ba>

  </animations>
</AllAnimations>
```


# (3) Patch XML files

Patch XML files are different, they have their own syntax to define **PATCH** operations.


## (3.1) haven_patch_NewJukebox.xml

Let's make the amount of building material configurable using a mod variable:
- The mod variable is `{TechBlocksCost}`, the one we defined in **info.xml**
- Here we use the **AttributeSet** operation to set the XML attribute value targeted by **xpath** and **attribute**

Let's also add the **New Jukebox** to same technology of the original **Jukebox**
- Here we use the **Add** operation to add a new child node to node targeted by **xpath**

File: [XmlModTutorial/patches/haven_patch_NewJukebox.xml](Learning/XmlModTutorial/patches/haven_patch_NewJukebox.xml)

```
<Patch>
	<Operation Class="AttributeSet">
		<xpath>/data/Element/me[@mid="{id}1526"]/objectInfo/cu/groups/l/customPrice/l[@elementId="930"]</xpath>
		<attribute>howMuch</attribute>
		<value>{TechBlocksCost}</value>
	</Operation>

	<Operation Class="Add">
		<xpath>/data/Tech/tech[@id="2564"]/unlocks</xpath>
		<value>
			<l type="Building" buildingId="{id}1526"/>
		</value>
	</Operation>
</Patch>
```


## (3.1) audio_patch_NewJukebox.xml

Let's control the volume of the audio used by New Jukebox using a mod variable:
- The mod variable is `{NewJukeboxVolume}`, which is defined in **info.xml**
- We use the **AttributeSet** patch operation to set the XML attribute value targeted by **xpath** and **attribute**

File: [XmlModTutorial/patches/audio_patch_NewJukebox.xml](Learning/XmlModTutorial/patches/audio_patch_NewJukebox.xml)

```
<Patch>
	<Operation Class="AttributeSet">
		<xpath>/audio/a[@n="facility_newjukebox"]</xpath>
		<attribute>vo</attribute>
		<value>{NewJukeboxVolume}</value>
	</Operation>
</Patch>
```


# (4) Deploy your mod

Space Haven defines a classic "mods" folder location. Copy your root mod folder as a child of the "mods" folder:
- The **Classic Mods Dir** is defined in [System Core](tab://SystemCore)

```
mods
└── XmlModTutorial
    ├── audio
    ├── library
    ├── patches
    └── textures
```


# (5) To Space Haven!

Your mod should now be visible on Space Haven Launcher
- Go to [Navigation Console](tab://NavigationConsole) and click on the **(M) Lever** to run the modded game!


**Congratulations, you have finished the tutorial!**

