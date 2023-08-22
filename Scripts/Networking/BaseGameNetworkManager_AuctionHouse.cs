using Cysharp.Threading.Tasks;
using LiteNetLibManager;
using MultiplayerARPG.Auction;
using System.Collections.Generic;
using UnityEngine;
using UnityRestClient;

namespace MultiplayerARPG
{
    public partial class BaseGameNetworkManager
    {
        [System.Serializable]
        public struct AuctionHouseMessageTypes
        {
            public ushort createAuctionRequestType;
            public ushort bidRequestType;
            public ushort buyoutRequestType;
            public ushort cancelAuctionRequestType;
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
            cancelAuctionRequestType = 1304,
            getClientConfigRequestType = 1303,
        };
        public string auctionHouseServiceUrl = "http://localhost:9800";
        public string auctionHouseServiceUrlForClient = "http://localhost:9800";
        public string auctionHouseSecretKey = "secret";

        private AuctionRestClient _auctionRestClientForClient;
        public AuctionRestClient AuctionRestClientForClient
        {
            get
            {
                if (_auctionRestClientForClient == null)
                    _auctionRestClientForClient = gameObject.AddComponent<AuctionRestClient>();
                return _auctionRestClientForClient;
            }
        }
        private AuctionRestClient _auctionRestClientForServer;
        public AuctionRestClient AuctionRestClientForServer
        {
            get
            {
                if (_auctionRestClientForServer == null)
                    _auctionRestClientForServer = gameObject.AddComponent<AuctionRestClient>();
                return _auctionRestClientForServer;
            }
        }

        [DevExtMethods("RegisterMessages")]
        protected void RegisterMessages_AuctionHouse()
        {
            RegisterRequestToServer<CreateAuctionMessage, ResponseCreateAuctionMessage>(auctionHouseMessageTypes.createAuctionRequestType, HandleCreateAuctionAtServer);
            RegisterRequestToServer<BidMessage, ResponseBidMessage>(auctionHouseMessageTypes.bidRequestType, HandleBidAtServer);
            RegisterRequestToServer<BuyoutMessage, ResponseBuyoutMessage>(auctionHouseMessageTypes.buyoutRequestType, HandleBuyoutAtServer);
            RegisterRequestToServer<CancelAuctionMessage, ResponseCancelAuctionMessage>(auctionHouseMessageTypes.cancelAuctionRequestType, HandleCancelAuctionAtServer);
            RegisterRequestToServer<EmptyMessage, ResponseClientConfigMessage>(auctionHouseMessageTypes.getClientConfigRequestType, HandleGetAuctionClientConfigAtServer);
        }

        [DevExtMethods("OnStartServer")]
        protected void OnStartServer_AuctionHouse()
        {
            AuctionRestClientForServer.apiUrl = auctionHouseServiceUrl;
            AuctionRestClientForServer.secretKey = auctionHouseSecretKey;
        }

        public void CreateAuction(CreateAuctionMessage createAuction, ResponseDelegate<ResponseCreateAuctionMessage> callback)
        {
            if (!IsClientConnected)
                return;
            ClientSendRequest(auctionHouseMessageTypes.createAuctionRequestType, createAuction, callback);
        }

