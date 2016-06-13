namespace Conquest.scripts.ConqStructures
{
    using System.Collections.Generic;
    using System.Xml.Serialization;
	using System;

    [XmlType("ConqData")]
    public class ConqDataStruct
    {
		public DateTime LastRun { get; set; }
				
		public List<ConquestFaction> ConquestFactions;

        public List<ConquestBase> ConquestBases;
		
		public List<ConquestExclusionZone> ConquestExclusions;
    }
}
