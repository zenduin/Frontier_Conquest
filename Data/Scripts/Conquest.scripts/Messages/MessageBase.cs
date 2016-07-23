namespace Conquest.scripts.Messages
{
    using System;
    using System.Xml.Serialization;
    using ProtoBuf;

    // ALL CLASSES DERIVED FROM MessageBase MUST BE ADDED HERE
    [XmlInclude(typeof(MessageIncomingMessageParts))]
    [XmlInclude(typeof(MessageClientDialogMessage))]
    [XmlInclude(typeof(MessageClientTextMessage))]
    [XmlInclude(typeof(MessageConqBase))]	
	[XmlInclude(typeof(MessageConqBasePos))]
	[XmlInclude(typeof(MessageConqLeaderboard))]
	[XmlInclude(typeof(MessageConqConfig))]
	[XmlInclude(typeof(MessageConqExclude))]
	[XmlInclude(typeof(MessageConqReset))]
    [ProtoContract]
    public abstract class MessageBase
    {
        /// <summary>
        /// The SteamId of the message's sender. Note that this will be set when the message is sent, so there is no need for setting it otherwise.
        /// </summary>
        [ProtoMember(1)]
        public ulong SenderSteamId;

        /// <summary>
        /// The display name of the message sender.
        /// </summary>
        [ProtoMember(2)]
        public string SenderDisplayName;

        /// <summary>
        /// The current display language of the sender.
        /// </summary>
        [ProtoMember(3)]
        public int SenderLanguage;

        /// <summary>
        /// Defines on which side the message should be processed. Note that this will be set when the message is sent, so there is no need for setting it otherwise.
        /// </summary>
        [ProtoMember(4)]
        public MessageSide Side;

        /*
        [ProtoAfterDeserialization]
        void InvokeProcessing() // is not invoked after deserialization from xml
        {
            ConquestScript.Instance.ServerLogger.Write("START - Processing");
            switch (Side)
            {
                case MessageSide.ClientSide:
                    ProcessClient();
                    break;
                case MessageSide.ServerSide:
                    ProcessServer();
                    break;
            }
            ConquestScript.Instance.ServerLogger.Write("END - Processing");
        }
        */

        public void InvokeProcessing()
        {
            switch (Side)
            {
                case MessageSide.ClientSide:
                    InvokeClientProcessing();
                    break;
                case MessageSide.ServerSide:
                    InvokeServerProcessing();
                    break;
            }
        }

        private void InvokeClientProcessing()
        {
            ConquestScript.Instance.ClientLogger.WriteVerbose("Received - {0}", this.GetType().Name);
            try
            {
                ProcessClient();
            }
            catch (Exception ex)
            {
                // TODO: send error to server and notify admins
                ConquestScript.Instance.ClientLogger.WriteException(ex);
            }
        }

        private void InvokeServerProcessing()
        {
            ConquestScript.Instance.ServerLogger.WriteVerbose("Received - {0}", this.GetType().Name);
            try
            {
                ProcessServer();
            }
            catch (Exception ex)
            {
                ConquestScript.Instance.ServerLogger.WriteException(ex);
            }
        }

        public abstract void ProcessClient();
        public abstract void ProcessServer();
    }
}
