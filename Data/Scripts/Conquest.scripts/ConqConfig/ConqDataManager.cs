namespace Conquest.scripts.ConqConfig
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Conquest.scripts.ConqStructures;
    using Sandbox.ModAPI;
    using VRageMath;

    public static class ConqDataManager
    {
		#region Load and save CONFIG

        public static string GetConfigFilename()
        {
            return string.Format("ConquestConfig_{0}.xml", Path.GetFileNameWithoutExtension(MyAPIGateway.Session.CurrentPath));
        }

        public static ConqConfigStruct LoadConfig()
        {
            string filename = GetConfigFilename();
            ConqConfigStruct Config = null;

            if (!MyAPIGateway.Utilities.FileExistsInLocalStorage(filename, typeof(ConqConfigStruct)))
            {
                Config = InitConfig();
                return Config;
            }

            TextReader reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(filename, typeof(ConqConfigStruct));

            var xmlText = reader.ReadToEnd();
            reader.Close();

            if (string.IsNullOrWhiteSpace(xmlText))
            {
                Config = InitConfig();
                return Config;
            }

            try
            {
                Config = MyAPIGateway.Utilities.SerializeFromXML<ConqConfigStruct>(xmlText);
                ConquestScript.Instance.ServerLogger.WriteInfo("Loading existing ConqConfigStruct.");
            }
            catch
            {
                // Config failed to deserialize.
                ConquestScript.Instance.ServerLogger.WriteError("Failed to deserialize ConqConfigStruct. Creating new ConqConfigStruct.");
                Config = InitConfig();
            }

            return Config;
        }

        private static ConqConfigStruct InitConfig()
        {
            ConquestScript.Instance.ServerLogger.WriteInfo("Creating new ConqConfigStruct.");
            ConqConfigStruct Config = new ConqConfigStruct();
			Config.PlanetPoints = 5;
			Config.MoonPoints = 3;
			Config.AsteroidPoints = 1;
			Config.PlanetSize = 25000;
			Config.BeaconDistance = 50000;
			Config.BaseDistance = 80000;
			Config.ConquerDistance = 40000;
			Config.AssemblerReq = true;
			Config.RefineryReq = true;
			Config.CargoReq = true;
			Config.StaticReq = true;	
			Config.EnableLcds = true;
			Config.UpdateFrequency = 60;
            Config.Antenna = true;
            Config.AreaReq = true;
            Config.Persistant = false;
            Config.Upgrades = true;
            return Config;
        }

        public static void SaveConfig(ConqConfigStruct Config)
        {
            string filename = GetConfigFilename();
            TextWriter writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(filename, typeof(ConqConfigStruct));
            writer.Write(MyAPIGateway.Utilities.SerializeToXML<ConqConfigStruct>(Config));
            writer.Flush();
            writer.Close();
        }

        #endregion
		
		
        #region Load and save DATA

        public static string GetDataFilename()
        {
            return string.Format("ConquestData_{0}.xml", Path.GetFileNameWithoutExtension(MyAPIGateway.Session.CurrentPath));
        }

        public static ConqDataStruct LoadData()
        {
            string filename = GetDataFilename();
            ConqDataStruct data = null;

            if (!MyAPIGateway.Utilities.FileExistsInLocalStorage(filename, typeof(ConqDataStruct)))
            {
                data = InitData();
                return data;
            }

            TextReader reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(filename, typeof(ConqDataStruct));

            var xmlText = reader.ReadToEnd();
            reader.Close();

            if (string.IsNullOrWhiteSpace(xmlText))
            {
                data = InitData();
                return data;
            }

            try
            {
                data = MyAPIGateway.Utilities.SerializeFromXML<ConqDataStruct>(xmlText);
                ConquestScript.Instance.ServerLogger.WriteInfo("Loading existing ConqDataStruct.");
            }
            catch
            {
                // data failed to deserialize.
                ConquestScript.Instance.ServerLogger.WriteError("Failed to deserialize ConqDataStruct. Creating new ConqDataStruct.");
                data = InitData();
            }

            return data;
        }

        private static ConqDataStruct InitData()
        {
            ConquestScript.Instance.ServerLogger.WriteInfo("Creating new ConqDataStruct.");
            ConqDataStruct data = new ConqDataStruct();
			data.ConquestFactions = new List<ConquestFaction>();
			data.ConquestBases = new List<ConquestBase>();
			data.LastRun = MyAPIGateway.Session.GameDateTime;
            return data;
        }

        public static void SaveData(ConqDataStruct data)
        {
            string filename = GetDataFilename();
            TextWriter writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(filename, typeof(ConqDataStruct));
            writer.Write(MyAPIGateway.Utilities.SerializeToXML<ConqDataStruct>(data));
            writer.Flush();
            writer.Close();
        }

        #endregion
    }
}
