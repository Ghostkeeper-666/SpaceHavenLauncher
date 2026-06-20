using System.Collections.Generic;

namespace SH.Content.Xml.Haven.Elements;

public sealed class ElementXml_Data_Element_Object : ElementXml_Data_Element
{
    public List<ElementXml_Data_Element_Object_DefinedState> DefinedStates { get; }
    public ElementXml_Data_Element_Object_Features Features { get; }
    public EPowerCategory CommonPowerCategory { get; } // <commonPower>




            //    <additionaFullLit x="0" y="0" mirX="0" mirY="0" atNoPower="false" atStandby="true" atInuse="true">
            //  <r0 aid="couchLight1F" flx="false"/>
            //  <r90 aid="null" flx="false"/>
            //  <r180 aid="couchLight1B" flx="false"/>
            //  <r270 aid="null" flx="false"/>
            //</additionaFullLit>

            //    <definedStates>
            //  <l state="Standby">
            //    <light skipCenter="false" powerTier="None" color="590000ff">
            //      <offset x="0" y="0"/>
            //      <flood distance="3" brightness="200"/>
            //    </light>
            //    <extraLights>
            //      <l skipCenter="false" powerTier="Decorative" color="592c00ff">
            //        <flood distance="4" brightness="355"/>
            //      </l>
            //    </extraLights>
            //  </l>
            //</definedStates>

            //<definedStates>
            //  <l state="NoPowerOrError" playAnimation="false">
            //    <r0 aid="jukebox1F" flx="false"/>
            //    <r90 aid="null" flx="false"/>
            //    <r180 aid="jukebox1B" flx="false"/>
            //    <r270 aid="null" flx="false"/>
            //  </l>
            //  <l state="Standby" playAnimation="false">
            //    <r0 aid="jukebox1F" flx="false"/>
            //    <r90 aid="null" flx="false"/>
            //    <r180 aid="jukebox1B" flx="false"/>
            //    <r270 aid="null" flx="false"/>
            //  </l>
            //  <l state="InUse" playAnimation="true">
            //    <r0 aid="jukebox1F" flx="false"/>
            //    <r90 aid="null" flx="false"/>
            //    <r180 aid="jukebox1B" flx="false"/>
            //    <r270 aid="null" flx="false"/>
            //  </l>
            //</definedStates>

            //    <definedStates>
            //  <l state = "Standby" playAnimation="false">
            //    <r0 aid = "couch2-1F" flx="false"/>
            //    <r90 aid = "couch2-2F" flx="true"/>
            //    <r180 aid = "couch2-2B" flx="false"/>
            //    <r270 aid = "couch2-1B" flx="true"/>
            //    <tasks>
            //      <l task = "Sit" usage="Both" gridOffX="0" gridOffY="0" dir="D3" setStateInUse="false">
            //        <r0offset charOffX = "0" charOffY="3"/>
            //        <r180offset charOffX = "0" charOffY="-6"/>
            //        <r90offset charOffX = "10" charOffY="-3"/>
            //        <r270offset charOffX = "-10" charOffY="0"/>
            //        <r180InFront x = "0" y="0">
            //          <front aid = "couch2-2Bm" />
            //          < fullLit aid="null"/>
            //        </r180InFront>
            //        <r270InFront x = "0" y="0">
            //          <front aid = "couch2-1Bmf" />
            //          < fullLit aid="null"/>
            //        </r270InFront>
            //      </l>
            //    </tasks>
            //  </l>
            //</definedStates>

