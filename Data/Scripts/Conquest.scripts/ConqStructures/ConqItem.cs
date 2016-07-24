namespace Conquest.scripts.ConqStructures
{
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using System;

    [XmlType("ConqItem")]
    public class ConqItem
    {
        public string TypeId { get; set; }

        public string SubTypeName { get; set; }

        public int Amount { get; set; }
    }
}