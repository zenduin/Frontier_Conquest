namespace Conquest.scripts.ConqStructures
{
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using VRageMath;
	using System;
		
	[XmlType("ConquestBase")]
    public class ConquestBase
    {
		public Int64 EntityId { get; set; }
		
		public Int64 FactionId { get; set; }
		
		public String DisplayName { get; set; }
		
        public float Radius { get; set; } // Max Beacon/Antenna Radius

		public int Planets { get; set; }
		
		public int Moons { get; set; }
				
		public int Asteroids { get; set; }

        public DateTime Established { get; set; }
		
		public Vector3D Position { get; set; }
		
		public bool IsValid { get; set; }		// true if this base counts for points next time the code runs (set to false when first discovered)

        public bool IsValidPoints { get; set; }		
    }
	
}
