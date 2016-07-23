namespace Conquest.scripts.Messages
{
    using System.Collections.Generic;
    using Conquest.scripts;
    using ProtoBuf;
    using Sandbox.ModAPI;
    using VRageMath;
    using Conquest.scripts.ConqStructures;
    using VRage.ModAPI;
    using VRage.Game.ModAPI;
    [ProtoContract]
    public class MessageConqBasePos : MessageBase
    {
        [ProtoMember(2)]
        public Vector3D TempPosition;

        public override void ProcessClient()
        {
            // never processed on client
        }

        public override void ProcessServer()
        {
			BoundingSphereD Sphere = new BoundingSphereD(TempPosition, ConquestScript.Instance.Config.ConquerDistance);
			List<IMyEntity> Entities = MyAPIGateway.Entities.GetEntitiesInSphere(ref Sphere);
			List<IMyEntity> Grids = new List<IMyEntity>();
			foreach (IMyEntity Entity in Entities)
			{
				if (Entity is IMyCubeGrid)
				{
					Grids.Add(Entity);
				}
			}
			if (ConquestScript.Instance.DataLock.TryAcquireExclusive())
				{
					foreach (ConquestExclusionZone Zone in ConquestScript.Instance.Data.ConquestExclusions)
					{
						if ((Vector3D.Distance(TempPosition, Zone.Position) < Zone.Radius))
						{
							MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Base",
							string.Format("This is NOT a valid position to establish a conquest base as it is within safe zone: {0}", Zone.DisplayName));
                            ConquestScript.Instance.DataLock.ReleaseExclusive();
                            return;
						}
					}
					ConquestScript.Instance.DataLock.ReleaseExclusive();
				}
				else
				{
					MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Base", "Server busy, try again later.");
					return;
				}

			string OffendingBaseName = " ";
			bool FarEnough = true;
			foreach (IMyCubeGrid Grid in Grids)
			{
                ConquestGrid ConqGrid = new ConquestGrid(Grid);
				if (ConqGrid.IsValid)
				{
					FarEnough = false;
					OffendingBaseName = Grid.DisplayName;
					break;
				}
			}
			if (FarEnough)
			{
				//MyAPIGateway.Utilities.ShowMessage("ConquestBase", "This is a valid position to establish a conquest base.");
				MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Base", "This is a valid position to establish a conquest base.");
			}
			else
			{
				//MyAPIGateway.Utilities.ShowMessage("ConquestBase", "This is NOT a valid position to establish a conquest base. " + OffendingBaseName + " is too close.");
				MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Base",
				string.Format("This is NOT a valid position to establish a conquest base. {0} is too close.", OffendingBaseName));
			}				
        }

        public static void SendMessage( Vector3D tempPosition)
        {
            ConnectionHelper.SendMessageToServer(new MessageConqBasePos { TempPosition = tempPosition });
        }
    }
}
