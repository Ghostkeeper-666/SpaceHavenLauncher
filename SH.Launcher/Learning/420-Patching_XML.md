# XML Modding: XML Patch Operations

When you need to:
- make punctual changes to the game's XML
- make changes to your own merged mod XML accordingly to mod variables
- make changes on any existing XML nodes matching a given criteria
- conditionally add, set, replace or remove XML accordingly to mod variables

Then you need to use **XML Patch Operations**

The XML Path Operation system:
- selects zero or many target nodes defined by the XPATH of the given **patch operation**
- ignores the **patch operation** if it is conditionally disabled by mod variables

The XML Patch Operation files have a special syntax which does NOT follow the syntax of the target game XML file


# Mod XML patch files

Files used for XML patch must be in the patch subdirectory of the mod

For patch operations to be performed on a given game XML file, the name of the patch file must be:

```
┌────────────────────────┬─────────────────────────────────┐
│ TARGET GAME XML FILE   │ MOD FILENAME (PATCH OPERATIONS) │
╞════════════════════════╪═════════════════════════════════╡
│ haven                  │ haven_*.xml                     │
├────────────────────────┼─────────────────────────────────┤
│ texts                  │ texts_*.xml                     │
├────────────────────────┼─────────────────────────────────┤
│ audio                  │ haven_*.xml                     │
├────────────────────────┼─────────────────────────────────┤
│ textures               │ textures_*.xml                  │
├────────────────────────┼─────────────────────────────────┤
│ animations             │ animations_*.xml                │
├────────────────────────┼─────────────────────────────────┤
│ spacehavensettings.xml │ spacehavensettings_*.xml        │
└────────────────────────┴─────────────────────────────────┘
```


# Types of Patch Operations

These patch operations are fully compatible with the old **Mod Loader**:

```
┌──────────────────┬──────────────────────────────────────────────────────────────────┬──────────────────────────────────┐
│ PATCH OPERATION  │ WHAT IT DOES                                                     │ ON ERRORS                        │
╞══════════════════╪══════════════════════════════════════════════════════════════════╪══════════════════════════════════╡
│ NodeAddFirst     │ Adds the provided XML node as first child to the target XML node │ ignore if target node not exists │
├──────────────────┼──────────────────────────────────────────────────────────────────┼──────────────────────────────────┤
│ NodeAddLast      │ Adds the provided XML node as last child to the target XML node  │ ignore if target node not exists │
├──────────────────┼──────────────────────────────────────────────────────────────────┼──────────────────────────────────┤
│ NodeInsertAfter  │ Inserts the provided XML node after the target sibling XML node  │ ignore if target node not exists │
├──────────────────┼──────────────────────────────────────────────────────────────────┼──────────────────────────────────┤
│ NodeInsertBefore │ Inserts the provided XML node before the target sibling XML node │ ignore if target node not exists │
├──────────────────┼──────────────────────────────────────────────────────────────────┼──────────────────────────────────┤
│ NodeRemove       │ Removes an existing XML node                                     │ ignore if target node not exists │
├──────────────────┼──────────────────────────────────────────────────────────────────┼──────────────────────────────────┤
│ NodeReplace      │ Replaces the target XML node with the provided XML node          │ ignore if target node not exists │
├──────────────────┼──────────────────────────────────────────────────────────────────┼──────────────────────────────────┤
│ AttributeSet     │ Sets or adds an attribute with a given value to the target node  │ create attribute if not exists   │
├──────────────────┼──────────────────────────────────────────────────────────────────┼──────────────────────────────────┤
│ AttributeAdd     │ Adds or sets the attribute with a given value to the target node │ overwrite if attribute exists    │
├──────────────────┼──────────────────────────────────────────────────────────────────┼──────────────────────────────────┤
│ AttributeRemove  │ Removes the attribute from a node                                │ ignore if attribute not exists   │
├──────────────────┼──────────────────────────────────────────────────────────────────┼──────────────────────────────────┤
│ AttributeMath    │ Recalculates the attribute value using a math operation          │ ignore if attribute not exists   │
└──────────────────┴──────────────────────────────────────────────────────────────────┴──────────────────────────────────┘
```


# Conditionally Enabling or Disabling Path Operations

Just use: `<enable>...</enable>` OR `<disable>...</disable>`

These are used as a switch to conditionally enable or disable the patch operation, accodingly to the passed content

They work for any type of patch operation and are **OPTIONAL**

Example:

