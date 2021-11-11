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
            return Get<AuctionListResponse>(GetUrl(url, "/"), accessToken,
                new KeyValuePair<string, object>(nameof(limit), limit),
                new KeyValuePair<string, object>(nameof(page), page));
        }

        public Task<Result<AuctionListResponse>> GetHistoryList(int limit = 20, int page = 1)
        {
            return Get<AuctionListResponse>(GetUrl(url, "/history"), accessToken,
                new KeyValuePair<string, object>(nameof(limit), limit),
                new KeyValuePair<string, object>(nameof(page), page));
        }

        public Task<Result<AuctionData>> GetAuction(int id)
        {
            return Get<AuctionData>(GetUrl(url, $"/{id}"), accessToken);
        }

        public Task<Result<Dictionary<string, string>>> GetAccessToken(string userId)
        {
            return Get<Dictionary<string, string>>(GetUrl(url, "/internal/access-token"), accessToken, new KeyValuePair<string, object>(nameof(userId), userId));
        }

        public Task<Result> CreateAuction(string itemData, int startPrice, int buyoutPrice, string sellerId, string sellerName, int durationOption)
        {
            Dictionary<string, object> form = new Dictionary<string, object>();
            form.Add(nameof(itemData), itemData);
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
