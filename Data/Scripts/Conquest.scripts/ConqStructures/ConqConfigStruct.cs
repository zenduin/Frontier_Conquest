namespace Conquest.scripts.ConqStructures
{
    using System.Collections.Generic;
    using System.Xml.Serialization;
	using System;
	using VRageMath;

    [XmlType("ConqConfig")]
    public class ConqConfigStruct
    {	
        public List<ConqBody> Bodies { get; set; }

		public int PlanetPoints { get; set; }		// per minute
		
		public int MoonPoints { get; set; }			// per minute
		
		public int AsteroidPoints { get; set; }		// per minute
		
		public int PlanetSize { get; set; } 		// Under this size it is a moon
		
		public int BeaconDistance { get; set; } 	// How far beacons need to broadcast
				
		public int UpdateFrequency { get; set; }

        public bool Antenna { get; set; }
		
		public bool AssemblerReq { get; set; }
		
		public bool RefineryReq { get; set; }
		
		public bool CargoReq { get; set; }
		
		public bool StaticReq { get; set; }
		
		public bool EnableLcds { get; set; }

        public bool AreaReq { get; set; }       // Enable exclusive contested areas: Area Conquest

        public bool Persistent { get; set; }

        public bool Upgrades { get; set; }
        
        public bool Reward { get; set; }
        
        public int MaxBonusTime { get; set; }
        
        public int MaxBonusMod { get; set; }	

        public bool Debug { get; set; }
    }
}