```
  <Operation Class="NodeAddFirst">
    <enable>{Enable_NewStuff}</enable>
    <xpath>/data/Tech/tech[@id="3420"]/unlocks</xpath>
    <value>
      <l type="Augmentation" augmentationId="3400" enable="false"/>
    </value>
  </Operation>
```

This operation will be performed only if the value of the mod variable `{Enable_NewStuff}` evaluates to `true`

**Space Haven Launcher** can understand a wide range of values as `true` or `false`
- `0` or `0.0` is interpreted as `false`, and any other number as `true`
- `yes` is interpreted as `true` and `no` as false
- etc.

You may use the `<disable>` switch in the same way as `<enable>` - it's just the inverse logic!


# NodeAddFirst

Adds the provided XML node as first child to the target XML node (ignored if target node not exists)

- **Space Haven Launcher** aliases: `AddNodeAsFirst`, `NodeAddAsFirst`, `AddNodeFirst`, `NodeAddFirst`, `AddFirst`
- `<xpath>` (mandatory): defines the target parent node
- `<value>` (mandatory): defines the node to be added - **Space Haven Launcher** supports adding multiple nodes!

Example:

```
  <Operation Class="NodeAddFirst">
    <enable>{Enable_NewElement}</enable>
    <xpath>/data/Element</xpath>
    <value>
      <me mid="{id}2711" ec="1" costGroup="874" offsetRot="R0" nonSymmetrical="false" alternateY="false">
        <data>
          <l type="Object" eid="1" gridOffX="0" gridOffY="0" layer="0">
            <element id="0" renderOnFloor="false" renderOnFloorPartially="false">
            </element>
          </l>
        </data>
        <linked/>
        <events/>
      </me>
    </value>
  </Operation>
```


# NodeAddLast

Adds the provided XML node as last child to the target XML node (ignored if target node not exists)

- **Space Haven Launcher** aliases: `AddNodeAsLast`, `NodeAddAsLast`, `AddNodeLast`, `NodeAddLast`, `AddLast`, `AddNode`, `NodeAdd`, `Add`
- `<xpath>` (mandatory): defines the target parent node
- `<value>` (mandatory): defines the node to be added (**Space Haven Launcher** supports adding multiple nodes)

Example:

```
  <Operation Class="NodeAddLast">
    <enable>{Enable_NewTurretMode}</enable>
    <xpath>/data/Element/me[@mid="2782"]/data/l[@eid="1"]/element[@id="0"]/turret</xpath>
    <value>
      <specialMode type="MultiShot" shots="1"/>
    </value>
  </Operation>
```


# NodeInsertAfter

Inserts the provided XML node after the target sibling XML node (ignored if target node not exists)

- **Space Haven Launcher** aliases: `InsertNodeAfter`, `NodeInsertAfter`, `InsertAfter`, `InsertNode`, `NodeInsert`, `Insert`
- `<xpath>` (mandatory): defines the target sibling node
- `<value>` (mandatory): defines the node to be inserted (**Space Haven Launcher** supports inserting multiple nodes)

```
  <Operation Class="NodeInsertAfter">
    <enable>{Enable_NewAugmentation}</enable>
    <xpath>/data/Tech/tech[@id="3420"]/unlocks/l[@augmentationId="3411"]</xpath>
    <value>
      <l type="Augmentation" augmentationId="{id}3400" />
    </value>
  </Operation>
```


# NodeInsertBefore

Inserts the provided XML node before the target sibling XML node (ignored if target node not exists)

- **Space Haven Launcher** aliases: `InsertNodeBefore`, `NodeInsertBefore`, `InsertBefore`
- `<xpath>` (mandatory): defines the target sibling node
- `<value>` (mandatory): defines the node to be inserted (**Space Haven Launcher** supports inserting multiple nodes)

```
  <Operation Class="NodeInsertBefore">
    <enable>{Enable_NewAugmentation}</enable>
    <xpath>/data/Tech/tech[@id="3420"]/unlocks/l[@augmentationId="3410"]</xpath>
    <value>
      <l type="Augmentation" augmentationId="{id}3400" />
    </value>
  </Operation>
```


# NodeRemove

Removes an existing XML node (ignored if target node not exists)

- **Space Haven Launcher** aliases: `RemoveNode`, `NodeRemove`, `Remove`
- `<xpath>` (mandatory): defines the target node to be removed

```
  <Operation Class="NodeRemove">
    <enable>{Enable_NewAugmentation}</enable>
    <xpath>/data/Tech/tech[@id="3420"]/unlocks/l[@augmentationId="3410"]</xpath>
    <value>
    </value>
  </Operation>
```

