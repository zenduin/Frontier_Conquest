namespace Conquest.scripts.Messages
{
    using Conquest.scripts;
    using ProtoBuf;
    using Sandbox.ModAPI;

    [ProtoContract]
    public class MessageConqReset : MessageBase
    {
        public override void ProcessClient()
        {
            // never processed on client
        }

        public override void ProcessServer()
        {
			var player = MyAPIGateway.Players.FindPlayerBySteamId(SenderSteamId);
			if (player == null || !player.IsAdmin()) // hold on there, are we an admin first?
				return;
			ConquestScript.Instance.ResetData();
        }

        public static void SendMessage()
        {
            ConnectionHelper.SendMessageToServer(new MessageConqReset());
        }
    }
}
