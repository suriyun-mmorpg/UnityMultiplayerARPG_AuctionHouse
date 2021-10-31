using System.Collections.Generic;

namespace MultiplayerARPG.Auction
{
    [System.Serializable]
    public class AuctionListResponse
    {
        public List<AuctionData> list = new List<AuctionData>();
        public int limit = 20;
        public int page = 1;
        public int totalPage = 1;
    }
}
