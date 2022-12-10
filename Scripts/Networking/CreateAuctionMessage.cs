using LiteNetLib.Utils;

namespace MultiplayerARPG.Auction
{
    public struct CreateAuctionMessage : INetSerializable
    {
        public int indexOfItem;
        public short amount;
        public int startPrice;
        public int buyoutPrice;
        public int durationOption;

        public void Deserialize(NetDataReader reader)
        {
            indexOfItem = reader.GetPackedInt();
            amount = reader.GetPackedShort();
            startPrice = reader.GetPackedInt();
            buyoutPrice = reader.GetPackedInt();
            durationOption = reader.GetPackedInt();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.PutPackedInt(indexOfItem);
            writer.PutPackedShort(amount);
            writer.PutPackedInt(startPrice);
            writer.PutPackedInt(buyoutPrice);
            writer.PutPackedInt(durationOption);
        }
    }
}
