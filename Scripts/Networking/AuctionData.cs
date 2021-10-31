namespace MultiplayerARPG.Auction
{
    [System.Serializable]
    public struct AuctionData
    {
        public int id;
        public int buyoutPrice;
        public int bidPrice;
        public string sellerId;
        public string sellerName;
        public long timeLeft;
        public bool isEnd;
        public bool isBuyout;
        public string buyerId;
        public string buyerName;
    }
}
