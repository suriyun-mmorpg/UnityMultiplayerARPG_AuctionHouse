using LiteNetLib.Utils;

namespace MultiplayerARPG
{
    public struct CreateAuctionMessage : INetSerializable
    {
        public int indexOfItem;
        public short amount;
        public int startPrice;
        public int buyoutPrice;

        public void Deserialize(NetDataReader reader)
        {
            indexOfItem = reader.GetPackedInt();
            amount = reader.GetPackedShort();
            startPrice = reader.GetPackedShort();
            buyoutPrice = reader.GetPackedShort();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.PutPackedInt(indexOfItem);
            writer.PutPackedShort(amount);
            writer.PutPackedInt(startPrice);
            writer.PutPackedInt(buyoutPrice);
        }
    }
}