            //    <definedStates>
            //  <l state = "Standby" playAnimation="true">
            //    <r0 aid = "toilet1F3" flx="false"/>
            //    <r90 aid = "toilet1F1" flx="false"/>
            //    <r180 aid = "toilet1B7" flx="false"/>
            //    <r270 aid = "toilet1B9" flx="false"/>
            //    <tasks>
            //      <l task = "Sit" usage="Both" gridOffX="0" gridOffY="0" dir="D3" setStateInUse="false">
            //        <activate activateOffX = "0" activateOffY="-1"/>
            //        <r0offset charOffX = "5" charOffY="-1"/>
            //        <r180offset charOffX = "-6" charOffY="8"/>
            //        <r90offset charOffX = "-1" charOffY="-2"/>
            //        <r270offset charOffX = "5" charOffY="11"/>
            //        <r180InFront x = "0" y="0">
            //          <front aid = "toilet1B7mask" />
            //          < fullLit aid="null"/>
            //        </r180InFront>
            //        <r270InFront x = "0" y="0">
            //          <front aid = "toilet1B9mask" />
            //          < fullLit aid="null"/>
            //        </r270InFront>
            //      </l>
            //    </tasks>
            //  </l>
            //</definedStates>


    //<wallMount type = "Lower" bothSides="false" drawInSmallMode="true"/>
    //<wallMount type = "Lower" bothSides="true" drawInSmallMode="true"/>

    //</features>
    //<boxHatch x = "20" y="2" boxX="20" boxY="2" in="true" out="true" flipR90="false" type2="false" wallHatch="false">
    //    <activate activateOffX = "0" activateOffY="-1"/>
    //    <r90Pos x = "-20" y="2" boxX="-20" boxY="2"/>
    //</boxHatch>
    //<boxHatch x="19" y="0" boxX="19" boxY="0" in="true" out="true" flipR90="true" type2="false" wallHatch="false">
    //    <activate activateOffX="0" activateOffY="-1"/>
    //    <r90Pos x="-13" y="-3" boxX="-13" boxY="-3"/>
    //    <customHatch aid="boxhatch"/>
    //</boxHatch>

    //<boxHatch x="0" y="0" boxX="0" boxY="0" in="false" out="false" flipR90="true" type2="true" wallHatch="false">
    //<activate activateOffX="0" activateOffY="-1"/>
    //<customHatch aid="bodyStorageDoor1"/>
    //<type2Wall aid="bodyStorage2FH1B"/>
    //<type2WallR90 aid="bodyStorage2FH3B"/>
    //</boxHatch>

    //<boxHatch x="0" y="0" boxX="4" boxY="12" in="true" out="true" flipR90="false" type2="false" wallHatch="true">
    //<activate activateOffX="0" activateOffY="-1"/>
    //<customHatch aid="asteroidContrabandStorage1open"/>
    //<type2Wall aid="asteroidContrabandStorage1bg"/>
    //<type2WallR90 aid="asteroidContrabandStorage1bg"/>
    //</boxHatch>

    //<boxHatch x = "0" y="0" boxX="0" boxY="0" in="true" out="false" flipR90="true" type2="true" wallHatch="false">
    //<activate activateOffX = "0" activateOffY="-1"/>
    //<r90Pos x = "-3" y="0" boxX="0" boxY="0"/>
    //<customHatch aid = "compostHatch1" />
    //< type2Wall aid="compost2F2Back"/>
    //<type2WallR90 aid = "compost2F1Back" />
    //</ boxHatch >

    ///<learning from = "4" to="8"/>



    //<destruction noRubble="true">
    //    <effect effect="1475"/>
    //</destruction>

    //<destruction noRubble = "true" >
    //    < effect effect="2036"/>
    //</destruction>

    //    <cryoTile>
    //    <r0 aid="hypSleepTank1Top3"/>
    //    <r90 aid="hypSleepTank1Top1"/>
    //    <r180 aid="hypSleepTank1Top7"/>
    //    <r270 aid="hypSleepTank1Top9"/>
    //    <fullLitR0 aid="hypSleepTank1Top3Lit"/>
    //    <fullLitR90 aid="empty"/>
    //    <fullLitR180 aid="hypSleepTank1Top7Lit"/>
    //    <fullLitR270 aid="empty"/>
    //</cryoTile>
}
