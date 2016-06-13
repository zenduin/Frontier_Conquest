namespace Conquest.scripts.ConqStructures
{
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using VRageMath;
	using System;
		
	[XmlType("ConquestExclusionZone")]
    public class ConquestExclusionZone
    {
		
		public String DisplayName { get; set; }
				
		public Vector3D Position { get; set; }
		
		public int Radius { get; set; }
		
    }
	
}
