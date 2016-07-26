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
    using System;
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
                        if (TempData.ConquestExclusions.Count > 0)
                        {
                            foreach (ConquestExclusionZone Zone in TempData.ConquestExclusions)
                            {
                                if ((Vector3D.Distance(TempPosition, Zone.Position) < Zone.Radius))
                                {
                                    Reasons += string.Format("\nwithin exclusion zone: {0}", Zone.DisplayName);
                                    IsValid = false;
                                }
                            }
                        }                     
                        bool boAR = false;
                        if ((ConquestScript.Instance.Config.AreaReq) && (TempData.ConquestAreas.Count > 0))
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
                                Reasons += "\n is not within a Conquest Area";
                            }
                        }
                        if (TempData.ConquestBases.Count > 0)
                        {
                            foreach (ConquestBase ConqBase in TempData.ConquestBases)
                            {
                                if (Vector3D.Distance(TempPosition, ConqBase.Position) < ConqBase.Radius)
                                {
                                    Reasons += string.Format("\nwithin broadcast range of Conquest Base: {0}", ConqBase.DisplayName);
                                    IsValid = false;
                                }
                            }
                        }  
                        if (IsValid)
                        {
                            MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Base", "This is a valid position for a conquest base");                            
                        }
                        else
                        {
                            MessageClientDialogMessage.SendMessage(SenderSteamId, "Conquest Base", "This is NOT a valid position for a conquest base", Reasons);
                        }                        
                    }
                    catch (Exception ex)
                    {
                        ConquestScript.Instance.ServerLogger.WriteException(ex);
                        MyAPIGateway.Utilities.ShowMessage("Error", "An exception has been logged in the file:" + ConquestScript.Instance.ServerLogger.LogFileName);
                    }
                });
            }
            else
            {
                MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Base", "Server busy, try again later.");
            }
        }
        public static void SendMessage( Vector3D tempPosition)
        {
            ConnectionHelper.SendMessageToServer(new MessageConqBasePos { TempPosition = tempPosition });
        }
    }
}
