namespace Conquest.scripts.ConqStructures
{
    using System.Collections.Generic;
    using System.Xml.Serialization;
	using System;
	using VRageMath;

    [XmlType("ConqConfig")]
    public class ConqConfigStruct
    {	
		public int PlanetPoints { get; set; }		// per minute
		
		public int MoonPoints { get; set; }			// per minute
		
		public int AsteroidPoints { get; set; }		// per minute
		
		public int PlanetSize { get; set; } 		// Under this size it is a moon
		
		public int BeaconDistance { get; set; } 	// How far beacons need to broadcast
		
		public int BaseDistance { get; set; }		// How far apart conquest bases need to be
		
		public int ConquerDistance { get; set; }	// Everything in this radius counts as captured by conquest base
		
		public int UpdateFrequency { get; set; }
		
		public bool AssemblerReq { get; set; }
		
		public bool RefineryReq { get; set; }
		
		public bool CargoReq { get; set; }
		
		public bool StaticReq { get; set; }
		
		public bool EnableLcds { get; set; }
		

    }
}