        private async UniTaskVoid HandleCreateAuctionAtServer(RequestHandlerData requestHandler, CreateAuctionMessage request,
            RequestProceedResultDelegate<ResponseCreateAuctionMessage> result)
        {
            if (!ServerUserHandlers.TryGetPlayerCharacter(requestHandler.ConnectionId, out IPlayerCharacterData playerCharacter))
            {
                // Do nothing, player character is not enter the game yet.
                result.InvokeError(new ResponseCreateAuctionMessage()
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
                result.InvokeError(new ResponseCreateAuctionMessage()
                {
                    message = UITextKeys.UI_ERROR_CONTENT_NOT_AVAILABLE,
                });
                return;
            }
            int createAuctionPrice = durationOptionsResult.Content.durationOptions[request.durationOption].price;
            if (playerCharacter.Gold < createAuctionPrice)
            {
                result.InvokeError(new ResponseCreateAuctionMessage()
                {
                    message = UITextKeys.UI_ERROR_NOT_ENOUGH_GOLD,
                });
                return;
            }
            // Require index of non equip items, amount, starting auction price, buyout price (optional, 0 = no buyout)
            // Check player's item, then tell the service to add to bidding list, and remove it from inventory
            if (request.indexOfItem < 0 || request.indexOfItem >= playerCharacter.NonEquipItems.Count)
            {
                result.InvokeError(new ResponseCreateAuctionMessage()
                {
                    message = UITextKeys.UI_ERROR_INVALID_ITEM_INDEX,
                });
                return;
            }
            // Check amount
            if (playerCharacter.NonEquipItems[request.indexOfItem].amount < request.amount)
            {
                result.InvokeError(new ResponseCreateAuctionMessage()
                {
                    message = UITextKeys.UI_ERROR_NOT_ENOUGH_ITEMS,
                });
                return;
            }
            // Tell the service to add to bidding list
            CharacterItem clonedItemForAmountChanging = playerCharacter.NonEquipItems[request.indexOfItem].Clone();
            clonedItemForAmountChanging.amount = request.amount;
            Mail mail = new Mail();
            mail.Items.Add(clonedItemForAmountChanging);
            RestClient.Result createResult = await AuctionRestClientForServer.CreateAuction(
                mail.WriteItems(),
                clonedItemForAmountChanging.GetItem().DefaultTitle,
                clonedItemForAmountChanging.level,
                request.startPrice,
                request.buyoutPrice,
                playerCharacter.UserId,
                playerCharacter.CharacterName,
                request.durationOption);
            if (createResult.IsNetworkError || createResult.IsHttpError)
            {
                result.InvokeError(new ResponseCreateAuctionMessage()
                {
                    message = UITextKeys.UI_ERROR_INTERNAL_SERVER_ERROR,
                });
                return;
            }
            // Remove item from inventory
            playerCharacter.DecreaseItemsByIndex(request.indexOfItem, request.amount, true);
            playerCharacter.Gold -= createAuctionPrice;
            result.InvokeSuccess(new ResponseCreateAuctionMessage());
        }

        public void Bid(BidMessage bid, ResponseDelegate<ResponseBidMessage> callback)
        {
            if (!IsClientConnected)
                return;
            ClientSendRequest(auctionHouseMessageTypes.bidRequestType, bid, callback);
        }

        private async UniTaskVoid HandleBidAtServer(RequestHandlerData requestHandler, BidMessage request,
            RequestProceedResultDelegate<ResponseBidMessage> result)
        {
            if (!ServerUserHandlers.TryGetPlayerCharacter(requestHandler.ConnectionId, out IPlayerCharacterData playerCharacter))
            {
                // Do nothing, player character is not enter the game yet.
                result.InvokeError(new ResponseBidMessage()
                {
                    message = UITextKeys.UI_ERROR_NOT_LOGGED_IN,
                });
                return;
            }
            // Get highest bidding price from service
            RestClient.Result<AuctionData> getResult = await AuctionRestClientForServer.GetAuction(request.auctionId);
            if (getResult.IsNetworkError || getResult.IsHttpError)
            {
                result.InvokeError(new ResponseBidMessage()
                {
                    message = UITextKeys.UI_ERROR_CONTENT_NOT_AVAILABLE,
                });
                return;
            }
            // Seller cannot bid
            if (playerCharacter.UserId.Equals(getResult.Content.sellerId))
            {
                result.InvokeError(new ResponseBidMessage()
                {
                    message = UITextKeys.UI_ERROR_NOT_ALLOWED,
                });
                return;
            }
            // Bidder cannot over bid themself
            if (playerCharacter.UserId.Equals(getResult.Content.buyerId))
            {
                result.InvokeError(new ResponseBidMessage()
                {
                    message = UITextKeys.UI_ERROR_NOT_ALLOWED,
                });
                return;
            }
            // Validate gold
            if (request.price <= getResult.Content.bidPrice)
            {
                result.InvokeError(new ResponseBidMessage()
                {
                    message = UITextKeys.UI_ERROR_NOT_ENOUGH_GOLD,
                });
                return;
            }
            // Validate buyout price
            if (getResult.Content.buyoutPrice > 0 && request.price >= getResult.Content.buyoutPrice)
            {
                result.InvokeError(new ResponseBidMessage()
                {
                    message = UITextKeys.UI_ERROR_BAD_REQUEST,
                });
                return;
            }
            if (playerCharacter.Gold < request.price)
            {
                result.InvokeError(new ResponseBidMessage()
                {
                    message = UITextKeys.UI_ERROR_NOT_ENOUGH_GOLD,
                });
                return;
            }
            // Tell the service to add to bid
            RestClient.Result bidResult = await AuctionRestClientForServer.Bid(playerCharacter.UserId, playerCharacter.CharacterName, request.auctionId, request.price);
            if (bidResult.IsNetworkError || bidResult.IsHttpError)
            {
                result.InvokeError(new ResponseBidMessage()
                {
                    message = UITextKeys.UI_ERROR_INTERNAL_SERVER_ERROR,
                });
                return;
            }
            // Reduce gold
            playerCharacter.Gold -= request.price;
            result.InvokeSuccess(new ResponseBidMessage());
        }

        public void Buyout(BuyoutMessage buyout, ResponseDelegate<ResponseBuyoutMessage> callback)
        {
            if (!IsClientConnected)
                return;
            ClientSendRequest(auctionHouseMessageTypes.buyoutRequestType, buyout, callback);
        }

        private async UniTaskVoid HandleBuyoutAtServer(RequestHandlerData requestHandler, BuyoutMessage request,
            RequestProceedResultDelegate<ResponseBuyoutMessage> result)
        {
            if (!ServerUserHandlers.TryGetPlayerCharacter(requestHandler.ConnectionId, out IPlayerCharacterData playerCharacter))
            {
                // Do nothing, player character is not enter the game yet.
                result.InvokeError(new ResponseBuyoutMessage()
                {
                    message = UITextKeys.UI_ERROR_NOT_LOGGED_IN,
                });
                return;
            }
            // Get buyout price from service
            RestClient.Result<AuctionData> getResult = await AuctionRestClientForServer.GetAuction(request.auctionId);
            if (getResult.IsNetworkError || getResult.IsHttpError)
            {
                result.InvokeError(new ResponseBuyoutMessage()
                {
                    message = UITextKeys.UI_ERROR_CONTENT_NOT_AVAILABLE,
                });
                return;
            }
            // Seller cannot bid
            if (playerCharacter.UserId.Equals(getResult.Content.sellerId))
            {
                result.InvokeError(new ResponseBuyoutMessage()
                {
                    message = UITextKeys.UI_ERROR_NOT_ALLOWED,
                });
                return;
            }
            // Validate buyout price
            if (getResult.Content.buyoutPrice <= 0)
            {
                result.InvokeError(new ResponseBuyoutMessage()
                {
                    message = UITextKeys.UI_ERROR_NOT_ALLOWED,
                });
                return;
            }
            int price = getResult.Content.buyoutPrice;
            // Validate gold
            if (playerCharacter.Gold < getResult.Content.buyoutPrice)
            {
                result.InvokeError(new ResponseBuyoutMessage()
                {
                    message = UITextKeys.UI_ERROR_NOT_ENOUGH_GOLD,
                });
                return;
            }
            // Tell the service to add to buyout
            RestClient.Result buyoutResult = await AuctionRestClientForServer.Buyout(playerCharacter.UserId, playerCharacter.CharacterName, request.auctionId);
            if (buyoutResult.IsNetworkError || buyoutResult.IsHttpError)
            {
                result.InvokeError(new ResponseBuyoutMessage()
                {
                    message = UITextKeys.UI_ERROR_INTERNAL_SERVER_ERROR,
                });
                return;
            }
            // Reduce gold
            playerCharacter.Gold -= price;
            result.InvokeSuccess(new ResponseBuyoutMessage());
        }

        public void CancelAuction(CancelAuctionMessage cancelAuction, ResponseDelegate<ResponseCancelAuctionMessage> callback)
        {
            if (!IsClientConnected)
                return;
            ClientSendRequest(auctionHouseMessageTypes.cancelAuctionRequestType, cancelAuction, callback);
        }

        private async UniTaskVoid HandleCancelAuctionAtServer(RequestHandlerData requestHandler, CancelAuctionMessage request,
            RequestProceedResultDelegate<ResponseCancelAuctionMessage> result)
        {
            if (!ServerUserHandlers.TryGetPlayerCharacter(requestHandler.ConnectionId, out IPlayerCharacterData playerCharacter))
            {
                // Do nothing, player character is not enter the game yet.
                result.InvokeError(new ResponseCancelAuctionMessage()
                {
                    message = UITextKeys.UI_ERROR_NOT_LOGGED_IN,
                });
                return;
            }
            // Get auction data from service
            RestClient.Result<AuctionData> getResult = await AuctionRestClientForServer.GetAuction(request.auctionId);
            if (getResult.IsNetworkError || getResult.IsHttpError)
            {
                result.InvokeError(new ResponseCancelAuctionMessage()
                {
                    message = UITextKeys.UI_ERROR_CONTENT_NOT_AVAILABLE,
                });
                return;
            }
            // Non-seller cannot cancel
            if (!playerCharacter.UserId.Equals(getResult.Content.sellerId))
            {
                result.InvokeError(new ResponseCancelAuctionMessage()
                {
                    message = UITextKeys.UI_ERROR_NOT_ALLOWED,
                });
                return;
            }
            // Bidden cannot be cancelled
            if (!string.IsNullOrWhiteSpace(getResult.Content.buyerId))
            {
                result.InvokeError(new ResponseCancelAuctionMessage()
                {
                    message = UITextKeys.UI_ERROR_NOT_ALLOWED,
                });
                return;
            }
            // Tell the service to cancel auction
            RestClient.Result buyoutResult = await AuctionRestClientForServer.CancelAuction(playerCharacter.UserId, request.auctionId);
            if (buyoutResult.IsNetworkError || buyoutResult.IsHttpError)
            {
                result.InvokeError(new ResponseCancelAuctionMessage()
                {
                    message = UITextKeys.UI_ERROR_INTERNAL_SERVER_ERROR,
                });
                return;
            }
            result.InvokeSuccess(new ResponseCancelAuctionMessage());
        }

        public async UniTask<AsyncResponseData<ResponseClientConfigMessage>> GetAuctionClientConfig()
        {
            AsyncResponseData<ResponseClientConfigMessage> result = await ClientSendRequestAsync<EmptyMessage, ResponseClientConfigMessage>(auctionHouseMessageTypes.getClientConfigRequestType, EmptyMessage.Value);
            switch (result.ResponseCode)
            {
                case AckResponseCode.Success:
                    AuctionRestClientForClient.apiUrl = result.Response.serviceUrl;
                    AuctionRestClientForClient.secretKey = result.Response.accessToken;
                    break;
                case AckResponseCode.Error:
                    Logging.LogError(LogTag, $"Error occuring when retrieving client connection data from server, code: {result.Response.message}");
                    ClientGenericActions.ClientReceiveGameMessage(result.Response.message);
                    break;
                default:
                    Logging.LogError(LogTag, $"Cannot retrieving client connection data from server, response code: {result.ResponseCode}");
                    break;
            }
            return result;
        }

        private async UniTaskVoid HandleGetAuctionClientConfigAtServer(RequestHandlerData requestHandler, EmptyMessage request,
            RequestProceedResultDelegate<ResponseClientConfigMessage> result)
        {
            if (!ServerUserHandlers.TryGetPlayerCharacter(requestHandler.ConnectionId, out IPlayerCharacterData playerCharacter))
            {
                // Do nothing, player character is not enter the game yet.
                result.InvokeError(new ResponseClientConfigMessage()
                {
                    message = UITextKeys.UI_ERROR_NOT_LOGGED_IN,
                });
                return;
            }
            RestClient.Result<Dictionary<string, string>> getAccessTokenResult = await AuctionRestClientForServer.GetAccessToken(playerCharacter.UserId);
            if (getAccessTokenResult.IsNetworkError || getAccessTokenResult.IsHttpError)
            {
                result.InvokeError(new ResponseClientConfigMessage()
                {
                    message = UITextKeys.UI_ERROR_INTERNAL_SERVER_ERROR,
                });
                return;
            }
            result.InvokeSuccess(new ResponseClientConfigMessage()
            {
                serviceUrl = auctionHouseServiceUrlForClient,
                accessToken = getAccessTokenResult.Content["accessToken"]
            });
        }
    }
}
