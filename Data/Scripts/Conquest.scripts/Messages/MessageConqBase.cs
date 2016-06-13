namespace Conquest.scripts.Messages
{
    using System;
    using System.Collections.Generic;	
    using ConqConfig;
    using Conquest.scripts;
    using ProtoBuf;
    using Sandbox.ModAPI;
	using VRageMath;
	using Conquest.scripts.ConqStructures;

    [ProtoContract]
    public class MessageConqBase : MessageBase
    {
        [ProtoMember(1)]
        public Int64 SelectedGridId;

        [ProtoMember(2)]
        public Vector3D TempPosition;

        public override void ProcessClient()
        {
            // never processed on client
        }

        public override void ProcessServer()
        {
			try
			{
				if (ConquestScript.Instance.DataLock.TryAcquireExclusive())
				{
					IMyCubeGrid SelectedGrid = MyAPIGateway.Entities.GetEntityById(SelectedGridId) as IMyCubeGrid;
					foreach (ConquestExclusionZone Zone in ConquestScript.Instance.Data.ConquestExclusions)
					{
						if ((Vector3D.Distance(SelectedGrid.GetPosition(), Zone.Position) < Zone.Radius))
						{
							MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Base",
							string.Format(" {0} Is NOT a valid Conquest Base as it is within safe zone: {1}", SelectedGrid.DisplayName, Zone.DisplayName));
							return;
						}
					}
					ConquestScript.Instance.DataLock.ReleaseExclusive();
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
					
					
					Vector3D ConquestPosition = new Vector3D();
					string Reason = " ";
					if (ConquestScript.Instance.IsConquestBase(SelectedGrid, ref ConquestPosition, ref Reason))
					{
						//MyAPIGateway.Utilities.ShowMessage("ConquestBase", SelectedBlock.CubeGrid.DisplayName + " Is a valid Conquest Base!");
						MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Base",
						string.Format(" {0} Is a valid Conquest Base!", SelectedGrid.DisplayName));
					}
					else
					{
						//MyAPIGateway.Utilities.ShowMessage("ConquestBase", SelectedBlock.CubeGrid.DisplayName + " Is NOT a valid Conquest Base! Reason(s):" + Reason);
						MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Base",
						string.Format(" {0} Is NOT a valid Conquest Base! Reason(s): {1}", SelectedGrid.DisplayName, Reason));
					}
					bool FarEnough = true;
					string OffendingBaseName2 = " ";
					foreach (IMyCubeGrid Grid in Grids)
					{
						Vector3D ConquestPosition2 = new Vector3D();
						string Reason2 = " ";
						if ((Grid.EntityId != SelectedGrid.EntityId) && (ConquestScript.Instance.IsConquestBase(Grid, ref ConquestPosition2, ref Reason2)))
						{
							FarEnough = false;
							OffendingBaseName2 = Grid.DisplayName;
							break;
						}
					}
					if (!FarEnough)
					{
						MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Base",
						string.Format(" {0} is too close to {1} and is not valid for points", SelectedGrid.DisplayName, OffendingBaseName2));
					}
				}
				else
				{
					MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Base", "Server busy, try again later.");
					return;
				}
				
			}
			catch
			{
				MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Base", "Error: Could not retrieve IMyCubeGrid with EntityId.");
				ConquestScript.Instance.DataLock.ReleaseExclusive();
				return;
			}			
        }

        public static void SendMessage(Int64 selectedGridId, Vector3D tempPosition)
        {
            ConnectionHelper.SendMessageToServer(new MessageConqBase { SelectedGridId = selectedGridId, TempPosition = tempPosition });
        }
    }
}
