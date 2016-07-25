namespace Conquest.scripts.Messages
{
    using System;
    using System.Collections.Generic;
    using Conquest.scripts;
    using ProtoBuf;
    using Sandbox.ModAPI;
    using VRageMath;
    using Conquest.scripts.ConqStructures;
    using VRage.Game.ModAPI;
    using VRage.ModAPI;
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
                            ConquestScript.Instance.DataLock.ReleaseExclusive();
                            return;
						}
					}
                    bool boAR = false;
                    if (ConquestScript.Instance.Config.AreaReq == true)
                    {
                        foreach (ConquestAreaZone Zone in ConquestScript.Instance.Data.ConquestAreas)
                        {

                            if ((Vector3D.Distance(TempPosition, Zone.Position) < Zone.Radius))
                            {
                                boAR = true;
                                break;
                            }
                        }

                        if (boAR == false)
                        {

                            MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Base",
                            string.Format("This grid Is NOT a valid Conquest Base as it is NOT within a Conquest Area"));
                            ConquestScript.Instance.DataLock.ReleaseExclusive();
                            return;
                        }
                    }
                    ConquestScript.Instance.DataLock.ReleaseExclusive();
					BoundingSphereD Sphere = new BoundingSphereD(TempPosition, ConquestScript.Instance.Config.BaseDistance);
					List<IMyEntity> Entities = MyAPIGateway.Entities.GetEntitiesInSphere(ref Sphere);
					List<IMyEntity> Grids = new List<IMyEntity>();
					foreach (IMyEntity Entity in Entities)
					{
						if (Entity is IMyCubeGrid)
						{
							Grids.Add(Entity);
						}
					}
                    string Reason = "";
                    ConquestGrid ConqGrid = new ConquestGrid(SelectedGrid);
					if (ConqGrid.IsValid)
					{
						//MyAPIGateway.Utilities.ShowMessage("ConquestBase", SelectedBlock.CubeGrid.DisplayName + " Is a valid Conquest Base!");
						MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Base",
						string.Format(" {0} Is a valid Conquest Base!", SelectedGrid.DisplayName));
					}
					else
					{
                        foreach (string reason in ConqGrid.Reasons)
                        {
                            Reason += reason;
                        }
                        //MyAPIGateway.Utilities.ShowMessage("ConquestBase", SelectedBlock.CubeGrid.DisplayName + " Is NOT a valid Conquest Base! Reason(s):" + Reason);
                        MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Base",
						string.Format(" {0} Is NOT a valid Conquest Base! Reason(s): {1}", SelectedGrid.DisplayName, Reason));
                        ConquestScript.Instance.DataLock.ReleaseExclusive();
                        return;
                    }
					bool FarEnough = true;
					string OffendingBaseName2 = " ";
					foreach (IMyCubeGrid Grid in Grids)
					{
                        ConqGrid = new ConquestGrid(Grid);
						if ((Grid.EntityId != SelectedGrid.EntityId) && (ConqGrid.IsValid))
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
                        ConquestScript.Instance.DataLock.ReleaseExclusive();
                        return;
                    }
                }
				else
				{
					MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Base", "Server busy, try again later.");
                    try
                    {
                        ConquestScript.Instance.DataLock.ReleaseExclusive();
                        return;
                    }
                    catch { return; }

                }

            }
			catch (Exception ex)
			{
                ConquestScript.Instance.ClientLogger.WriteException(ex);
                // MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Base", "Error: Could not retrieve IMyCubeGrid with EntityId.");
                MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Base", "Error: " + ex.Source);
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
