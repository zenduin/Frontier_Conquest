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
            Config.Bodies = InitBodies();
			Config.PlanetPoints = 5;
			Config.MoonPoints = 3;
			Config.AsteroidPoints = 1;
			Config.PlanetSize = 25000;
			Config.BeaconDistance = 1000;
			Config.AssemblerReq = true;
			Config.RefineryReq = true;
			Config.CargoReq = true;
			Config.StaticReq = true;	
			Config.EnableLcds = true;
			Config.UpdateFrequency = 30;
            Config.Antenna = true;
            Config.AreaReq = false;
            Config.Persistent = true;
            Config.Upgrades = true;
            return Config;
        }

        public static List<ConqBody> InitBodies()
        {
            List<ConqBody> Bodies = new List<ConqBody>();
            ConqBody Planet = new ConqBody { Type = "Planet", DefaultItems = new List<ConqItem>(), CommonItems = new List<ConqItem>(), RareItems = new List<ConqItem>() };
            ConqBody Moon = new ConqBody { Type = "Moon", DefaultItems = new List<ConqItem>(), CommonItems = new List<ConqItem>(), RareItems = new List<ConqItem>() };
            ConqBody Asteroid = new ConqBody { Type = "Asteroid", DefaultItems = new List<ConqItem>(), CommonItems = new List<ConqItem>(), RareItems = new List<ConqItem>() }; 

            ConqItem Iron = new ConqItem { TypeId = "Ingot", SubTypeName = "Iron", Amount =  2500};

            ConqItem Nickel = new ConqItem { TypeId = "Ingot", SubTypeName = "Nickel", Amount = 750 };
            ConqItem Silicon = new ConqItem { TypeId = "Ingot", SubTypeName = "Silicon", Amount = 650 };
            ConqItem Cobalt = new ConqItem { TypeId = "Ingot", SubTypeName = "Cobalt", Amount = 425 };
            ConqItem Silver = new ConqItem { TypeId = "Ingot", SubTypeName = "Silver", Amount = 110 };

            ConqItem Gold = new ConqItem { TypeId = "Ingot", SubTypeName = "Gold", Amount = 15 };
            ConqItem Magnesium = new ConqItem { TypeId = "Ingot", SubTypeName = "Magnesium", Amount = 5 };
            ConqItem Uranium = new ConqItem { TypeId = "Ingot", SubTypeName = "Uranium", Amount = 5 };
            ConqItem Platinum = new ConqItem { TypeId = "Ingot", SubTypeName = "Platinum", Amount = 3 };

            Planet.DefaultItems.Add(Iron);
            Moon.DefaultItems.Add(Iron);
            Asteroid.DefaultItems.Add(Iron);

            Planet.CommonItems.Add(Nickel);
            Moon.CommonItems.Add(Nickel);
            Asteroid.CommonItems.Add(Nickel);
            Planet.CommonItems.Add(Silicon);
            Moon.CommonItems.Add(Silicon);
            Asteroid.CommonItems.Add(Silicon);
            Planet.CommonItems.Add(Cobalt);
            Moon.CommonItems.Add(Cobalt);
            Asteroid.CommonItems.Add(Cobalt);
            Planet.CommonItems.Add(Silver);
            Moon.CommonItems.Add(Silver);
            Asteroid.CommonItems.Add(Silver);

            Planet.RareItems.Add(Gold);
            Moon.RareItems.Add(Gold);
            Asteroid.RareItems.Add(Gold);
            Planet.RareItems.Add(Magnesium);
            Moon.RareItems.Add(Magnesium);
            Asteroid.RareItems.Add(Magnesium);
            Planet.RareItems.Add(Uranium);
            Moon.RareItems.Add(Uranium);
            Asteroid.RareItems.Add(Uranium);
            Planet.RareItems.Add(Platinum);
            Moon.RareItems.Add(Platinum);
            Asteroid.RareItems.Add(Platinum);
            Bodies.Add(Planet);
            Bodies.Add(Moon);
            Bodies.Add(Asteroid);
            return Bodies;
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
