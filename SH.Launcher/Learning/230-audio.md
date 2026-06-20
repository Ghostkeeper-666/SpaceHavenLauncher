# Space Haven Basics: Audio

Audio content is relatively simply to understand:
- It can be encoded either in **mp3** or **ogg** file format
- It is categorized by the game as either **audio type** "Sound" or type "Music"
- Audio information contains audio length (in seconds) and volume (0 - 100)

# The "audio" XML file

Each audio metadata is stored in the **audio XML file**:

```
<audio>
...
  <a n="footsteps_metallic_floor_01" at="Sound" id="4317" mp3="library/sound/mp3/footsteps_metallic_floor_01.mp3" mp3l="0.4" st="Game" vo="50"/>
  <a n="footsteps_metallic_floor_02" at="Sound" id="4318" mp3="library/sound/mp3/footsteps_metallic_floor_02.mp3" mp3l="0.4" st="Game" vo="50"/>
...
  <a n="refinery_operating_02" at="Sound" id="4352" ogg="library/sound/ogg/refinery_operating_02.ogg" oggl="3.875" st="Game" vo="65"/>
  <a n="refinery_operating_pistonlike" at="Sound" id="4353" ogg="library/sound/ogg/refinery_operating_pistonlike.ogg" oggl="4.0" st="Game" vo="65"/>
...
	<a n="To_The_Moon_Synth" at="Music" id="1131" ogg="library/music/ogg/to_the_moon_synth.ogg" oggl="43.0" vo="70"/>
...
</audio>
```

### The &lt;a&gt; node

- Attribute **id** uniquely identifies audio files - it is used by the **haven** file
- Attribute **n** means audio **name**, and should be unique
- Attribute **at** means audio type: either "Sound" or "Music"
- Attribute **ogg** or **mp3** must match the **audio format** and define the **path to the audio file** within the "spacehaven.jar" file
- Attribute **oggl** or **mp3l** must match the **audio format** and define the **length in seconds** of the audio file
- Attribute **st** means **sound type** and is applicable only when audio type is "Sound"

# Important Notes

- The **haven** file references **audio** by its **id** attribute
- **Sound Type**: the value **Game** is used for sound effects, and **UI** is used for user interface sounds
- The audio **filename** and it's XML **name** MUST contain at least one **underscore character** e.g. `facilities_MyNewFacility.mp3`
- **Otherwise the game's audio system stops working!**
