namespace Conquest.scripts.ConqStructures
{
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using VRageMath;
	using System;

	[XmlType("ConquestFaction")]
    public class ConquestFaction
    {
		public Int64 FactionId { get; set; }
		
		public String FactionName { get; set; }
		
		public String FactionTag { get; set; }
	
		public int VictoryPoints { get; set; }
		
		public int PlanetBases { get; set; }
		
		public int MoonBases { get; set; }
		
		public int AsteroidBases { get; set; }		

    }
}
