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
            if (ConquestScript.Instance.DataLock.TryAcquireExclusive())
            {
                ConqDataStruct TempData = ConquestScript.Instance.Data;
                ConquestScript.Instance.DataLock.ReleaseExclusive();
                MyAPIGateway.Parallel.Start(delegate ()
                {
                    try
                    {
                        bool IsValid = true;
                        string Reasons = "";
                        IMyCubeGrid SelectedGrid = MyAPIGateway.Entities.GetEntityById(SelectedGridId) as IMyCubeGrid;
                        if (SelectedGrid == null)
                        {
                            MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Base", "Error: Could not retrieve IMyCubeGrid, are you looking at a block with a terminal?");
                            return;
                        }
                        foreach (ConquestExclusionZone Zone in TempData.ConquestExclusions)
                        {
                            if ((Vector3D.Distance(SelectedGrid.GetPosition(), Zone.Position) < Zone.Radius))
                            {
                                Reasons += string.Format("\n {0} is within exclusion zone: {1}", SelectedGrid.DisplayName, Zone.DisplayName);
                                IsValid = false;
                            }
                        }
                        bool boAR = false;
                        if (ConquestScript.Instance.Config.AreaReq)
                        {
                            foreach (ConquestAreaZone Zone in TempData.ConquestAreas)
                            {

                                if ((Vector3D.Distance(TempPosition, Zone.Position) < Zone.Radius))
                                {
                                    boAR = true;
                                    break;
                                }
                            }

                            if (!boAR)
                            {
                                Reasons += string.Format("\n {0} is not within a Conquest Area", SelectedGrid.DisplayName);
                            }
                        }
                        ConquestGrid ConqGrid = new ConquestGrid(SelectedGrid);
                        foreach (ConquestBase ConqBase in TempData.ConquestBases)
                        {
                            // If this isn't the same grid...
                            if (SelectedGrid.EntityId != ConqBase.EntityId)
                            {
                                if (Vector3D.Distance(SelectedGrid.GetPosition(), ConqBase.Position) < ConqBase.Radius)
                                {
                                    Reasons += string.Format("\n {0} is within broadcast range of Conquest Base: {1}", SelectedGrid.DisplayName, ConqBase.DisplayName);
                                    IsValid = false;
                                }
                                if (Vector3D.Distance(ConqGrid.Position, ConqBase.Position) < ConqGrid.Radius)
                                {
                                    Reasons += string.Format("\n Conquest Base {0} is within broadcast range", ConqBase.DisplayName);
                                    IsValid = false;
                                }
                            }
                        }                        
                        if (ConqGrid.IsValid && IsValid)
                        {
                            //MyAPIGateway.Utilities.ShowMessage("ConquestBase", SelectedBlock.CubeGrid.DisplayName + " Is a valid Conquest Base!");
                            if (ConquestScript.Instance.Config.Reward)
                            {
                                MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Base",
                                    string.Format(" {0} Is a valid Conquest Base broadcasting at {1}m for {2}% of possible reward.", SelectedGrid.DisplayName, ConqGrid.Radius, Math.Round(ConqGrid.Radius / 500)));
                            }
                            else
                            {
                                MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Base",
                                    string.Format(" {0} Is a valid Conquest Base.", SelectedGrid.DisplayName));
                            }
                           
                        }
                        else
                        {
                            foreach (string reason in ConqGrid.Reasons)
                            {
                                Reasons += reason;
                            }
                            MessageClientDialogMessage.SendMessage(SenderSteamId, "Conquest Base", string.Format(" {0} Is NOT a valid Conquest Base", SelectedGrid.DisplayName), Reasons);
                        }
                    }
                    catch (Exception ex)
                    {
                        ConquestScript.Instance.ServerLogger.WriteException(ex);
                        MyAPIGateway.Utilities.ShowMessage("Error", "An exception has been logged in the file: " + ConquestScript.Instance.ServerLogger.LogFileName);
                    }
                });
            }
            else
            {
                MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Base", "Server busy, try again later.");
                ConquestScript.Instance.DataLock.ReleaseExclusive();
            }
        }
        
        public static void SendMessage(Int64 selectedGridId, Vector3D tempPosition)
        {
            ConnectionHelper.SendMessageToServer(new MessageConqBase { SelectedGridId = selectedGridId, TempPosition = tempPosition });
        }
    }
}
