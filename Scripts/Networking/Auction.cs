namespace MultiplayerARPG.Auction
{
    [System.Serializable]
    public struct Auction
    {
        public int id;
        public int buyoutPrice;
        public int bidPrice;
        public string sellerId;
        public string sellerName;
        public long timeLeft;
    }
}
