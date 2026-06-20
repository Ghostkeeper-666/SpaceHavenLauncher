using System.Collections.Generic;

namespace SH.Content.Xml.Haven.Elements;

public sealed class ElementXml_Data_Element_Object_Features
{
    // From Properties:
    public bool AddFacilityIcons { get; }
    public bool NoPressureDmg { get; }
    public bool NormalState { get; }
    public bool ProduceInNormal { get; }
    public bool InUseState { get; }

    // From Nodes:
    public bool? StateWatchdogAutoProduce { get; } // stateWatchdog/autoproduce
    public bool? HiddenInventory { get; } // hiddenInventory, e.g. Robot Workbench, Item Workbench
    public ElementXml_Data_Element_Object_Features_NoiseAndComfort NoiseAndComfort { get; } // noiseAndComfort
    public ElementXml_Data_Element_Object_Features_Environment Environment { get; } // <environment>
    public ElementXml_Data_Element_Object_Features_ContainerBuilder ContainerBuilder { get; } // containerBuilder
    public ElementXml_Data_Element_Object_Features_SolarPower SolarPower { get; } // solar
    public ElementXml_Data_Element_Object_Features_BackupPower BackupPower { get; } // backupPower
    public ElementXml_Data_Element_Object_Features_AutoConsole AutoConsole { get; } // autoConsoles
    public ElementXml_Data_Element_Object_Features_Core Core { get; } // core
    public ElementXml_Data_Element_Object_Features_GrowHub GrowHub { get; } // growHub
    public ElementXml_Data_Element_Object_Features_Cocooon Cocoon { get; } // cocoon
    public ElementXml_Data_Element_Object_Features_Toilet Toilet { get; } // toilet
    public ElementXml_Data_Element_Object_Features_ResearchHub ResearchHub { get; } // researchHub
    public ElementXml_Data_Element_Object_Features_Examinable Examinable { get; } // examinable
    public ElementXml_Data_Element_Object_Features_ContrabandStorage ContrabandStorage { get; } // contraBandStorage
    public ElementXml_Data_Element_Object_Features_Composting Composting { get; } // compostbins
    public ElementXml_Data_Element_Object_Features_OreProcessing OreProcessing { get; } // oreProcessing
    public ElementXml_Data_Element_Object_Features_BuildTools BuildTools { get; } // buildTools
    public ElementXml_Data_Element_Object_Features_MedicalHub MedicalHub { get; } // medicalHub
    public ElementXml_Data_Element_Object_Features_PreHive PreHive { get; } // preHive
    public ElementXml_Data_Element_Object_Features_Enslaver Enslaver { get; } // enslaver
    public ElementXml_Data_Element_Object_Features_Storage Storage { get; } // stores
    public ElementXml_Data_Element_Object_Features_CryoHub CryoHub { get; } // cryoHub
    public ElementXml_Data_Element_Object_Features_CryoTank CryoTank { get; } // cryoTank
    public ElementXml_Data_Element_Object_Features_HullStabilizer HullStabilizer { get; } // stabilizer
    public ElementXml_Data_Element_Object_Features_RestPlace RestPlace { get; } // rest
    public ElementXml_Data_Element_Object_Features_EngineHub EngineHub { get; } // engineHub
    public ElementXml_Data_Element_Object_Features_RobotStation RobotStation { get; } // roboDock
    public ElementXml_Data_Element_Object_Features_Vent Vent { get; } // vent
    public ElementXml_Data_Element_Object_Features_Entertainment Entertainment { get; } // entertainment
    public List<ElementXml_Data_Element_Object_Features_Production> Production { get; } // produces








    //<entertainment>
    //<arcade/>
    //<singleUserTasks>
    //    <l task="StandWork" usage="Both" gridOffX="0" gridOffY="-1" dir="D7" setStateInUse="true">
    //    <activate activateOffX="0" activateOffY="-1"/>
    //    <r0offset charOffX="-14" charOffY="6"/>
    //    <r180offset charOffX="12" charOffY="-6"/>
    //    </l>
    //</singleUserTasks>
    //<dualUserTasks>
    //    <l task="StandWork" usage="Both" gridOffX="0" gridOffY="-1" dir="D7" setStateInUse="true">
    //    <activate activateOffX="0" activateOffY="-1"/>
    //    <r0offset charOffX="-22" charOffY="2"/>
    //    <r180offset charOffX="20" charOffY="-2"/>
    //    </l>
    //    <l task="StandWork" usage="Both" gridOffX="0" gridOffY="-1" dir="D7" setStateInUse="true">
    //    <activate activateOffX="0" activateOffY="-1"/>
    //    <r0offset charOffX="-6" charOffY="10"/>
    //    <r180offset charOffX="2" charOffY="-13"/>
    //    </l>
    //</dualUserTasks>
    //</entertainment>

    //<entertainment>
    //<juke/>
    //<singleUserTasks>
    //    <l task = "StandWork" usage="Both" gridOffX="0" gridOffY="-1" dir="D7" setStateInUse="false">
    //    <activate activateOffX = "0" activateOffY="-1"/>
    //    <r0offset charOffX = "-14" charOffY="6"/>
    //    <r180offset charOffX = "12" charOffY="-6"/>
    //    </l>
    //</singleUserTasks>
    //</entertainment>





    //<itemSpot>
    //  <corpseDisposal monster="true" human="true" android="false" robot="false"/>
    //  <itemPlace R0centerX="5" R0centerY="16" R180centerX="-5" R180centerY="16">
    //    <spots>
    //      <l x="0.0" y="0.0" dir="D1"/>
    //    </spots>
    //  </itemPlace>
    //</itemSpot>

    //// Tables
    //<itemSpot>
    //<eating R0centerX = "0" R0centerY="22" R180centerX="0" R180centerY="22">
    //    <spots>
    //    <l x = "0.4" y="0.0" dir="D9"/>
    //    <l x = "-0.4" y="0.0" dir="D1"/>
    //    <l x = "0.0" y="0.4" dir="D7"/>
    //    <l x = "0.0" y="-0.4" dir="D3"/>
    //    </spots>
    //</eating>
    //</itemSpot>
}

public class ElementXml_Data_Element_Object_Features_Entertainment
{
}