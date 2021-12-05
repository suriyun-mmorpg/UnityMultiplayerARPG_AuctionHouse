using LiteNetLib.Utils;

namespace MultiplayerARPG.Auction
{
    public struct ResponseAccessTokenMessage : INetSerializable
    {
        public UITextKeys message;
        public string accessToken;

        public void Deserialize(NetDataReader reader)
        {
            message = (UITextKeys)reader.GetPackedUShort();
            accessToken = reader.GetString();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.PutPackedUShort((ushort)message);
            writer.Put(accessToken);
        }
    }
}
