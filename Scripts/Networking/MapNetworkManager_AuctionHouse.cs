using Cysharp.Threading.Tasks;
using LiteNetLibManager;
using MultiplayerARPG.Auction;
using System.Collections.Generic;
using UnityEngine;
using UnityRestClient;

namespace MultiplayerARPG.MMO
{
    public partial class MapNetworkManager
    {
        [System.Serializable]
        public class AuctionHouseMessageTypes
        {
            public ushort createAuctionRequestType;
            public ushort bidRequestType;
            public ushort buyoutRequestType;
            public ushort getClientConfigRequestType;
        }

        /*
         * Do `Item Listing`, `Sell History`, `Bid Listing`, `Bidding`, `Buying` directly with service
         */
        [Header("Auction House")]
        public AuctionHouseMessageTypes auctionHouseMessageTypes = new AuctionHouseMessageTypes()
        {
            createAuctionRequestType = 1300,
            bidRequestType = 1301,
            buyoutRequestType = 1302,
            getClientConfigRequestType = 1303,
        };
        public string auctionHouseServiceUrl = "http://localhost:9800";
        public string auctionHouseSecretKey = "secret";

        public AuctionRestClient AuctionRestClientForClient { get; private set; } = new AuctionRestClient();
        public AuctionRestClient AuctionRestClientForServer { get; private set; } = new AuctionRestClient();

        [DevExtMethods("RegisterMessages")]
        private void RegisterMessages_AuctionHouse()
        {
            RegisterRequestToServer<CreateAuctionMessage, ResponseCreateAuctionMessage>(auctionHouseMessageTypes.createAuctionRequestType, HandleCreateAuctionAtServer);
            RegisterRequestToServer<BidMessage, ResponseBidMessage>(auctionHouseMessageTypes.bidRequestType, HandleBidAtServer);
            RegisterRequestToServer<BuyoutMessage, ResponseBuyoutMessage>(auctionHouseMessageTypes.buyoutRequestType, HandleBuyoutAtServer);
            RegisterRequestToServer<EmptyMessage, ResponseClientConfigMessage>(auctionHouseMessageTypes.getClientConfigRequestType, HandleGetClientConfigAtServer);
        }

        [DevExtMethods("OnStartServer")]
        private void OnStartServer_AuctionHouse()
        {
            AuctionRestClientForServer.url = auctionHouseServiceUrl;
            AuctionRestClientForServer.accessToken = auctionHouseSecretKey;
        }

        public void CreateAuction(CreateAuctionMessage createAuction, ResponseDelegate<ResponseCreateAuctionMessage> callback)
        {
            if (!IsClientConnected)
                return;
            // Send create auction message to server
            ClientSendRequest(auctionHouseMessageTypes.createAuctionRequestType, createAuction, callback);
        }

        private async UniTaskVoid HandleCreateAuctionAtServer(RequestHandlerData requestHandler, CreateAuctionMessage request,
            RequestProceedResultDelegate<ResponseCreateAuctionMessage> result)
        {
            IPlayerCharacterData playerCharacterData;
            if (!ServerUserHandlers.TryGetPlayerCharacter(requestHandler.ConnectionId, out playerCharacterData))
            {
                // Do nothing, player character is not enter the game yet.
                result.Invoke(AckResponseCode.Error, new ResponseCreateAuctionMessage()
                {
                    message = UITextKeys.UI_ERROR_NOT_LOGGED_IN,
                });
                return;
            }
            if (request.amount <= 0)
                request.amount = 1;
            // Reduce gold by create auction price
            RestClient.Result<DurationOptionsResponse> durationOptionsResult = await AuctionRestClientForServer.GetDurationOptions();
            if (durationOptionsResult.IsNetworkError || durationOptionsResult.IsHttpError)
            {
                result.Invoke(AckResponseCode.Error, new ResponseCreateAuctionMessage()
                {
                    message = UITextKeys.UI_ERROR_CONTENT_NOT_AVAILABLE,
                });
                return;
            }
            int createAuctionPrice = durationOptionsResult.Content.durationOptions[request.durationOption].price;
            if (playerCharacterData.Gold < createAuctionPrice)
            {
                result.Invoke(AckResponseCode.Error, new ResponseCreateAuctionMessage()
                {
                    message = UITextKeys.UI_ERROR_NOT_ENOUGH_GOLD,
                });
                return;
            }
            // Require index of non equip items, amount, starting auction price, buyout price (optional, 0 = no buyout)
            // Check player's item, then tell the service to add to bidding list, and remove it from inventory
            if (request.indexOfItem >= playerCharacterData.NonEquipItems.Count ||
                playerCharacterData.NonEquipItems[request.indexOfItem].amount < request.amount)
            {
                // Do nothing, wrong index of item or item amount is over than it has
                result.Invoke(AckResponseCode.Error, new ResponseCreateAuctionMessage()
                {
                    message = UITextKeys.UI_ERROR_INVALID_ITEM_INDEX,
                });
                return;
            }
            // Tell the service to add to bidding list
            CharacterItem clonedItemForAmountChanging = playerCharacterData.NonEquipItems[request.indexOfItem].Clone();
            clonedItemForAmountChanging.amount = request.amount;
            Mail mail = new Mail();
            mail.Items.Add(clonedItemForAmountChanging);
            RestClient.Result createResult = await AuctionRestClientForServer.CreateAuction(
                mail.WriteItems(),
                clonedItemForAmountChanging.GetItem().DefaultTitle,
                clonedItemForAmountChanging.level,
                request.startPrice,
                request.buyoutPrice,
                playerCharacterData.UserId,
                playerCharacterData.CharacterName,
                request.durationOption);
            if (createResult.IsNetworkError || createResult.IsHttpError)
            {
                result.Invoke(AckResponseCode.Error, new ResponseCreateAuctionMessage()
                {
                    message = UITextKeys.UI_ERROR_INTERNAL_SERVER_ERROR,
                });
                return;
            }
            // Remove item from inventory
            playerCharacterData.DecreaseItemsByIndex(request.indexOfItem, request.amount);
            playerCharacterData.Gold -= createAuctionPrice;
            result.Invoke(AckResponseCode.Success, new ResponseCreateAuctionMessage());
        }

