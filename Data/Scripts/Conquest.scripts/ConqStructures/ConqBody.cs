namespace Conquest.scripts.ConqStructures
{
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using System;

    [XmlType("ConqBody")]
    public class ConqBody
    {
        public string Type { get; set; }

        public List<ConqItem> DefaultItems { get; set; }

        public List<ConqItem> CommonItems { get; set; }

        public List<ConqItem> RareItems { get; set; }
    }
}
