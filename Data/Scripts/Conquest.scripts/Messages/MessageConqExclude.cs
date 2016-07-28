namespace Conquest.scripts.Messages
{
    using System;
	using System.Linq;
    using System.Text;
    using Conquest.scripts.ConqStructures;
	using Conquest.scripts;
    using ProtoBuf;
    using Sandbox.ModAPI;
	using VRageMath;

    /// <summary>
    /// this is to do the actual work of setting new prices and stock levels.
    /// </summary>
    [ProtoContract]
    public class MessageConqExclude : MessageBase
    {
        #region properties

		[ProtoMember(1)]
        public ExcludeManage CommandType;
        /// <summary>
        /// The key config item to set.
        /// </summary>
        [ProtoMember(2)]
        public string ZoneName;

        /// <summary>
        /// The value to set the config item to.
        /// </summary>
        [ProtoMember(3)]
        public double X;
		
		/// <summary>
        /// The value to set the config item to.
        /// </summary>
        [ProtoMember(4)]
        public double Y;
		
		/// <summary>
        /// The value to set the config item to.
        /// </summary>
        [ProtoMember(5)]
        public double Z;
		
		/// <summary>
        /// The value to set the config item to.
        /// </summary>
        [ProtoMember(6)]
        public int Size;

        #endregion
	
		public static void SendAddMessage(string zoneName, double x, double y, double z, int size)
        {
            ConnectionHelper.SendMessageToServer(new MessageConqExclude { CommandType = ExcludeManage.Add, ZoneName = zoneName, X = x, Y = y, Z = z, Size = size});
        }

        public static void SendRemoveMessage(string zoneName)
        {
            ConnectionHelper.SendMessageToServer(new MessageConqExclude { CommandType = ExcludeManage.Remove, ZoneName = zoneName });
        }
		
		public static void SendListMessage()
        {
            ConnectionHelper.SendMessageToServer(new MessageConqExclude { CommandType = ExcludeManage.List });
        }

        public override void ProcessClient()
        {
            // never processed on client
        }

        public override void ProcessServer()
        {
            if (ConquestScript.Instance.DataLock.TryAcquireExclusive())
            {
                try
                {
                    var player = MyAPIGateway.Players.FindPlayerBySteamId(SenderSteamId);
                    if (player == null || !player.IsAdmin()) // hold on there, are we an admin first?
                        return;
                    switch (CommandType)
                    {
                        case ExcludeManage.Add:
                            {
                                if (string.IsNullOrWhiteSpace(ZoneName) || ZoneName == "*")
                                {
                                    MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Exclusion Add", "Invalid name supplied for the zone name.");
                                    return;
                                }

                                var checkZone = ConquestScript.Instance.Data.ConquestExclusions.FirstOrDefault(m => m.DisplayName.Equals(ZoneName, StringComparison.InvariantCultureIgnoreCase));
                                if (checkZone != null)
                                {
                                    MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Exclusion Add", "An exclusion zone of name '{0}' already exists.", checkZone.DisplayName);
                                    return;
                                }

                                // TODO: market inside market check?

                                ConquestExclusionZone NewZone = new ConquestExclusionZone();
                                NewZone.DisplayName = ZoneName;
                                NewZone.Position = new Vector3D(X, Y, Z);
                                NewZone.Radius = Size;
                                ConquestScript.Instance.Data.ConquestExclusions.Add(NewZone);
                                MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Exclusion Add", "A new conquest exclusion zone called '{0}' has been created.", ZoneName);
                            }
                            break;
                        case ExcludeManage.Remove:
                            {
                                var zone = ConquestScript.Instance.Data.ConquestExclusions.FirstOrDefault(m => m.DisplayName.Equals(ZoneName, StringComparison.InvariantCultureIgnoreCase));
                                if (zone == null)
                                {
                                    var zones = ConquestScript.Instance.Data.ConquestExclusions.Where(m => m.DisplayName.IndexOf(ZoneName, StringComparison.InvariantCultureIgnoreCase) >= 0).ToArray();
                                    if (zones.Length == 0)
                                    {
                                        MessageClientTextMessage.SendMessage(SenderSteamId, "Conqueset Exclusion Remove", "The specified zone name could not be found.");
                                        return;
                                    }
                                    if (zones.Length > 1)
                                    {
                                        var str = new StringBuilder();
                                        str.Append("The specified zone name could not be found.\r\n    Which did you mean?\r\n");
                                        foreach (var m in zones)
                                            str.AppendLine(m.DisplayName);
                                        MessageClientDialogMessage.SendMessage(SenderSteamId, "Conquest Exclusion Remove", " ", str.ToString());
                                        return;
                                    }
                                    zone = zones[0];
                                }

                                ConquestScript.Instance.Data.ConquestExclusions.Remove(zone);
                                MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Exclusion Remove", "The zone '{0}' has been removed.", zone.DisplayName);
                            }
                            break;
                        case ExcludeManage.List:
                            {
                                var str = new StringBuilder();
                                if (ConquestScript.Instance.Data.ConquestExclusions.Count > 0)
                                {
                                    foreach (var zone in ConquestScript.Instance.Data.ConquestExclusions)
                                    {
                                        str.AppendFormat("Zone: {0}\r\n", zone.DisplayName);
                                        str.AppendFormat("  Center Position=X:{0:N} | Y:{1:N} | Z:{2:N} Radius={3:N}m\r\n\r\n", zone.Position.X.ToString(), zone.Position.Y.ToString(), zone.Position.Z.ToString(), zone.Radius.ToString());
                                    }
                                    MessageClientDialogMessage.SendMessage(SenderSteamId, "Conquest Exclusion Zone List", " ", str.ToString());
                                }
                                else
                                {
                                    MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Exclusion List", "List is currently empty");
                                }
                            }
                            break;
                    }
                    ConquestScript.Instance.DataLock.ReleaseExclusive();
                }
                catch (Exception ex)
                {
                    ConquestScript.Instance.ServerLogger.WriteException(ex);
                    MyAPIGateway.Utilities.ShowMessage("Error", "An exception has been logged in the file:" + ConquestScript.Instance.ServerLogger.LogFileName);
                    ConquestScript.Instance.DataLock.ReleaseExclusive();
                }                    
            }
            else
            {
                MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Exclude", "Server busy, try again later.");
            }
        }
    }
}
