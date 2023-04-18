using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Serialization;
using UnityRestClient;

namespace MultiplayerARPG.Auction
{
    public class AuctionRestClient : RestClient
    {
        [FormerlySerializedAs("url")]
        public string apiUrl;
        [FormerlySerializedAs("accessToken")]
        public string secretKey;

        public Task<Result<AuctionListResponse>> GetAuctionList(int limit = 20, int page = 1)
        {
            Dictionary<string, object> queries = new Dictionary<string, object>();
            queries[nameof(limit)] = limit;
            queries[nameof(page)] = page;
            return Get<AuctionListResponse>(GetUrl(apiUrl, "/"), queries, secretKey);
        }

        public Task<Result<AuctionListResponse>> GetSellHistoryList(int limit = 20, int page = 1)
        {
            Dictionary<string, object> queries = new Dictionary<string, object>();
            queries[nameof(limit)] = limit;
            queries[nameof(page)] = page;
            return Get<AuctionListResponse>(GetUrl(apiUrl, "/sell-history"), queries, secretKey);
        }

        public Task<Result<AuctionListResponse>> GetBuyHistoryList(int limit = 20, int page = 1)
        {
            Dictionary<string, object> queries = new Dictionary<string, object>();
            queries[nameof(limit)] = limit;
            queries[nameof(page)] = page;
            return Get<AuctionListResponse>(GetUrl(apiUrl, "/buy-history"), queries, secretKey);
        }

        public Task<Result<AuctionData>> GetAuction(int id)
        {
            Dictionary<string, object> queries = new Dictionary<string, object>();
            return Get<AuctionData>(GetUrl(apiUrl, $"/{id}"), queries, secretKey);
        }

        public Task<Result<Dictionary<string, string>>> GetAccessToken(string userId)
        {
            Dictionary<string, object> queries = new Dictionary<string, object>();
            queries[nameof(userId)] = userId;
            return Get<Dictionary<string, string>>(GetUrl(apiUrl, "/internal/access-token"), queries, secretKey);
        }

        public Task<Result> CreateAuction(string itemData, string metaName, int metaLevel, int startPrice, int buyoutPrice, string sellerId, string sellerName, int durationOption)
        {
            Dictionary<string, object> form = new Dictionary<string, object>
            {
                { nameof(itemData), itemData },
                { nameof(metaName), metaName },
                { nameof(metaLevel), metaLevel },
                { nameof(startPrice), startPrice },
                { nameof(buyoutPrice), buyoutPrice },
                { nameof(sellerId), sellerId },
                { nameof(sellerName), sellerName },
                { nameof(durationOption), durationOption }
            };
            return Post(GetUrl(apiUrl, "/internal/auction"), form, secretKey);
        }

        public Task<Result> Bid(string userId, string characterName, int id, int price)
        {
            Dictionary<string, object> form = new Dictionary<string, object>
            {
                { nameof(userId), userId },
                { nameof(characterName), characterName },
                { nameof(id), id },
                { nameof(price), price }
            };
            return Post(GetUrl(apiUrl, "/internal/bid"), form, secretKey);
        }

        public Task<Result> Buyout(string userId, string characterName, int id)
        {
            Dictionary<string, object> form = new Dictionary<string, object>
            {
                { nameof(userId), userId },
                { nameof(characterName), characterName },
                { nameof(id), id }
            };
            return Post(GetUrl(apiUrl, "/internal/buyout"), form, secretKey);
        }

        public Task<Result> CancelAuction(string userId, int id)
        {
            Dictionary<string, object> form = new Dictionary<string, object>
            {
                { nameof(userId), userId },
                { nameof(id), id }
            };
            return Post(GetUrl(apiUrl, "/internal/cancel-auction"), form, secretKey);
        }

        public Task<Result<DurationOptionsResponse>> GetDurationOptions()
        {
            return Get<DurationOptionsResponse>(GetUrl(apiUrl, "/duration-options"));
        }
    }
}