The old **Mod Loader** requires you to pass `<value>` in two separate lines as in the above example

**Space Haven Launcher** does not require `<value>` at all, and it can be completely omitted


# NodeReplace

Replaces the target XML node with the provided XML node (ignored if target node not exists)

- **Space Haven Launcher** aliases: `ReplaceNode`, `NodeReplace`, `Replace`
- `<xpath>` (mandatory): defines the target node to be replaced
- `<value>` (mandatory): defines the node to be added (**Space Haven Launcher** supports adding multiple nodes)

```
  <Operation Class="NodeReplace">
    <enable>{Enable_NewAugmentation}</enable>
    <xpath>/data/Tech/tech[@id="3420"]/unlocks/l[@augmentationId="3410"]</xpath>
    <value>
      <l type="Augmentation" augmentationId="{id}3410" />
    </value>
  </Operation>
```


# AttributeSet

Sets or adds an attribute with a given value to the target node (creates attribute if not exists)

- **Space Haven Launcher** aliases: `SetAttribute`, `AttributeSet`
- `<xpath>` (mandatory): defines the target node of the attribute
- `<attribute>` (mandatory): defines the target attribute name
- `<value>` (mandatory): defines the value to be set

```
  <Operation Class="AttributeSet">
    <enable>{Enable_ScannerModifications}</enable>
    <xpath>/data/Element/me[@mid="2239"]/objectInfo</xpath>
    <attribute>systemPoints</attribute>
    <value>{Scanner_SystemPoints}</value>
  </Operation>
```


# AttributeAdd

Adds or sets the attribute with a given value to the target node (overwrites if attribute exists)

- **Space Haven Launcher** aliases: `SetAttribute`, `AttributeSet`
- `<xpath>` (mandatory): defines the target node of the attribute
- `<attribute>` (mandatory): defines the name of the attribute to be added
- `<value>` (mandatory): defines the value of the attribute to be added

```
  <Operation Class="AttributeAdd">
    <enable>{Enable_ScannerModifications}</enable>
    <xpath>/data/Element/me[@mid="2239"]/objectInfo</xpath>
    <attribute>systemPoints</attribute>
    <value>{Scanner_SystemPoints}</value>
  </Operation>
```


# AttributeRemove

Removes the attribute from a node (ignores if attribute not exists)

- **Space Haven Launcher** aliases: `RemoveAttribute`, `AttributeRemove`
- `<xpath>` (mandatory): defines the target node of the attribute
- `<attribute>` (mandatory): defines the name of the attribute to be removed

```
  <Operation Class="AttributeRemove">
    <enable>{Enable_ScannerModifications}</enable>
    <xpath>/data/Element/me[@mid="2239"]/objectInfo</xpath>
    <attribute>systemPoints</attribute>
    <value>
    </value>
  </Operation>
```

The old **Mod Loader** requires you to pass `<value>` in two separate lines as in the above example

**Space Haven Launcher** does not require `<value>` at all, and it can be completely omitted


# AttributeMath

Recalculates the attribute value using a math operation (ignores if attribute not exists)
- **Beware**: The math operation can catastrophically fail in old **Mod Loader** if any targeted XML node attribute does not contains a valid number value!
- **Space Haven Launcher** just warns you about the issue instead

- **Space Haven Launcher** aliases: `RemoveAttribute`, `AttributeRemove`
- `<xpath>` (mandatory): defines the target node of the attribute
- `<attribute>` (mandatory): defines the target attribute name - the attribute value is the **1st parameter** of the math operation
- `<value>` (mandatory): defines the value to be used as **2nd parameter** for the math operation
- Attribute `opType` of node `<value>`: defines the math operation operator

```
┌────────────────────────┬─────────────────────────────────────────────────────┐
│ opType attribute value │ Math Operation                                      │
╞════════════════════════╪═════════════════════════════════════════════════════╡
│ add                    │ new attribute value = 1st parameter + 2nd parameter │
├────────────────────────┼─────────────────────────────────────────────────────┤
│ subtract               │ new attribute value = 1st parameter + 2nd parameter │
├────────────────────────┼─────────────────────────────────────────────────────┤
│ multiply               │ new attribute value = 1st parameter + 2nd parameter │
├────────────────────────┼─────────────────────────────────────────────────────┤
│ divide                 │ new attribute value = 1st parameter + 2nd parameter │
└────────────────────────┴─────────────────────────────────────────────────────┘
```
