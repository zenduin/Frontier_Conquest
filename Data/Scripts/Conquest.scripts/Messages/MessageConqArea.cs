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
    public class MessageConqArea : MessageBase
    {
        #region properties

        [ProtoMember(1)]
        public AreaManage CommandType;
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
            ConnectionHelper.SendMessageToServer(new MessageConqArea { CommandType = AreaManage.Add, ZoneName = zoneName, X = x, Y = y, Z = z, Size = size });
        }

        public static void SendRemoveMessage(string zoneName)
        {
            ConnectionHelper.SendMessageToServer(new MessageConqArea { CommandType = AreaManage.Remove, ZoneName = zoneName });
        }

        public static void SendListMessage()
        {
            ConnectionHelper.SendMessageToServer(new MessageConqArea { CommandType = AreaManage.List });
        }

        public override void ProcessClient()
        {
            // never processed on client
        }

        public override void ProcessServer()
        {
            if (ConquestScript.Instance.DataLock.TryAcquireExclusive())
            {
                var player = MyAPIGateway.Players.FindPlayerBySteamId(SenderSteamId);
                if (player == null || !player.IsAdmin()) // hold on there, are we an admin first?
                    return;
                switch (CommandType)
                {
                    case AreaManage.Add:
                        {
                            if (string.IsNullOrWhiteSpace(ZoneName) || ZoneName == "*")
                            {
                                MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Area Add", "Invalid name supplied for the zone name.");
                                ConquestScript.Instance.DataLock.ReleaseExclusive();
                                return;
                            }

                            var checkZone = ConquestScript.Instance.Data.ConquestAreas.FirstOrDefault(m => m.DisplayName.Equals(ZoneName, StringComparison.InvariantCultureIgnoreCase));
                            if (checkZone != null)
                            {
                                MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Area Add", "A conquest Area zone of name '{0}' already exists.", checkZone.DisplayName);
                                ConquestScript.Instance.DataLock.ReleaseExclusive();
                                return;
                            }

                            ConquestAreaZone NewZone = new ConquestAreaZone();
                            NewZone.DisplayName = ZoneName;
                            NewZone.Position = new Vector3D(X, Y, Z);
                            NewZone.Radius = Size;
                            ConquestScript.Instance.Data.ConquestAreas.Add(NewZone);
                            MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Area Add", "A new conquest Area zone called '{0}' has been created.", ZoneName);
                        }
                        break;
                    case AreaManage.Remove:
                        {
                            var zone = ConquestScript.Instance.Data.ConquestAreas.FirstOrDefault(m => m.DisplayName.Equals(ZoneName, StringComparison.InvariantCultureIgnoreCase));
                            if (zone == null)
                            {
                                var zones = ConquestScript.Instance.Data.ConquestAreas.Where(m => m.DisplayName.IndexOf(ZoneName, StringComparison.InvariantCultureIgnoreCase) >= 0).ToArray();
                                if (zones.Length == 0)
                                {
                                    MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Area Remove", "The specified zone name could not be found.");
                                    ConquestScript.Instance.DataLock.ReleaseExclusive();
                                    return;
                                }
                                if (zones.Length > 1)
                                {
                                    var str = new StringBuilder();
                                    str.Append("The specified zone name could not be found.\r\n    Which did you mean?\r\n");
                                    foreach (var m in zones)
                                        str.AppendLine(m.DisplayName);
                                    MessageClientDialogMessage.SendMessage(SenderSteamId, "Conquest Area Remove", " ", str.ToString());
                                    ConquestScript.Instance.DataLock.ReleaseExclusive();
                                    return;
                                }
                                zone = zones[0];
                            }

                            ConquestScript.Instance.Data.ConquestAreas.Remove(zone);
                            MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Area Remove", "The zone '{0}' has been removed.", zone.DisplayName);
                        }
                        break;
                    case AreaManage.List:
                        {
                            var str = new StringBuilder();

                            if (ConquestScript.Instance.Data.ConquestAreas.Count > 0)
                            {
                                foreach (var zone in ConquestScript.Instance.Data.ConquestAreas)
                                {
                                    str.AppendFormat("Zone: {0}\r\n", zone.DisplayName);
                                    str.AppendFormat("  Center Position=X:{0:N} | Y:{1:N} | Z:{2:N} Radius={3:N}m\r\n\r\n", zone.Position.X.ToString(), zone.Position.Y.ToString(), zone.Position.Z.ToString(), zone.Radius.ToString());
                                }
                                MessageClientDialogMessage.SendMessage(SenderSteamId, "Conquest Area Zone List", " ", str.ToString());
                            }
                            else
                            {
                                MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Area List", "List is currently empty.");
                            }
                        }
                        break;
                }
                ConquestScript.Instance.DataLock.ReleaseExclusive();
            }
            else
            {
                MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Area", "Server busy, try again later.");
            }
        }
    }
}