        public void Bid(BidMessage bid, ResponseDelegate<ResponseBidMessage> callback)
        {
            if (!IsClientConnected)
                return;
            // Send create auction message to server
            ClientSendRequest(auctionHouseMessageTypes.bidRequestType, bid, callback);
        }

        private async UniTaskVoid HandleBidAtServer(RequestHandlerData requestHandler, BidMessage request,
            RequestProceedResultDelegate<ResponseBidMessage> result)
        {
            IPlayerCharacterData playerCharacterData;
            if (!ServerUserHandlers.TryGetPlayerCharacter(requestHandler.ConnectionId, out playerCharacterData))
            {
                // Do nothing, player character is not enter the game yet.
                result.Invoke(AckResponseCode.Error, new ResponseBidMessage()
                {
                    message = UITextKeys.UI_ERROR_NOT_LOGGED_IN,
                });
                return;
            }
            // Get highest bidding price from service
            RestClient.Result<AuctionData> getResult = await AuctionRestClientForServer.GetAuction(request.auctionId);
            if (getResult.IsNetworkError || getResult.IsHttpError)
            {
                result.Invoke(AckResponseCode.Error, new ResponseBidMessage()
                {
                    message = UITextKeys.UI_ERROR_CONTENT_NOT_AVAILABLE,
                });
                return;
            }
            // Seller cannot bid
            if (playerCharacterData.UserId.Equals(getResult.Content.sellerId))
            {
                result.Invoke(AckResponseCode.Error, new ResponseBidMessage()
                {
                    message = UITextKeys.UI_ERROR_NOT_ALLOWED,
                });
                return;
            }
            // Validate gold
            if (request.price <= getResult.Content.bidPrice)
            {
                result.Invoke(AckResponseCode.Error, new ResponseBidMessage()
                {
                    message = UITextKeys.UI_ERROR_NOT_ENOUGH_GOLD,
                });
                return;
            }
            // Validate buyout price
            if (getResult.Content.buyoutPrice > 0 && request.price >= getResult.Content.buyoutPrice)
            {
                result.Invoke(AckResponseCode.Error, new ResponseBidMessage()
                {
                    message = UITextKeys.UI_ERROR_BAD_REQUEST,
                });
                return;
            }
            if (playerCharacterData.Gold < getResult.Content.bidPrice)
            {
                ServerGameMessageHandlers.SendGameMessage(requestHandler.ConnectionId, UITextKeys.UI_ERROR_NOT_ENOUGH_GOLD);
                result.Invoke(AckResponseCode.Error, new ResponseBidMessage()
                {
                    message = UITextKeys.UI_ERROR_NOT_ENOUGH_GOLD,
                });
                return;
            }
            // Tell the service to add to bid
            RestClient.Result bidResult = await AuctionRestClientForServer.Bid(playerCharacterData.UserId, playerCharacterData.CharacterName, request.auctionId, request.price);
            if (bidResult.IsNetworkError || bidResult.IsHttpError)
            {
                ServerGameMessageHandlers.SendGameMessage(requestHandler.ConnectionId, UITextKeys.UI_ERROR_INTERNAL_SERVER_ERROR);
                return;
            }
            // Reduce gold
            playerCharacterData.Gold -= request.price;
            result.Invoke(AckResponseCode.Success, new ResponseBidMessage());
        }

