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
	using Conquest.scripts.Management;

    [ProtoContract]
    public class MessageConqLeaderboard : MessageBase
    {

        public override void ProcessClient()
        {
            // never processed on client
        }

        public override void ProcessServer()
        {
			ConquestScript.Instance.DataLock.AcquireExclusive();	
			string Title = "Conquest Faction Leaderboard";
			string Prefix = "Points last updated " + (MyAPIGateway.Session.GameDateTime-ConquestScript.Instance.Data.LastRun).Minutes.ToString() + " minutes ago.";
			string Content = " Tag   Faction Name  Points  Base type count";
			
		
			foreach (ConquestFaction Faction in ConquestScript.Instance.Data.ConquestFactions)
			{			
				Content += string.Format("\n {0}   {1}  {2}     ", Faction.FactionTag, Faction.FactionName, Faction.VictoryPoints.ToString());
				if (ConquestScript.Instance.Config.PlanetPoints > 0)
					Content += string.Format("P:{0} ", Faction.PlanetBases.ToString());
				if (ConquestScript.Instance.Config.MoonPoints > 0)
					Content += string.Format("M:{0} ", Faction.MoonBases.ToString());
				if (ConquestScript.Instance.Config.AsteroidPoints > 0)
					Content += string.Format("A:{0} ", Faction.AsteroidBases.ToString());
			}
			ConquestScript.Instance.DataLock.ReleaseExclusive();			
			MessageClientDialogMessage.SendMessage(SenderSteamId, Title, Prefix, Content);		
        }

        public static void SendMessage()
        {
            ConnectionHelper.SendMessageToServer(new MessageConqLeaderboard());
        }
    }
}
