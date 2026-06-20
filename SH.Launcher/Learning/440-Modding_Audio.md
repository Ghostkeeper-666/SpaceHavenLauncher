# XML Modding: Audio

Audio is added to the game using **library/audio\*.xml** files
- if you use and existing audio name or an existing audio ID, the audio will be replaced
- you can use the AUTO-ID system to avoid audio ID collisions
- your audio filename and the XML audio entry name MUST have at east one underscore character '_'
- otherwise the game's sound or music system gets muted!
- place tour new audio files directly under the mod's **audio** subdirectory

Example of new audio entries in a **library/audio\*.xml** file:

```
<audio>
	<a n="facility_NewToilet" at="Sound" id="{id}0010" ogg="library/sound/ogg/facility_NewToilet.ogg" oggl="0.99" st="Game" vo="90"/>
	<a n="facility_NewWaterPurifier" at="Sound" id="{id}0020" ogg="library/sound/ogg/facility_NewWaterPurifier.ogg" oggl="2.22" st="Game" vo="70"/>
	<a n="facility_NewAssembler" at="Sound" id="{id}0030" ogg="library/sound/ogg/facility_NewAssembler.ogg" oggl="1.20" st="Game" vo="90"/>
</audio>
```

All these OGG sound files are directly stored under the mod's **audio** subdirectory:

```
MyNewFacility
└── audio
    ├── facility_NewToilet.ogg
    ├── facility_NewWaterPurifier.ogg
    └── facility_NewAssembler.ogg
```

# Important attributes

- Attribute **at** is **audio type**: either "Sound" or "Music"
- Attribute **st** is **sound type**: either "Game" for sound effects or "UI" for user interface sounds
- Attribute **vo** is **audio volume** and must be a value in range (0 - 100)
- Attribute **id** should be unique
- Unless you want to override audio (in this case also use the same audio name/filename)

If the audio encoding is **MP3**:
- Attribute **mp3** points to the target location in the **spacehaven.jar** file
- Attribute **mp3l** is the exact audio length in seconds

If the audio encoding is **OGG**:
- Attribute **ogg** points to the target location in the **spacehaven.jar** file
- Attribute **oggl** is the exact audio length in seconds

**That's all !**
