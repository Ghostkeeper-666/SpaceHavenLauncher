using System.Collections.Generic;

namespace SH.Content.Xml.Haven.Elements;

public sealed class ElementXml_Data_Element_Object_Features_Storage
{
    public bool Starter { get; } // stores/starter
    public bool NotRealStorage { get; } // stores/notRealStorage
    public bool Capacity { get; } // stores/capacity
    public List<ECorpseType> AcceptedCorpseTypes { get; } // stores/capacity

    // TODO: <rules/>
}