        public void Buyout(BuyoutMessage buyout, ResponseDelegate<ResponseBuyoutMessage> callback)
        {
            if (!IsClientConnected)
                return;
            // Send create auction message to server
            ClientSendRequest(auctionHouseMessageTypes.buyoutRequestType, buyout, callback);
        }

        private async UniTaskVoid HandleBuyoutAtServer(RequestHandlerData requestHandler, BuyoutMessage request,
            RequestProceedResultDelegate<ResponseBuyoutMessage> result)
        {
            IPlayerCharacterData playerCharacterData;
            if (!ServerUserHandlers.TryGetPlayerCharacter(requestHandler.ConnectionId, out playerCharacterData))
            {
                // Do nothing, player character is not enter the game yet.
                result.Invoke(AckResponseCode.Error, new ResponseBuyoutMessage()
                {
                    message = UITextKeys.UI_ERROR_NOT_LOGGED_IN,
                });
                return;
            }
            // Get buyout price from service
            RestClient.Result<AuctionData> getResult = await AuctionRestClientForServer.GetAuction(request.auctionId);
            if (getResult.IsNetworkError || getResult.IsHttpError)
            {
                result.Invoke(AckResponseCode.Error, new ResponseBuyoutMessage()
                {
                    message = UITextKeys.UI_ERROR_CONTENT_NOT_AVAILABLE,
                });
                return;
            }
            // Seller cannot bid
            if (playerCharacterData.UserId.Equals(getResult.Content.sellerId))
            {
                result.Invoke(AckResponseCode.Error, new ResponseBuyoutMessage()
                {
                    message = UITextKeys.UI_ERROR_NOT_ALLOWED,
                });
                return;
            }
            // Validate buyout price
            if (getResult.Content.buyoutPrice <= 0)
            {
                result.Invoke(AckResponseCode.Error, new ResponseBuyoutMessage()
                {
                    message = UITextKeys.UI_ERROR_NOT_ALLOWED,
                });
                return;
            }
            int price = getResult.Content.buyoutPrice;
            // Validate gold
            if (playerCharacterData.Gold < getResult.Content.buyoutPrice)
            {
                ServerGameMessageHandlers.SendGameMessage(requestHandler.ConnectionId, UITextKeys.UI_ERROR_NOT_ENOUGH_GOLD);
                return;
            }
            // Tell the service to add to buyout
            RestClient.Result buyoutResult = await AuctionRestClientForServer.Buyout(playerCharacterData.UserId, playerCharacterData.CharacterName, request.auctionId);
            if (buyoutResult.IsNetworkError || buyoutResult.IsHttpError)
            {
                ServerGameMessageHandlers.SendGameMessage(requestHandler.ConnectionId, UITextKeys.UI_ERROR_INTERNAL_SERVER_ERROR);
                return;
            }
            // Reduce gold
            playerCharacterData.Gold -= price;
            result.Invoke(AckResponseCode.Success, new ResponseBuyoutMessage());
        }

        public void GetAccessToken(ResponseDelegate<ResponseClientConfigMessage> callback)
        {
            if (!IsClientConnected)
                return;
            // Send create auction message to server
            ClientSendRequest(auctionHouseMessageTypes.getClientConfigRequestType, EmptyMessage.Value, callback);
        }

        private async UniTaskVoid HandleGetClientConfigAtServer(RequestHandlerData requestHandler, EmptyMessage request,
            RequestProceedResultDelegate<ResponseClientConfigMessage> result)
        {
            IPlayerCharacterData playerCharacterData;
            if (!ServerUserHandlers.TryGetPlayerCharacter(requestHandler.ConnectionId, out playerCharacterData))
            {
                // Do nothing, player character is not enter the game yet.
                result.Invoke(AckResponseCode.Error, new ResponseClientConfigMessage()
                {
                    message = UITextKeys.UI_ERROR_NOT_LOGGED_IN,
                });
                return;
            }
            RestClient.Result<Dictionary<string, string>> getAccessTokenResult = await AuctionRestClientForServer.GetAccessToken(playerCharacterData.UserId);
            if (getAccessTokenResult.IsNetworkError || getAccessTokenResult.IsHttpError)
            {
                result.Invoke(AckResponseCode.Error, new ResponseClientConfigMessage()
                {
                    message = UITextKeys.UI_ERROR_INTERNAL_SERVER_ERROR,
                });
                return;
            }
            result.Invoke(AckResponseCode.Success, new ResponseClientConfigMessage()
            {
                serviceUrl = auctionHouseServiceUrl,
                accessToken = getAccessTokenResult.Content["accessToken"]
            });
        }
    }
}
