namespace Conquest.scripts
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Timers;
    using Sandbox.Game.Entities;
    using Sandbox.ModAPI;

    using VRage;
    using VRage.Game;
    using VRage.Game.Components;
    using VRage.Game.ModAPI;
    using VRage.ModAPI;
    using VRageMath;

    using ConqStructures;
    using ConqConfig;
    using Management;
    using Messages;

    using IMyControllableEntity = VRage.Game.ModAPI.Interfaces.IMyControllableEntity;
    using IMyRadioAntenna = Sandbox.ModAPI.Ingame.IMyRadioAntenna;
    using IMyBeacon = Sandbox.ModAPI.Ingame.IMyBeacon;
    using IMyOreDetector = Sandbox.ModAPI.Ingame.IMyOreDetector;
    using IMyAssembler = Sandbox.ModAPI.Ingame.IMyAssembler;
    using IMyRefinery = Sandbox.ModAPI.Ingame.IMyRefinery;
    using IMyCargoContainer = Sandbox.ModAPI.Ingame.IMyCargoContainer;

    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
	public class ConquestScript : MySessionComponentBase
	{		
		const string ConquestConfigPattern = @"^(?<command>/conqconfig)(?:\s+(?<config>((PlanetPoints)|(MoonPoints)|(AsteroidPoints)|(PlanetSize)|(BeaconDistance)|(UpdateFrequency)|(AssemblerReq)|(RefineryReq)|(CargoReq)|(StaticReq)|(AreaReq)|(Lcds)|(Antenna)|(Persistent)|(Upgrades)|(Reward)|(MaxBonusTime)|(MaxBonusMod)|(Debug)))(?:\s+(?<value>.+))?)?";
	    const string ConquestExcludeAddPattern = @"(?<command>/conqexclude)\s+((add)|(create))\s+(?:(?:""(?<name>[^""]|.*?)"")|(?<name>[^\s]*))\s+(?<X>[+-]?((\d+(\.\d*)?)|(\.\d+)))\s+(?<Y>[+-]?((\d+(\.\d*)?)|(\.\d+)))\s+(?<Z>[+-]?((\d+(\.\d*)?)|(\.\d+)))\s+(?<Size>(\d+(\.\d*)?))";
        const string ConquestExcludeDeletePattern = @"(?<command>/conqexclude)\s+((del)|(delete)|(remove))\s+(?:(?:""(?<name>[^""]|.*?)"")|(?<name>.*))";
        const string ConquestAreaAddPattern = @"(?<command>/conqarea)\s+((add)|(create))\s+(?:(?:""(?<name>[^""]|.*?)"")|(?<name>[^\s]*))\s+(?<X>[+-]?((\d+(\.\d*)?)|(\.\d+)))\s+(?<Y>[+-]?((\d+(\.\d*)?)|(\.\d+)))\s+(?<Z>[+-]?((\d+(\.\d*)?)|(\.\d+)))\s+(?<Size>(\d+(\.\d*)?))";
        const string ConquestAreaDeletePattern = @"(?<command>/conqarea)\s+((del)|(delete)|(remove))\s+(?:(?:""(?<name>[^""]|.*?)"")|(?<name>.*))";


        private bool _isInitialized;
        private bool _isClientRegistered;
        private bool _isServerRegistered;
        private Timer _UpdateTimer;
		private Timer _FirstRunTimer;
        private Timer _timer1Events; // 1 second.
		private Timer _timer30Events; // 30 seconds.
        private bool _timer1Block;	
		private bool _timer30Block;
        private bool _UpdateTimerBlock;
		private DateTime NextRun;

	    private readonly Action<byte[]> _messageHandler = new Action<byte[]>(HandleMessage);
		
		public static ConquestScript Instance;

        public TextLogger ServerLogger = new TextLogger(); // This is a dummy logger until Init() is called.
        public TextLogger ClientLogger = new TextLogger(); // This is a dummy logger until Init() is called.
		
		public ConqDataStruct Data;
		public ConqConfigStruct Config;
		
		public FastResourceLock DataLock = new FastResourceLock();

        #region attaching events and wiring up
		
		public override void UpdateAfterSimulation()
		{
			Instance = this;

            // This needs to wait until the MyAPIGateway.Session.Player is created, as running on a Dedicated server can cause issues.
            // It would be nicer to just read a property that indicates this is a dedicated server, and simply return.
			if (!_isInitialized && MyAPIGateway.Session != null && MyAPIGateway.Session.Player != null)
            {
                if (MyAPIGateway.Session.OnlineMode.Equals(MyOnlineModeEnum.OFFLINE)) // pretend single player instance is also server.
                    InitServer();
                if (!MyAPIGateway.Session.OnlineMode.Equals(MyOnlineModeEnum.OFFLINE) && MyAPIGateway.Multiplayer.IsServer && !MyAPIGateway.Utilities.IsDedicated)
                    InitServer();
                InitClient();
            }

            // Dedicated Server.
            if (!_isInitialized && MyAPIGateway.Utilities != null && MyAPIGateway.Multiplayer != null
                && MyAPIGateway.Session != null && MyAPIGateway.Utilities.IsDedicated && MyAPIGateway.Multiplayer.IsServer)
            {
                InitServer();
            }
			
            base.UpdateAfterSimulation();
		}
        private void InitClient()
		{
			_isInitialized = true; // Set this first to block any other calls from UpdateAfterSimulation().
			_isClientRegistered = true;
			ClientLogger.Init("ConquestClient.Log", false, 0); // comment this out if logging is not required for the Client.
			ClientLogger.WriteStart("Conquest Client Log Started");
			ClientLogger.WriteInfo("Conquest Client Version {0}", ConquestConsts.ModCommunicationVersion);
			if (ClientLogger.IsActive)
				VRage.Utils.MyLog.Default.WriteLine(String.Format("##Mod## Conquest Client Logging File: {0}", ClientLogger.LogFile));

			MyAPIGateway.Utilities.MessageEntered += GotMessage;

			if (MyAPIGateway.Multiplayer.MultiplayerActive && !_isServerRegistered) // if not the server, also need to register the messagehandler.
			{
				ClientLogger.WriteStart("RegisterMessageHandler");
				MyAPIGateway.Multiplayer.RegisterMessageHandler(ConquestConsts.ConnectionId, _messageHandler);
			}
			MyAPIGateway.Utilities.ShowMessage("Frontier Conquest", string.Format("Version {0} loaded, type /conqhelp for available commands.", ConquestConsts.MajorVer));
		}

		public void ResetData()
		{
            if (Config.Debug) ServerLogger.WriteStart("ResetData Called");
            List<ConquestFaction> NewFactions = new List<ConquestFaction>();
			List<ConquestBase> NewBases = new List<ConquestBase>();
			DataLock.AcquireExclusive();
				Data.ConquestFactions = NewFactions;
				Data.ConquestBases = NewBases;
				LcdManager.UpdateLcds();		
			DataLock.ReleaseExclusive();
			_UpdateTimer.Stop();
			_FirstRunTimer.Interval = 1;
			_FirstRunTimer.Start();
            if (Config.Debug) ServerLogger.WriteStart("ResetData Returned");
        }
        private void InitServer()
        {
            _isInitialized = true; // Set this first to block any other calls from UpdateAfterSimulation().
            _isServerRegistered = true;
            ServerLogger.Init("ConquestServer.Log", false, 0); // comment this out if logging is not required for the Server.
            ServerLogger.WriteStart("Conquest Server Log Started");
            ServerLogger.WriteInfo("Conquest Server Version {0}", ConquestConsts.ModCommunicationVersion);
            if (ServerLogger.IsActive)
                VRage.Utils.MyLog.Default.WriteLine(String.Format("##Mod## Conquest Server Logging File: {0}", ServerLogger.LogFile));

            ServerLogger.WriteStart("RegisterMessageHandler");
            MyAPIGateway.Multiplayer.RegisterMessageHandler(ConquestConsts.ConnectionId, _messageHandler);

            Config = ConqDataManager.LoadConfig();
            // This is to update config file already generated by older mod version
            if (Config.Bodies.Count == 0)
            {
               Config.Bodies = ConqDataManager.InitBodies();
            }
            Data = ConqDataManager.LoadData();


            // start the timer last, as all data should be loaded before this point.
            ServerLogger.WriteStart("Attaching Event 1 timer.");
            _timer1Events = new Timer(1000);
            _timer1Events.Elapsed += Timer1EventsOnElapsed;
            _timer1Events.Start();
            ServerLogger.WriteStart("Attaching Event 30 timer.");
            _timer30Events = new Timer(30000);
            _timer30Events.Elapsed += Timer30EventsOnElapsed;
            _timer30Events.Start();
            _UpdateTimer = new Timer(Config.UpdateFrequency * 60000);
            _UpdateTimer.Elapsed += UpdateTimerEventsOnElapsed;
            try
            {
                NextRun = Data.LastRun.AddMinutes(Config.UpdateFrequency);

                //Milliseconds = (NextRun-MyAPIGateway.Session.GameDateTime).TotalMilliseconds;		
                _FirstRunTimer = new Timer((NextRun - MyAPIGateway.Session.GameDateTime).TotalMilliseconds);
                ServerLogger.WriteStart(string.Format("Current game time is {0}. Last scan was at {1}. Scheduling first scan for {2} (in {3}).", MyAPIGateway.Session.GameDateTime, Data.LastRun, NextRun, (NextRun - MyAPIGateway.Session.GameDateTime).ToString()));

            }
            catch
            {
                ServerLogger.WriteStart(string.Format("Error setting next scan time, defaulting to {0} minutes from now. Last run: {1}", Config.UpdateFrequency, Data.LastRun));
                _FirstRunTimer = new Timer(Config.UpdateFrequency * 60000);
            }
            _FirstRunTimer.Elapsed += FirstRunTimerEventsOnElapsed;
            _FirstRunTimer.AutoReset = false;
            _FirstRunTimer.Start();
        }
        #endregion attaching events and wiring up

        #region detaching events

        protected override void UnloadData()
        {
            ClientLogger.WriteStop("UnloadData");
            ServerLogger.WriteStop("UnloadData");
            if (_isClientRegistered)
            {
                if (MyAPIGateway.Utilities != null)
                {
					MyAPIGateway.Utilities.MessageEntered -= GotMessage;
                }
                if (!_isServerRegistered) // if not the server, also need to unregister the messagehandler.
                {
                    ClientLogger.WriteStop("UnregisterMessageHandler");
                    MyAPIGateway.Multiplayer.UnregisterMessageHandler(ConquestConsts.ConnectionId, _messageHandler);
                }
                ClientLogger.WriteStop("Log Closed");
                ClientLogger.Terminate();
            }
            if (_isServerRegistered)
            {
                ServerLogger.WriteStop("UnregisterMessageHandler");
                MyAPIGateway.Multiplayer.UnregisterMessageHandler(ConquestConsts.ConnectionId, _messageHandler);

                if (_UpdateTimer != null)
                {
                    ServerLogger.WriteStop("Stopping Update timer.");
                    _UpdateTimer.Stop();
                    _UpdateTimer.Elapsed -= UpdateTimerEventsOnElapsed;
                    _UpdateTimer.Close();
                    _UpdateTimer = null;
					
                }
				if (_timer1Events != null)
                {
                    ServerLogger.WriteStop("Stopping Event 1 timer.");
                    _timer1Events.Stop();
                    _timer1Events.Elapsed -= Timer1EventsOnElapsed;
                    _timer1Events.Close();
                    _timer1Events = null;
                }
				if (_timer30Events != null)
                {
                    ServerLogger.WriteStop("Stopping Event 30 timer.");
                    _timer30Events.Stop();
                    _timer30Events.Elapsed -= Timer30EventsOnElapsed;
                    _timer30Events.Close();
                    _timer30Events = null;
                }
				if (_FirstRunTimer != null)
				{
					ServerLogger.WriteStop("Stopping First run timer.");
					_FirstRunTimer.Stop();
                    _FirstRunTimer.Elapsed -= FirstRunTimerEventsOnElapsed;
                    _FirstRunTimer.Close();
                    _FirstRunTimer = null;
				}
                Data = null;
                ServerLogger.WriteStop("Log Closed");
                ServerLogger.Terminate();
            }
            base.UnloadData();
        }

        public override void SaveData()
        {
            ClientLogger.WriteStop("SaveData");
            ServerLogger.WriteStop("SaveData");

            if (_isServerRegistered)
            {
				if (Data != null && DataLock.TryAcquireExclusive())
                {
                    ServerLogger.WriteInfo("Save Data Started");
                    ConqDataManager.SaveData(Data);
                    ServerLogger.WriteInfo("Save Data End");
                    DataLock.ReleaseExclusive();
                }
				if (Config != null)
                {
                    ServerLogger.WriteInfo("Save Config Started");
                    ConqDataManager.SaveConfig(Config);
                    ServerLogger.WriteInfo("Save Config End");
                }
			}
            base.SaveData();
        }

        #endregion detaching events

        #region message handling

        private void GotMessage(string messageText, ref bool sendToOthers)
        {
            try
            {
                // here is where we nail the echo back on commands "return" also exits us from processMessage
                if (ProcessMessage(messageText)) { sendToOthers = false; }
            }
            catch (Exception ex)
            {
                ClientLogger.WriteException(ex);
                MyAPIGateway.Utilities.ShowMessage("Error", "An exception has been logged in the file: " + ClientLogger.LogFileName);
                DataLock.ReleaseExclusive();
            }
        }

        private static void HandleMessage(byte[] message)
        {
            ConquestScript.Instance.ServerLogger.WriteVerbose("HandleMessage");
            ConquestScript.Instance.ClientLogger.WriteVerbose("HandleMessage");
            ConnectionHelper.ProcessData(message);
        }
        #endregion message handling

        #region timers
        private void FirstRunTimerEventsOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
		{
			ServerLogger.WriteStart("Starting Conquest Update Timer.");
			_UpdateTimer.Start();
			UpdateTimerEventsOnElapsed(sender, elapsedEventArgs);
		}
		
		private void Timer1EventsOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {

            MyAPIGateway.Utilities.InvokeOnGameThread(delegate
            {
                // Recheck main Gateway properties, as the Game world my be currently shutting down when the InvokeOnGameThread is called.
                if (MyAPIGateway.Players == null || MyAPIGateway.Entities == null || MyAPIGateway.Session == null || MyAPIGateway.Utilities == null)
                    return;

                if (_timer1Block) // prevent other any additional calls into this code while it may still be running.
                    return;

                if (DataLock.TryAcquireExclusive())
                {
                    _timer1Block = true;
                    LcdManager.UpdateLcds();
                    DataLock.ReleaseExclusive();
                    _timer1Block = false;
                }
            });            
        }
		
        private void UpdateTimerEventsOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
			// Recheck main Gateway properties, as the Game world my be currently shutting down when the InvokeOnGameThread is called.
			if (MyAPIGateway.Players == null || MyAPIGateway.Entities == null || MyAPIGateway.Session == null || MyAPIGateway.Utilities == null)
				return;

			if (_UpdateTimerBlock) // prevent any additional calls into this code while it may still be running.
				return;
	
			int Hours = (MyAPIGateway.Session.GameDateTime-Data.LastRun).Hours;

			_UpdateTimerBlock = true;
            if (Config.Debug) ServerLogger.WriteStart("Update Timer even code Called");

            MyObjectBuilder_FactionCollection FactionCollection = MyAPIGateway.Session.Factions.GetObjectBuilder();
			IMyFactionCollection TempFactions = MyAPIGateway.Session.Factions;
			IMyEntities TempEntities = MyAPIGateway.Entities;
			HashSet<IMyEntity> Grids = new HashSet<IMyEntity>();
			MyAPIGateway.Entities.GetEntities(Grids, x => x is IMyCubeGrid);
							
			MyAPIGateway.Parallel.Start(delegate ()
            // Background processing occurs within this block.
            {
                if (Config.Debug) ServerLogger.WriteStart("Update Timer parallel task started");
                ServerLogger.WriteStart("UpdateTimer Acquire Data Lock. START");
                DataLock.AcquireExclusive();
                Data.LastRun = MyAPIGateway.Session.GameDateTime;
                ConqDataStruct TempData = Data;
                DataLock.ReleaseExclusive();
                ServerLogger.WriteStart("UpdateTimer Released Data Lock. START");
                if (Config.Persistent)
                {
                    foreach (ConquestFaction Faction in TempData.ConquestFactions)
                    {
                        Faction.VictoryPoints = 0;
                    }
                }                   
                foreach (ConquestBase Base in TempData.ConquestBases)
				{
					if (Base.IsValid && Base.IsValidPoints) 
					{                        
                        ConquestFaction Faction = TempData.ConquestFactions.Find(x => x.FactionId == Base.FactionId);
                        int InitialPoints = Faction.VictoryPoints;
                        double PointMod = 1;
                        if (Config.MaxBonusMod > 1)
                        {
                            // time in minutes this base has survived
                            double TimeAlive = MyAPIGateway.Session.GameDateTime.Subtract(Base.Established).TotalMinutes;
                            if (TimeAlive > Config.MaxBonusTime)
                            {
                                PointMod = Config.MaxBonusMod;
                            }
                            else
                            {
                                // gets a modifier with min 1 and max Config.MaxBonusMod
                                PointMod = (TimeAlive / Config.MaxBonusTime) * (Config.MaxBonusMod - 1) + 1;
                            }
                        }
                        int NewPoints = ((Base.Asteroids * Config.AsteroidPoints) + (Base.Moons * Config.MoonPoints) + (Base.Planets * Config.PlanetPoints)) * (int)PointMod;                        
                        if (Config.Reward)
                        {
                            ConquestGrid ConqGrid = new ConquestGrid((IMyCubeGrid)MyAPIGateway.Entities.GetEntityById(Base.EntityId));
                            if (Config.Debug)
                                ServerLogger.WriteStart("Retrieved ConquestGrid from EntityId");
                            if (Base.Planets>0)
                            {
                                ConqBody Body = Config.Bodies.FirstOrDefault(x => x.Type == "Planet");
                                Reward(ConqGrid, Body, Base.Planets * Config.PlanetPoints * (Base.Radius / 50000) * (float)PointMod);
                            }
                            if (Base.Moons > 0)
                            {
                                ConqBody Body = Config.Bodies.FirstOrDefault(x => x.Type == "Moon");
                                Reward(ConqGrid, Body, Base.Moons * Config.MoonPoints * (Base.Radius / 50000) * (float)PointMod);
                            }
                            if (Base.Asteroids > 0)
                            {
                                ConqBody Body = Config.Bodies.FirstOrDefault(x => x.Type == "Asteroid");
                                Reward(ConqGrid, Body, Base.Asteroids * Config.AsteroidPoints * (Base.Radius / 50000) * (float)PointMod);
                            }
                        }
                        if (Config.Upgrades)
                        {
                            try
                            {
                                IMyCubeGrid Grid = MyAPIGateway.Entities.GetEntityById(Base.EntityId) as IMyCubeGrid;
                                List<IMySlimBlock> Blocks = new List<IMySlimBlock>();
                                Grid.GetBlocks(Blocks);
                                foreach (IMySlimBlock SBlock in Blocks)
                                {
                                    IMyCubeBlock Block = SBlock.FatBlock;
                                    if ((Block is IMyRefinery) || (Block is IMyAssembler))
                                    {
                                        Block.AddUpgradeValue("ConquestProductivity", (float)PointMod);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                ConquestScript.Instance.ServerLogger.WriteException(ex);
                                MyAPIGateway.Utilities.ShowMessage("Error", "An exception has been logged in the file: " + ConquestScript.Instance.ServerLogger.LogFileName);
                            }
                        }                                                 
                        Faction.VictoryPoints += NewPoints;
                        ServerLogger.WriteStart("Assigned " + NewPoints + " points for valid base " + Base.DisplayName);
                        Base.IsValidPoints = true;
                    }
                    else if (Base.IsValid)
                    {
                        Base.IsValidPoints = true;
                    }
				}
				ServerLogger.WriteStart("Finished assigning points.");
				TempData.ConquestFactions.Sort(CompareFaction);
                if (Config.Debug) ServerLogger.WriteStart("UpdateTimer Acquiring Data Lock. END");
				DataLock.AcquireExclusive();
					Data = TempData;  
				DataLock.ReleaseExclusive();
                if (Config.Debug) ServerLogger.WriteStart("UpdateTimer Released Data Lock. END");
				_UpdateTimerBlock = false;
                if (Config.Debug) ServerLogger.WriteStart("Update Timer parallel task finished");
            });
        }

        private void Reward(ConquestGrid ConqGrid, ConqBody Body, float Modifier)
        {
            try
            {
                    if (Body.DefaultItems.Count > 0)
                    {
                        foreach (ConqItem Item in Body.DefaultItems)
                        {
                            ConqGrid.AddItem(Item, Modifier);
                        }
                    }
                    if (Body.CommonItems.Count > 0)
                    {
                        Random RewardSelect = new Random();
                        ConqItem Item = Body.CommonItems[RewardSelect.Next(0, Body.CommonItems.Count)];
                        ConqGrid.AddItem(Item, Modifier);
                        Item = Body.CommonItems[RewardSelect.Next(0, Body.CommonItems.Count)];
                        ConqGrid.AddItem(Item, Modifier);
                    }
                    if (Body.RareItems.Count > 0)
                    {
                        Random RewardSelect = new Random();
                        ConqItem Item = Body.RareItems[RewardSelect.Next(0, Body.RareItems.Count)];
                        ConqGrid.AddItem(Item, Modifier);
                    }
            }
            catch (Exception ex)
            {
                ServerLogger.WriteException(ex);
                MyAPIGateway.Utilities.ShowMessage("Error", "An exception has been logged in the file: {0}" + ServerLogger.LogFileName);
            }
        }

        private void Timer30EventsOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            if (Config.Debug) ServerLogger.WriteStart("Timer30 Events Called");
            // Recheck main Gateway properties, as the Game world my be currently shutting down when the InvokeOnGameThread is called.
            if (MyAPIGateway.Players == null || MyAPIGateway.Entities == null || MyAPIGateway.Session == null || MyAPIGateway.Utilities == null)
				return;

			if (_timer30Block) // prevent any additional calls into this code while it may still be running.
				return;

			_timer30Block = true;

			MyObjectBuilder_FactionCollection FactionCollection = MyAPIGateway.Session.Factions.GetObjectBuilder();
			IMyFactionCollection TempFactions = MyAPIGateway.Session.Factions;
			IMyEntities TempEntities = MyAPIGateway.Entities;
			HashSet<IMyEntity> Grids = new HashSet<IMyEntity>();
			MyAPIGateway.Entities.GetEntities(Grids, x => x is IMyCubeGrid);

            MyAPIGateway.Parallel.Start(delegate ()
            // Background processing occurs within this block.
            {
                if (Config.Debug) ServerLogger.WriteStart("Timer30 Parallel task started");
                if (Config.Debug) ServerLogger.WriteStart("Timer30 Acquire Data Lock. START");
                DataLock.AcquireExclusive();
                ConqDataStruct TempData = Data;
                DataLock.ReleaseExclusive();
                if (Config.Debug) ServerLogger.WriteStart("Timer30 Released Data Lock. START");
                if (Config.Debug) ServerLogger.WriteStart("Updating Factions");
				List<ConquestFaction> ValidFactions = new List<ConquestFaction>();
				foreach (MyObjectBuilder_Faction FactionBuilder in FactionCollection.Factions)
				{
					Int64 FactionId = FactionBuilder.FactionId;
					IMyFaction Faction = TempFactions.TryGetFactionById(FactionId);
					if (!Faction.IsEveryoneNpc()) 
					{
						bool NewFaction = true;
						foreach (ConquestFaction ConqFaction in TempData.ConquestFactions)
						{
							if (ConqFaction.FactionId == FactionId)
							{
								NewFaction = false;
                                // update Faction Tags and names since these can change!
                                ConqFaction.FactionTag = FactionBuilder.Tag;
                                ConqFaction.FactionName = FactionBuilder.Name;
								ValidFactions.Add(ConqFaction);
								break;
							}
						}
						if (NewFaction)
						{
							ConquestFaction ConqFaction = new ConquestFaction();
							ConqFaction.FactionId = FactionId;
							ConqFaction.FactionName = Faction.Name;
							ConqFaction.FactionTag = Faction.Tag;
							ValidFactions.Add(ConqFaction);
						}
					}
				}
				
				List<ConquestBase> ValidBases = new List<ConquestBase>();
                if (Config.Debug) ServerLogger.WriteStart("Begin Background Grid Scanning (not for points)");
				foreach (IMyCubeGrid Grid in Grids)
				{
                    ConquestGrid ConqGrid = new ConquestGrid(Grid);
					//MyAPIGateway.Utilities.ShowMessage("IMyCubeGrid: ", Grid.DisplayName + " " + IsConquestBase(Grid, ref TempPosition).ToString());
                    bool IsNewBase = true;
                    if (ConqGrid.IsValid)
                    {
                        foreach (ConquestBase Base in TempData.ConquestBases)
                        {
                            if (Grid.EntityId == Base.EntityId)
                            {
                                if ((ConqGrid.Position == Base.Position) && (ConqGrid.Radius == Base.Radius))
                                {
                                    Base.DisplayName = Grid.DisplayName;
                                    Base.FactionId = TempFactions.TryGetPlayerFaction(Grid.BigOwners[0]).FactionId;
                                    Base.IsValid = true;
                                    ValidBases.Add(Base);
                                    IsNewBase = false;
                                }
                                // base has moved or radius has changed but is now valid
                                else if ((ConqGrid.Position != Base.Position) || (ConqGrid.Radius != Base.Radius))
                                {
                                    BoundingSphereD Sphere = new BoundingSphereD(ConqGrid.Position, ConqGrid.Radius);
                                    List<IMyEntity> Entities = TempEntities.GetEntitiesInSphere(ref Sphere);
                                    int PlanetCount = 0;
                                    int AsteroidCount = 0;
                                    int MoonCount = 0;
                                    List<string> Bodies = new List<string>();
                                    foreach (IMyEntity Entity in Entities)
                                    {
                                        if (Entity is MyPlanet)
                                        {
                                            MyPlanet Planet = Entity as MyPlanet;
                                            if (Planet.AverageRadius >= Config.PlanetSize)
                                            {
                                                PlanetCount++;
                                            }
                                            else
                                            {
                                                MoonCount++;
                                            }
                                        }
                                        else if (Entity is MyVoxelMap)
                                        {
                                            AsteroidCount++;
                                        }
                                    }
                                    //Don't count rocks on planets and moons as asteroids
                                    if (PlanetCount > 0 || MoonCount > 0)
                                    {
                                        AsteroidCount = 0;
                                    }
                                    Base.DisplayName = Grid.DisplayName;
                                    Base.FactionId = TempFactions.TryGetPlayerFaction(Grid.BigOwners[0]).FactionId;
                                    Base.Planets = PlanetCount;
                                    Base.Moons = MoonCount;
                                    Base.Asteroids = AsteroidCount;
                                    Base.Position = ConqGrid.Position;
                                    Base.Radius = ConqGrid.Radius;
                                    Base.IsValid = true;
                                    Base.IsValidPoints = false;
                                    Base.Established = MyAPIGateway.Session.GameDateTime;
                                    IsNewBase = false;
                                    ValidBases.Add(Base);
                                }
                                else
                                {
                                    IsNewBase = false;
                                    Base.IsValidPoints = false;
                                    ValidBases.Add(Base);
                                }
                            }
                        }
                        if (IsNewBase)
                        {
                            BoundingSphereD Sphere = new BoundingSphereD(ConqGrid.Position, ConqGrid.Radius);
                            List<IMyEntity> Entities = TempEntities.GetEntitiesInSphere(ref Sphere);
                            int PlanetCount = 0;
                            int AsteroidCount = 0;
                            int MoonCount = 0;
                            foreach (IMyEntity Entity in Entities)
                            {
                                if (Entity is MyPlanet)
                                {
                                    MyPlanet Planet = Entity as MyPlanet;
                                    if (Planet.AverageRadius >= Config.PlanetSize)
                                    {
                                        PlanetCount++;
                                    }
                                    else
                                    {
                                        MoonCount++;
                                    }
                                }
                                else if (Entity is MyVoxelMap)
                                {
                                    AsteroidCount++;
                                }
                            }
                            //Don't count rocks on planets and moons as asteroids
                            if (PlanetCount > 0 || MoonCount > 0)
                            {
                                AsteroidCount = 0;
                            }
                            ConquestBase NewBase = new ConquestBase();
                            NewBase.DisplayName = Grid.DisplayName;
                            NewBase.EntityId = Grid.EntityId;
                            NewBase.FactionId = TempFactions.TryGetPlayerFaction(Grid.BigOwners[0]).FactionId;
                            NewBase.Planets = PlanetCount;
                            NewBase.Moons = MoonCount;
                            NewBase.Asteroids = AsteroidCount;
                            NewBase.Radius = ConqGrid.Radius;
                            NewBase.Position = ConqGrid.Position;
                            NewBase.IsValid = true;
                            NewBase.Established = MyAPIGateway.Session.GameDateTime;
                            ValidBases.Add(NewBase);
                        }
                    }                    
                }

                if (Config.Debug) ServerLogger.WriteStart("Resetting faction base counts...");
				foreach (ConquestFaction Faction in ValidFactions) 
				{
					Faction.PlanetBases = 0;
					Faction.MoonBases = 0;
					Faction.AsteroidBases = 0;
				}
                if (Config.Debug) ServerLogger.WriteStart("Grid Scanning Finished, Checking that bases are valid...");
				for (int i=0;i<ValidBases.Count;i++)
				{
                    if (!ValidBases[i].IsValid)
                    {
                        continue;
                    }
                    foreach (ConquestBase ConqBase in ValidBases)
					{
                        // if not the same base
                        if (ValidBases[i].EntityId != ConqBase.EntityId)
                        {
                            // if the distance between these bases is less than the sum of their radii (they are too close together)
                            if ((ValidBases[i].Radius + ConqBase.Radius) > Vector3D.Distance(ValidBases[i].Position, ConqBase.Position))
                            {
                                ValidBases[i].IsValid = false;
                                ConqBase.IsValid = false;
                                ValidBases[i].IsValidPoints = false;
                                ConqBase.IsValidPoints = false;
                            }
                        }
					}
                    if (!ValidBases[i].IsValid)
                    {
                        continue;
                    }
                    foreach (ConquestExclusionZone ExclusionZone in TempData.ConquestExclusions)
					{
						if ((Vector3D.Distance(ValidBases[i].Position, ExclusionZone.Position) < ExclusionZone.Radius))
						{
                            ValidBases[i].IsValid = false;
                            ValidBases[i].IsValidPoints = false;
                            break;
						}
					}
                    if (Config.AreaReq)
                    {
                        ValidBases[i].IsValid = false;
                        foreach (ConquestAreaZone AreaZone in TempData.ConquestAreas)
                        {
                            if ((Vector3D.Distance(ValidBases[i].Position, AreaZone.Position) < AreaZone.Radius))
                            {
                                ValidBases[i].IsValid = true;
                                ValidBases[i].IsValidPoints = false;
                                break;
                            }
                        }
                    }
                }
                foreach (ConquestBase Base in ValidBases)
				{
					if (Base.IsValid)
					{
						try 
						{
							ConquestFaction Faction = ValidFactions.Find(x => x.FactionId == Base.FactionId);
							if (Base.Planets > 0)
							{
								Faction.PlanetBases += 1;
							} 
							if (Base.Moons > 0)
							{
								Faction.MoonBases += 1;
							} 
							if (Base.Asteroids > 0)
							{
								Faction.AsteroidBases += 1;
							}
						}
						catch
						{
							continue;
						}
					}
				}
                ValidFactions.Sort(CompareFaction);
                if (Config.Debug) ServerLogger.WriteStart("Timer30 Acquire Data Lock. END");
                DataLock.AcquireExclusive();
					Data.ConquestFactions = ValidFactions;
                    Data.ConquestBases = ValidBases;
				DataLock.ReleaseExclusive();
                if (Config.Debug) ServerLogger.WriteStart("Timer30 Release Data Lock. END");
                _timer30Block = false;
                if (Config.Debug) ServerLogger.WriteStart("Timer30 Parallel task finished");
            });
            if (Config.Debug) ServerLogger.WriteStart("Timer30 Events Called");
        }

        #endregion timers

        #region command list
        private bool ProcessMessage(string messageText)
        {
            Match match; // used by the Regular Expression to test user input.
                         // this list is going to get messy since the help and commands themself tell user the same thing 
            string[] split = messageText.Split(new Char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            // nothing useful was entered.
            if (split.Length == 0)
                return false;

			#region leaderboard

            if (split[0].Equals("/conqlb", StringComparison.InvariantCultureIgnoreCase) || split[0].Equals("/conqleaderboard", StringComparison.InvariantCultureIgnoreCase))
            {
				MessageConqLeaderboard.SendMessage();
				//something slipped through the cracks lets get out of here before something odd happens.
                return true;
            }
            #endregion leaderboard
			
			#region conquestbase

            if (split[0].Equals("/conqbase", StringComparison.InvariantCultureIgnoreCase))
            {
				IMyEntity selectedEntity = FindLookAtEntity(MyAPIGateway.Session.ControlledObject, false, true, false, false, false, false, false);
				Vector3D tempPosition = MyAPIGateway.Session.LocalHumanPlayer.GetPosition();
						
				if (selectedEntity != null)
				{
					IMyCubeBlock Block = selectedEntity as IMyCubeBlock;
					MessageConqBase.SendMessage(Block.CubeGrid.EntityId, tempPosition);
				}
				else
				{
					MessageConqBasePos.SendMessage(tempPosition);
				}
				
				//something slipped through the cracks lets get out of here before something odd happens.
				return true;
            }
            #endregion conquestbase
			
			#region conqconfig
			match = Regex.Match(messageText, ConquestConfigPattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                MessageConqConfig.SendMessage(match.Groups["config"].Value, match.Groups["value"].Value);
                return true;
            }
			#endregion
			
			#region conqexclude
			if (split[0].Equals("/conqexclude", StringComparison.InvariantCultureIgnoreCase) && MyAPIGateway.Session.Player.IsAdmin())
            {
				match = Regex.Match(messageText, ConquestExcludeAddPattern, RegexOptions.IgnoreCase);
				if (match.Success)
				{
					string ZoneName = match.Groups["name"].Value;
					double x = Convert.ToDouble(match.Groups["X"].Value, CultureInfo.InvariantCulture);
					double y = Convert.ToDouble(match.Groups["Y"].Value, CultureInfo.InvariantCulture);
					double z = Convert.ToDouble(match.Groups["Z"].Value, CultureInfo.InvariantCulture);
					int size = Convert.ToInt32(match.Groups["Size"].Value, CultureInfo.InvariantCulture);
					MessageConqExclude.SendAddMessage(ZoneName, x, y, z, size);
					return true;
				}
				
				match = Regex.Match(messageText, ConquestExcludeDeletePattern, RegexOptions.IgnoreCase);
				if (match.Success)
				{
					MessageConqExclude.SendRemoveMessage(match.Groups["name"].Value);
					return true;
				}
				
				if (split.Length > 1 && split[1].Equals("list", StringComparison.InvariantCultureIgnoreCase))
                {
                    MessageConqExclude.SendListMessage();
                    return true;
                }
				
				 MyAPIGateway.Utilities.ShowMessage("/conqexclude list/[add]/remove zone [x y z radius]", "Manages conquest base exclusion zones");
				 return true;
				
			}
            #endregion

            #region conqarea
            if (split[0].Equals("/conqarea", StringComparison.InvariantCultureIgnoreCase) && MyAPIGateway.Session.Player.IsAdmin())
            {
                match = Regex.Match(messageText, ConquestAreaAddPattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    string ZoneName = match.Groups["name"].Value;
                    double x = Convert.ToDouble(match.Groups["X"].Value, CultureInfo.InvariantCulture);
                    double y = Convert.ToDouble(match.Groups["Y"].Value, CultureInfo.InvariantCulture);
                    double z = Convert.ToDouble(match.Groups["Z"].Value, CultureInfo.InvariantCulture);
                    int size = Convert.ToInt32(match.Groups["Size"].Value, CultureInfo.InvariantCulture);
                    MessageConqArea.SendAddMessage(ZoneName, x, y, z, size);
                    return true;
                }

                match = Regex.Match(messageText, ConquestAreaDeletePattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    MessageConqArea.SendRemoveMessage(match.Groups["name"].Value);
                    return true;
                }

                if (split.Length > 1 && split[1].Equals("list", StringComparison.InvariantCultureIgnoreCase))
                {
                    MessageConqArea.SendListMessage();
                    return true;
                }

                MyAPIGateway.Utilities.ShowMessage("/conqarea list/[add]/remove zone [x y z radius]", "Manages conquest area zones");
                return true;

            }
            #endregion


            #region conqreset
            if (split[0].Equals("/conqreset", StringComparison.InvariantCultureIgnoreCase) && MyAPIGateway.Session.Player.IsAdmin())
            {		
                MessageConqReset.SendMessage();				
				return true;				
			}
			#endregion
						
            #region help
            // help command
            if (split[0].Equals("/conqhelp", StringComparison.InvariantCultureIgnoreCase))
            {
                if (split.Length <= 1)
                {
                    //did we just type conqhelp? show what else they can get help on
                    //might be better to make a more detailed help reply here using mission window later on
                    MyAPIGateway.Utilities.ShowMessage("Conquest Help", "Commands: conqbase, conqlb & conqleaderboard, conqconfig");
					if (MyAPIGateway.Session.Player.IsAdmin())
                    {
						MyAPIGateway.Utilities.ShowMessage("Conquest Help", "Admin commands: conqconfig conqexclude conqarea conqreset");
					}
                    MyAPIGateway.Utilities.ShowMessage("Conquest Help", "Try '/conqhelp command' for more informations about specific command");
                    return true;
                }
                else
                {
                    switch (split[1].ToLowerInvariant())
                    {
                        // did we type /chelp help ?
                        case "help":
                            MyAPIGateway.Utilities.ShowMessage("/conqhelp #", "Displays help on the specified command [#].");
                            return true;
                        // did we type /help buy etc
                        case "conqbase":
                            MyAPIGateway.Utilities.ShowMessage("Conquest Help", "Target (place reticle over) a block, this command tells you if that grid is a valid conquest base.");
                            MyAPIGateway.Utilities.ShowMessage("Conquest Help", "If you use this command without targetting a block, it will tell you if YOUR current location is valid for a conquest base.");
                            return true;
						case "conqlb":
                            MyAPIGateway.Utilities.ShowMessage("Conquest Help", "This command shows the current faction leaderboard in a dialog window. Note that base numbers update every 30sec.");
                            return true;
						case "conqleaderboard":
                            MyAPIGateway.Utilities.ShowMessage("Conquest Help", "This command shows the current faction leaderboard in a dialog window. Note that base numbers update every 30sec.");
                            return true;
						case "conqconfig":
							if (!MyAPIGateway.Session.Player.IsAdmin()) 
                                {
									return false;
								}
								else
								{
                                MyAPIGateway.Utilities.ShowMessage("Conquest Help", "/conqconfig subcommands: PlanetPoints MoonPoints AsteroidPoints PlanetSize BeaconDistance UpdateFrequency AssemblerReq RefineryReq CargoReq StaticReq AreaReq Lcds Antenna Upgrades Reward Debug Persistent");
                                return true;
								}
						case "conqexclude":
							if (!MyAPIGateway.Session.Player.IsAdmin()) 
                                {
									return false;
								}
								else
								{
									MyAPIGateway.Utilities.ShowMessage("Conquest Help", "/conqexclude subcommands: Add [Name X Y Z Radius] Remove [Name] List");
									return true; 
								}
                        case "conqarea":
                            if (!MyAPIGateway.Session.Player.IsAdmin())
                            {
                                return false;
                            }
                            else
                            {
                                MyAPIGateway.Utilities.ShowMessage("Conquest Help", "/conqarea subcommands: Add [Name X Y Z Radius] Remove [Name] List");
                                return true;
                            }
                        case "conqreset":
							if (!MyAPIGateway.Session.Player.IsAdmin()) 
								{
									return false;
								}
								else
								{
									MyAPIGateway.Utilities.ShowMessage("Conquest Help", "This command clears all current conquest factions and bases, then starts a fresh scan.");
									return true; 
								}
                    }
                }
            }
            #endregion help
	
            // it didnt start with help or anything else that matters so return false and get us out of here;
            return false;
        }
        #endregion command list
	
		
		public static int CompareFaction(ConquestFaction A, ConquestFaction B)
		{
			if (A.VictoryPoints == B.VictoryPoints)
			{
				return 0;
			} else if (A.VictoryPoints < B.VictoryPoints)
			{
				return 1;
			}
			else
			{
				return -1;
			}
		}
		
		public static IMyEntity FindLookAtEntity(IMyControllableEntity controlledEntity, bool findShips, bool findCubes, bool findPlayers, bool findAsteroids, bool findPlanets, bool findReplicable, bool ignoreProjection)
        {
            IMyEntity entity;
            double distance;
            Vector3D hitPoint;
            FindLookAtEntity(controlledEntity, true, ignoreProjection, out entity, out distance, out hitPoint, findShips, findCubes, findPlayers, findAsteroids, findPlanets, findReplicable);
            return entity;
        }

        public static void FindLookAtEntity(IMyControllableEntity controlledEntity, bool ignoreOccupiedGrid, bool ignoreProjection, out IMyEntity lookEntity, out double lookDistance, out Vector3D hitPoint, bool findShips, bool findCubes, bool findPlayers, bool findAsteroids, bool findPlanets, bool findReplicable)
        {
            const float range = 5000000;
            Matrix worldMatrix;
            Vector3D startPosition;
            Vector3D endPosition;
            IMyCubeGrid occupiedGrid = null;

            if (controlledEntity.Entity.Parent == null)
            {
                worldMatrix = controlledEntity.GetHeadMatrix(true, true, false); // dead center of player cross hairs, or the direction the player is looking with ALT.
                startPosition = worldMatrix.Translation + worldMatrix.Forward * 0.5f;
                endPosition = worldMatrix.Translation + worldMatrix.Forward * (range + 0.5f);
            }
            else
            {
                occupiedGrid = controlledEntity.Entity.GetTopMostParent() as IMyCubeGrid;
                worldMatrix = controlledEntity.Entity.WorldMatrix;
                // TODO: need to adjust for position of cockpit within ship.
                startPosition = worldMatrix.Translation + worldMatrix.Forward * 1.5f;
                endPosition = worldMatrix.Translation + worldMatrix.Forward * (range + 1.5f);
            }

            var entites = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entites, e => e != null);

            var list = new Dictionary<IMyEntity, double>();
            var ray = new RayD(startPosition, worldMatrix.Forward);

            foreach (var entity in entites)
            {
                if (findShips || findCubes)
                {
                    var cubeGrid = entity as IMyCubeGrid;

                    if (cubeGrid != null)
                    {
                        if (ignoreOccupiedGrid && occupiedGrid != null && occupiedGrid.EntityId == cubeGrid.EntityId)
                            continue;

                        // Will ignore Projected grids, new grid/cube placement, and grids in middle of copy/paste.
                        if (ignoreProjection && cubeGrid.Physics == null)
                            continue;

                        // check if the ray comes anywhere near the Grid before continuing.    
                        if (ray.Intersects(entity.WorldAABB).HasValue)
                        {
                            var hit = cubeGrid.RayCastBlocks(startPosition, endPosition);
                            if (hit.HasValue)
                            {
                                var distance = (startPosition - cubeGrid.GridIntegerToWorld(hit.Value)).Length();
                                var block = cubeGrid.GetCubeBlock(hit.Value);

                                if (block.FatBlock != null && findCubes)
                                    list.Add(block.FatBlock, distance);
                                else if (findShips)
                                    list.Add(entity, distance);
                            }
                        }
                    }
                }

                if (findPlayers)
                {
                    var controller = entity as IMyControllableEntity;
                    if (controlledEntity.Entity.EntityId != entity.EntityId && controller != null && ray.Intersects(entity.WorldAABB).HasValue)
                    {
                        var distance = (startPosition - entity.GetPosition()).Length();
                        list.Add(entity, distance);
                    }
                }

                if (findReplicable)
                {
                    var replicable = entity as Sandbox.Game.Entities.MyInventoryBagEntity;
                    if (replicable != null && ray.Intersects(entity.WorldAABB).HasValue)
                    {
                        var distance = (startPosition - entity.GetPosition()).Length();
                        list.Add(entity, distance);
                    }
                }

                if (findAsteroids)
                {
                    var voxelMap = entity as IMyVoxelMap;
                    if (voxelMap != null)
                    {
                        var aabb = new BoundingBoxD(voxelMap.PositionLeftBottomCorner, voxelMap.PositionLeftBottomCorner + voxelMap.Storage.Size);
                        var hit = ray.Intersects(aabb);
                        if (hit.HasValue)
                        {
                            var center = voxelMap.PositionLeftBottomCorner + (voxelMap.Storage.Size / 2);
                            var distance = (startPosition - center).Length();  // use distance to center of asteroid.
                            list.Add(entity, distance);
                        }
                    }
                }

                if (findPlanets)
                {
                    // Looks to be working against Git and public release.
                    var planet = entity as Sandbox.Game.Entities.MyPlanet;
                    if (planet != null)
                    {
                        var aabb = new BoundingBoxD(planet.PositionLeftBottomCorner, planet.PositionLeftBottomCorner + planet.Size);
                        var hit = ray.Intersects(aabb);
                        if (hit.HasValue)
                        {
                            var center = planet.WorldMatrix.Translation;
                            var distance = (startPosition - center).Length(); // use distance to center of planet.
                            list.Add(entity, distance);
                        }
                    }
                }
            }

            if (list.Count == 0)
            {
                lookEntity = null;
                lookDistance = 0;
                hitPoint = Vector3D.Zero;
                return;
            }

            // find the closest Entity.
            var item = list.OrderBy(f => f.Value).First();
            lookEntity = item.Key;
            lookDistance = item.Value;
            hitPoint = startPosition + (Vector3D.Normalize(ray.Direction) * lookDistance);
        }
		
	}
    public class ConquestGrid
    {
        public bool IsValid;
        IMyCubeGrid Grid;
        public Vector3D Position;
        public float Radius;
        public List<string> Reasons = new List<string>();
        public List<IMyCargoContainer> Containers = new List<IMyCargoContainer>();
        public ConquestGrid(IMyCubeGrid grid)
        {
            Grid = grid;
            IsValid = IsConquest();
        }
        private bool IsConquest()
        {
            bool ConquestBeacon = false;
            bool ConquestAntenna = false;
            bool ConquestAssembler = false;
            bool ConquestRefinery = false;
            bool ConquestCargo = false;
            long OwnerFaction;
            try
            {
                IMyFaction Faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(Grid.BigOwners[0]);
                if (Faction.IsEveryoneNpc())
                {
                    Reasons.Add("Grid does not belong to a player faction.");
                    return false;
                }
                OwnerFaction = Faction.FactionId;
            }
            catch
            {
                Reasons.Add("Could not determine owner or owner's faction (check ownership).");
                return false;
            }
            // Check that this grid is a station (consequently not small grid) OR that static grid isnt required to be conquest base
            if (Grid.IsStatic || !ConquestScript.Instance.Config.StaticReq)
            {
                List<IMySlimBlock> SlimBlocks = new List<IMySlimBlock>();

                Grid.GetBlocks(SlimBlocks);
                foreach (IMySlimBlock SlimBlock in SlimBlocks)
                {
                    try
                    {
                        IMyCubeBlock FatBlock = SlimBlock.FatBlock;
                        if (ConquestScript.Instance.Config.Antenna && FatBlock is IMyRadioAntenna)
                        {
                            IMyRadioAntenna Antenna = FatBlock as IMyRadioAntenna;
                            if ((Math.Ceiling(Antenna.Radius) >= ConquestScript.Instance.Config.BeaconDistance) && (Antenna.IsWorking) && (Antenna.IsBroadcasting))
                            {
                                if (Antenna.Radius > Radius)
                                {
                                    Position = FatBlock.GetPosition();
                                    Radius = Antenna.Radius;
                                    ConquestAntenna = true;
                                    ConquestBeacon = false;
                                }                                 
                                try
                                {
                                    if (OwnerFaction != MyAPIGateway.Session.Factions.TryGetPlayerFaction(Antenna.OwnerId).FactionId)
                                    {
                                        Reasons.Add("Antenna owner must be the same as the majority owner of grid.");
                                        return false;
                                    }
                                }
                                catch
                                {
                                    Reasons.Add("Could not determine owner or owner's faction.");
                                    return false;
                                }
                            }
                        }
                        else if (FatBlock is IMyBeacon)
                        {
                            IMyBeacon Beacon = FatBlock as IMyBeacon;
                            if ((Math.Ceiling(Beacon.Radius) >= ConquestScript.Instance.Config.BeaconDistance) && (Beacon.IsWorking))
                            {
                                if (Beacon.Radius > Radius)
                                {
                                    Position = FatBlock.GetPosition();
                                    Radius = Beacon.Radius;
                                    ConquestAntenna = false;
                                    ConquestBeacon = true;
                                }
                                try
                                {
                                    if (OwnerFaction != MyAPIGateway.Session.Factions.TryGetPlayerFaction(Beacon.OwnerId).FactionId)
                                    {
                                        Reasons.Add("Beacon owner must be the same as the majority owner of grid.");
                                        return false;
                                    }
                                }
                                catch
                                {
                                    Reasons.Add("Could not determine owner or owner's faction.");
                                    return false;
                                }
                            }
                        }                        
                        else if ((FatBlock is IMyAssembler) && FatBlock.IsFunctional)
                        {
                            ConquestAssembler = true;
                        }
                        else if ((FatBlock is IMyRefinery) && FatBlock.IsFunctional)
                        {
                            ConquestRefinery = true;
                        }
                        else if ((FatBlock is IMyCargoContainer) && FatBlock.IsFunctional)
                        {
                            Containers.Add((IMyCargoContainer)FatBlock);
                            ConquestCargo = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        ConquestScript.Instance.ClientLogger.WriteException(ex);
                        MyAPIGateway.Utilities.ShowMessage("Error", "An exception has been logged in the file: {0}" + ConquestScript.Instance.ClientLogger.LogFileName);
                    }
                }
            }
            else
            {
                Reasons.Add("Grid must be static.");
                return false;
            }

            if ((ConquestBeacon || ConquestAntenna) && (ConquestAssembler || !ConquestScript.Instance.Config.AssemblerReq) && (ConquestRefinery || !ConquestScript.Instance.Config.RefineryReq) && (ConquestCargo || !ConquestScript.Instance.Config.CargoReq))
            {
                return true;
            }
            else
            {
                if (!ConquestBeacon && !ConquestScript.Instance.Config.Antenna)
                {
                    Reasons.Add("Grid must contain beacon broadcasting at " + ConquestScript.Instance.Config.BeaconDistance + "m or more.");
                }
                if (!ConquestBeacon && !ConquestAntenna && ConquestScript.Instance.Config.Antenna)
                {
                    Reasons.Add("Grid must contain beacon or antenna broadcasting at " + ConquestScript.Instance.Config.BeaconDistance + "m or more.");
                }
                if (!ConquestAssembler && ConquestScript.Instance.Config.AssemblerReq)
                {
                    Reasons.Add("Grid must contain a functional assembler.");
                }
                if (!ConquestRefinery && ConquestScript.Instance.Config.RefineryReq)
                {
                    Reasons.Add("Grid must contain a functional refinery.");
                }
                if (!ConquestCargo && ConquestScript.Instance.Config.CargoReq)
                {
                    Reasons.Add("Grid must contain a functional cargo container.");
                }
                return false;
            }
        }

        internal void AddItem(ConqItem item, float modifier)
        {
            foreach (IMyCargoContainer Container in Containers)
            {
                var Entity = Container as VRage.Game.Entity.MyEntity;
                var Inventory = Entity.GetInventory();
                var ObjectBuilder = new MyObjectBuilder_PhysicalObject();
                switch (item.TypeId)
                {
                    case "Ore":
                        ObjectBuilder = new MyObjectBuilder_Ore() { SubtypeName = item.SubTypeName };
                        break;

                    case "Ingot":
                        ObjectBuilder = new MyObjectBuilder_Ingot() { SubtypeName = item.SubTypeName };
                        break;
                }
                if (Inventory.CanItemsBeAdded((VRage.MyFixedPoint)item.Amount*modifier, ObjectBuilder.GetId()))
                {
                    Inventory.AddItems((VRage.MyFixedPoint)item.Amount*modifier, ObjectBuilder);
                    break;
                }
            }
        }
    }
}