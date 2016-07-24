namespace Conquest.scripts.ConqStructures
{
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using System;

    [XmlType("ConqPlanet")]
    public class ConqPlanet
    {
        public string SubTypeId { get; set; }

        public List<ConqItem> Items { get; set; }
    }
}
