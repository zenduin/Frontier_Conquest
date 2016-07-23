namespace Conquest.scripts.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Conquest.scripts;
    using Management;
    using ProtoBuf;
    using Sandbox.ModAPI;
    using VRage;
    using VRage.Game.ModAPI;

    /// <summary>
    /// this is to do the actual work of setting new prices and stock levels.
    /// </summary>
    [ProtoContract]
    public class MessageConqConfig : MessageBase
    {
        #region properties

        /// <summary>
        /// The key config item to set.
        /// </summary>
        [ProtoMember(1)]
        public string ConfigName;

        /// <summary>
        /// The value to set the config item to.
        /// </summary>
        [ProtoMember(2)]
        public string Value;

        #endregion

        public static void SendMessage(string configName, string value)
        {
            ConnectionHelper.SendMessageToServer(new MessageConqConfig { ConfigName = configName.ToLower(), Value = value });
        }

        public override void ProcessClient()
        {
            // never processed on client
        }

        public override void ProcessServer()
        {
            var player = MyAPIGateway.Players.FindPlayerBySteamId(SenderSteamId);

            // Only Admin can change config.
            if (!player.IsAdmin())
            {
                ConquestScript.Instance.ServerLogger.WriteWarning("A Player without Admin \"{0}\" {1} attempted to access Conquest Config.", SenderDisplayName, SenderSteamId);
                return;
            }

            MyTexts.LanguageDescription myLanguage;

            // These will match with names defined in the RegEx patterm <ConquestScript.Conquest ConfigPattern>
            switch (ConfigName)
            {
                #region planetpoints

                case "planetpoints":
                    if (string.IsNullOrEmpty(Value))
                    {
                        MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Config", "Points for planet bases: {0}", ConquestScript.Instance.Config.PlanetPoints.ToString());
                    }
                    else
                    {
                        int intTest;
                        if (int.TryParse(Value, out intTest))
                        {
							ConquestScript.Instance.Config.PlanetPoints = intTest;
							MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Config", "Points for planet bases updated to: {0}", ConquestScript.Instance.Config.PlanetPoints.ToString());
							return;
                        }
                    }
                    break;

                #endregion
				
				#region moonpoints

                case "moonpoints":
                    if (string.IsNullOrEmpty(Value))
                    {
                        MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Config", "Points for moon bases: {0}", ConquestScript.Instance.Config.MoonPoints.ToString());
                    }
                    else
                    {
                        int intTest;
                        if (int.TryParse(Value, out intTest))
                        {
							ConquestScript.Instance.Config.MoonPoints = intTest;
							MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Config", "Points for moon bases updated to: {0}", ConquestScript.Instance.Config.MoonPoints.ToString());
							return;
                        }
                    }
                    break;

                #endregion
				
				#region asteroidpoints

                case "asteroidpoints":
                    if (string.IsNullOrEmpty(Value))
                    {
                        MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Config", "Points for asteroid bases: {0}", ConquestScript.Instance.Config.AsteroidPoints.ToString());
                    }
                    else
                    {
                        int intTest;
                        if (int.TryParse(Value, out intTest))
                        {
							ConquestScript.Instance.Config.AsteroidPoints = intTest;
							MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Config", "Points for asteroid bases updated to: {0}", ConquestScript.Instance.Config.AsteroidPoints.ToString());
							return;
                        }
                    }
                    break;

                #endregion
				
				#region planetsize

                case "planetsize":
                    if (string.IsNullOrEmpty(Value))
                    {
                        MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Config", "Planet size: {0} (if it is smaller then it is a moon)", ConquestScript.Instance.Config.PlanetSize.ToString());
                    }
                    else
                    {
                        int intTest;
                        if (int.TryParse(Value, out intTest))
                        {
							if ((intTest > 120000) || (intTest < 19000))
							{
								MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Config", "Planet size must be between 19000 and 120000");
								return;
							}
							else                           
                            {
                                ConquestScript.Instance.Config.PlanetSize = intTest;
                                MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Config", "Planet size updated to: {0}", ConquestScript.Instance.Config.PlanetSize.ToString());
                                return;
                            }
                        }
                    }
                    break;

                #endregion
				
				#region beacondistance

                case "beacondistance":
                    if (string.IsNullOrEmpty(Value))
                    {
                        MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Config", "Required beacon broadcast distance: {0}", ConquestScript.Instance.Config.BeaconDistance.ToString());
                    }
                    else
                    {
                        int intTest;
                        if (int.TryParse(Value, out intTest))
                        {
                            if (intTest>50000)
							{
								MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Config", "Maximum broadcast radius for beacons is 50000m");
                                return;
							}
							else
                            {
                                ConquestScript.Instance.Config.BeaconDistance = intTest;
                                MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Config", "Required beacon broadcast distance updated to: {0}", ConquestScript.Instance.Config.BeaconDistance.ToString());
                                return;
                            }
                        }
                    }
                    break;

                #endregion

				#region basedistance

                case "basedistance":
                    if (string.IsNullOrEmpty(Value))
                    {
                        MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Config", "Minimum distance between conquest bases: {0}", ConquestScript.Instance.Config.BaseDistance.ToString());
                    }
                    else
                    {
                        int intTest;
                        if (int.TryParse(Value, out intTest))
                        {
                            if (intTest < ConquestScript.Instance.Config.ConquerDistance*2)
							{
								MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Config", "Minimum distance between conquest bases needs to be at least 2X the Conquer Distance ({0}m).", (ConquestScript.Instance.Config.ConquerDistance*2).ToString());
                                return;
							}
							else
                            {
                                ConquestScript.Instance.Config.BaseDistance = intTest;
                                MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Config", "Minimum distance between conquest bases updated to: {0}", ConquestScript.Instance.Config.BaseDistance.ToString());
                                return;
                            }
                        }
                    }
                    break;

                #endregion
				
				#region conquerdistance

                case "conquerdistance":
                    if (string.IsNullOrEmpty(Value))
                    {
                        MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Config", "Conquer distance for conquest bases: {0}", ConquestScript.Instance.Config.ConquerDistance.ToString());
                    }
                    else
                    {
                        int intTest;
                        if (int.TryParse(Value, out intTest))
                        {
                            if (intTest > ConquestScript.Instance.Config.BaseDistance/2)
							{
								MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Config", "Conquer distance needs to be less than 1/2 the minimum required distance between bases ({0}m).", (ConquestScript.Instance.Config.BaseDistance/2).ToString());
                                return;
							}
							else
                            {
                                ConquestScript.Instance.Config.ConquerDistance = intTest;
                                MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Config", "Conquer distance updated to: {0}", ConquestScript.Instance.Config.ConquerDistance.ToString());
                                return;
                            }
                        }
                    }
                    break;

                #endregion
				
				#region updatefrequency

                case "updatefrequency":
                    if (string.IsNullOrEmpty(Value))
                    {
                        MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Config", "Conquest bases are checked and points are talled every {0} minutes.", ConquestScript.Instance.Config.UpdateFrequency.ToString());
                    }
                    else
                    {
                        int intTest;
                        if (int.TryParse(Value, out intTest))
                        {
							ConquestScript.Instance.Config.UpdateFrequency = intTest;
							MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Config", "Update frequency updated to: {0} minutes. Server restart required.", intTest.ToString());
							return;
                        }
                    }
                    break;

                #endregion
								
				#region assemblerreq
				
		        case "assemblerreq":
                    if (string.IsNullOrEmpty(Value))
                        MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Config", "Assembler required: {0}", ConquestScript.Instance.Config.AssemblerReq ? "On" : "Off");
                    else
                    {
                        bool? boolTest = GetBool(Value);
                        if (boolTest.HasValue)
                        {
                            ConquestScript.Instance.Config.AssemblerReq = boolTest.Value;
                            MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Config", "Assembler required updated to: {0}", ConquestScript.Instance.Config.AssemblerReq ? "On" : "Off");
                            return;
                        }

                        MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Config", "Assembler required: {0}", ConquestScript.Instance.Config.AssemblerReq ? "On" : "Off");
                    }
                    break;

                #endregion
				
				#region refineryreq
				
		        case "refineryreq":
                    if (string.IsNullOrEmpty(Value))
                        MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Config", "Refinery required: {0}", ConquestScript.Instance.Config.RefineryReq ? "On" : "Off");
                    else
                    {
                        bool? boolTest = GetBool(Value);
                        if (boolTest.HasValue)
                        {
                            ConquestScript.Instance.Config.RefineryReq = boolTest.Value;
                            MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Config", "Refinery required updated to: {0}", ConquestScript.Instance.Config.RefineryReq ? "On" : "Off");
                            return;
                        }

                        MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Config", "Refinery required: {0}", ConquestScript.Instance.Config.RefineryReq ? "On" : "Off");
                    }
                    break;

                #endregion
				
				#region cargoreq
				
		        case "cargoreq":
                    if (string.IsNullOrEmpty(Value))
                        MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Config", "Cargo required: {0}", ConquestScript.Instance.Config.CargoReq ? "On" : "Off");
                    else
                    {
                        bool? boolTest = GetBool(Value);
                        if (boolTest.HasValue)
                        {
                            ConquestScript.Instance.Config.CargoReq = boolTest.Value;
                            MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Config", "Cargo required updated to: {0}", ConquestScript.Instance.Config.CargoReq ? "On" : "Off");
                            return;
                        }

                        MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Config", "Cargo required: {0}", ConquestScript.Instance.Config.CargoReq ? "On" : "Off");
                    }
                    break;

                #endregion
				
				#region staticreq
				
		        case "staticreq":
                    if (string.IsNullOrEmpty(Value))
                        MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Config", "Conquests bases are required to be static grids: {0}", ConquestScript.Instance.Config.StaticReq ? "On" : "Off");
                    else
                    {
                        bool? boolTest = GetBool(Value);
                        if (boolTest.HasValue)
                        {
                            ConquestScript.Instance.Config.StaticReq = boolTest.Value;
                            MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Config", "Static required updated to: {0}", ConquestScript.Instance.Config.StaticReq ? "On" : "Off");
                            return;
                        }

                        MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Config", "Conquests bases are required to be static grids: {0}", ConquestScript.Instance.Config.StaticReq ? "On" : "Off");
                    }
                    break;

                #endregion
				
				#region lcds
				
		        case "lcds":
                    if (string.IsNullOrEmpty(Value))
                        MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Config", "LCD updating is: {0}", ConquestScript.Instance.Config.EnableLcds ? "On" : "Off");
                    else
                    {
                        bool? boolTest = GetBool(Value);
                        if (boolTest.HasValue)
                        {
							var clearRefresh = ConquestScript.Instance.Config.EnableLcds && !boolTest.Value;
                            ConquestScript.Instance.Config.EnableLcds = boolTest.Value;
                            MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Config", "LCD updates changed to: {0}", ConquestScript.Instance.Config.EnableLcds ? "On" : "Off");
                            if (clearRefresh)
                                LcdManager.BlankLcds();
                            return;
                        }
						
						    
                            

                        MessageClientTextMessage.SendMessage(SenderSteamId, "Conquest Config", "LCD updating is: {0}", ConquestScript.Instance.Config.EnableLcds ? "On" : "Off");
                    }
                    break;

                #endregion
				
				default:
				// No Cases matched!
				string Content = string.Format("Points per planetary conquest base: {0}\nPoints per moon conquest base: {1}\nPoints per asteroid conquest base: {2}\nMinimum planet radius (smaller is a moon): {3}m ", 
					ConquestScript.Instance.Config.PlanetPoints.ToString(), ConquestScript.Instance.Config.MoonPoints.ToString(), ConquestScript.Instance.Config.AsteroidPoints.ToString(), ConquestScript.Instance.Config.PlanetSize.ToString());
				Content += string.Format("\nMinimum beacon transmit distance: {0}m\nMinimum distance between conquest bases: {1}m\nConquer distance: {2}m\nUpdate Frequency (Points and new bases): {3} minutes\n",
					ConquestScript.Instance.Config.BeaconDistance.ToString(), ConquestScript.Instance.Config.BaseDistance.ToString(), ConquestScript.Instance.Config.ConquerDistance.ToString(), ConquestScript.Instance.Config.UpdateFrequency.ToString());
				Content += string.Format("Assembler required on conquest base: {0}\nRefinery required on conquest base: {1}\nCargo container required on conquest base: {2}\nConquest bases required to be static grid: {3}\nLCD updating: {4}", 
				FromBool(ConquestScript.Instance.Config.AssemblerReq), FromBool(ConquestScript.Instance.Config.RefineryReq), FromBool(ConquestScript.Instance.Config.CargoReq), FromBool(ConquestScript.Instance.Config.StaticReq), FromBool(ConquestScript.Instance.Config.EnableLcds));
				MessageClientDialogMessage.SendMessage(SenderSteamId, "Frontier Conquest Config", "Configuration options and values", Content);
				break;
            }
        }

        private bool? GetBool(string value)
        {
            bool boolTest;
            if (bool.TryParse(value, out boolTest))
                return boolTest;

            if (value.Equals("off", StringComparison.InvariantCultureIgnoreCase) || value == "0")
                return false;

            if (value.Equals("on", StringComparison.InvariantCultureIgnoreCase) || value == "1")
                return true;
            return null;
        }
		
		private string FromBool(bool value)
		{
			if (value)
			{
				return "On";
			}
			else
			{
				return "Off";
			}
		}
    }
}
