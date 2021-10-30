using LiteNetLib.Utils;

namespace MultiplayerARPG.Auction
{
    public struct BidMessage : INetSerializable
    {
        public int auctionId;
        public int price;

        public void Deserialize(NetDataReader reader)
        {
            auctionId = reader.GetPackedInt();
            price = reader.GetPackedShort();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.PutPackedInt(auctionId);
            writer.PutPackedInt(price);
        }
    }
}