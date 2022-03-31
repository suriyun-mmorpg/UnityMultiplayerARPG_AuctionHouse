using System.Collections.Generic;
using System.Threading.Tasks;
using UnityRestClient;

namespace MultiplayerARPG.Auction
{
    public class AuctionRestClient : RestClient
    {
        public string url;
        public string accessToken;

        public Task<Result<AuctionListResponse>> GetAuctionList(int limit = 20, int page = 1)
        {
            Dictionary<string, object> queries = new Dictionary<string, object>();
            queries[nameof(limit)] = limit;
            queries[nameof(page)] = page;
            return Get<AuctionListResponse>(GetUrl(url, "/"), queries, accessToken);
        }

        public Task<Result<AuctionListResponse>> GetSellHistoryList(int limit = 20, int page = 1)
        {
            Dictionary<string, object> queries = new Dictionary<string, object>();
            queries[nameof(limit)] = limit;
            queries[nameof(page)] = page;
            return Get<AuctionListResponse>(GetUrl(url, "/sell-history"), queries, accessToken);
        }

        public Task<Result<AuctionListResponse>> GetBuyHistoryList(int limit = 20, int page = 1)
        {
            Dictionary<string, object> queries = new Dictionary<string, object>();
            queries[nameof(limit)] = limit;
            queries[nameof(page)] = page;
            return Get<AuctionListResponse>(GetUrl(url, "/buy-history"), queries, accessToken);
        }

        public Task<Result<AuctionData>> GetAuction(int id)
        {
            Dictionary<string, object> queries = new Dictionary<string, object>();
            return Get<AuctionData>(GetUrl(url, $"/{id}"), queries, accessToken);
        }

        public Task<Result<Dictionary<string, string>>> GetAccessToken(string userId)
        {
            Dictionary<string, object> queries = new Dictionary<string, object>();
            queries[nameof(userId)] = userId;
            return Get<Dictionary<string, string>>(GetUrl(url, "/internal/access-token"), queries, accessToken);
        }

        public Task<Result> CreateAuction(string itemData, string metaName, short metaLevel, int startPrice, int buyoutPrice, string sellerId, string sellerName, int durationOption)
        {
            Dictionary<string, object> form = new Dictionary<string, object>();
            form.Add(nameof(itemData), itemData);
            form.Add(nameof(metaName), metaName);
            form.Add(nameof(metaLevel), metaLevel);
            form.Add(nameof(startPrice), startPrice);
            form.Add(nameof(buyoutPrice), buyoutPrice);
            form.Add(nameof(sellerId), sellerId);
            form.Add(nameof(sellerName), sellerName);
            form.Add(nameof(durationOption), durationOption);
            return Post(GetUrl(url, "/internal/auction"), form, accessToken);
        }

        public Task<Result> Bid(string userId, string characterName, int id, int price)
        {
            Dictionary<string, object> form = new Dictionary<string, object>();
            form.Add(nameof(userId), userId);
            form.Add(nameof(characterName), characterName);
            form.Add(nameof(id), id);
            form.Add(nameof(price), price);
            return Post(GetUrl(url, "/internal/bid"), form, accessToken);
        }

        public Task<Result> Buyout(string userId, string characterName, int id)
        {
            Dictionary<string, object> form = new Dictionary<string, object>();
            form.Add(nameof(userId), userId);
            form.Add(nameof(characterName), characterName);
            form.Add(nameof(id), id);
            return Post(GetUrl(url, "/internal/buyout"), form, accessToken);
        }

        public Task<Result<DurationOptionsResponse>> GetDurationOptions()
        {
            return Get<DurationOptionsResponse>(GetUrl(url, "/duration-options"));
        }
    }
}
