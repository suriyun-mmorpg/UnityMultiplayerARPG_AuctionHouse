using LiteNetLib.Utils;

namespace MultiplayerARPG.Auction
{
    public struct ResponseClientConfigMessage : INetSerializable
    {
        public UITextKeys message;
        public string serviceUrl;
        public string accessToken;

        public void Deserialize(NetDataReader reader)
        {
            message = (UITextKeys)reader.GetPackedUShort();
            serviceUrl = reader.GetString();
            accessToken = reader.GetString();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.PutPackedUShort((ushort)message);
            writer.Put(serviceUrl);
            writer.Put(accessToken);
        }
    }
}
