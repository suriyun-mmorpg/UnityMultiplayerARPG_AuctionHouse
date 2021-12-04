using LiteNetLibManager;
using MultiplayerARPG.Auction;
using System.Collections;
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
            public ushort createAuctionMsgType;
            public ushort bidMsgType;
            public ushort buyoutMsgType;
            public ushort getAccessTokenMsgType;
        }

        /*
         * Do `Item Listing`, `Sell History`, `Bid Listing`, `Bidding`, `Buying` directly with service
         */
        [Header("Auction House")]
        public AuctionHouseMessageTypes auctionHouseMessageTypes = new AuctionHouseMessageTypes()
        {
            createAuctionMsgType = 1300,
            bidMsgType = 1301,
            buyoutMsgType = 1302,
            getAccessTokenMsgType = 1303,
        };
        public string auctionHouseServiceUrl = "http://localhost:9800";
        public string auctionHouseSecretKey = "secret";

        public AuctionRestClient AuctionRestClientForClient { get; private set; } = new AuctionRestClient();
        public AuctionRestClient AuctionRestClientForServer { get; private set; } = new AuctionRestClient();

        [DevExtMethods("RegisterMessages")]
        private void RegisterMessages_AuctionHouse()
        {
            RegisterServerMessage(auctionHouseMessageTypes.createAuctionMsgType, HandleCreateAuctionAtServer);
            RegisterServerMessage(auctionHouseMessageTypes.bidMsgType, HandleBidAtServer);
            RegisterServerMessage(auctionHouseMessageTypes.buyoutMsgType, HandleBuyoutAtServer);
            RegisterServerMessage(auctionHouseMessageTypes.getAccessTokenMsgType, HandleGetAuctionAccessTokenAtServer);
            RegisterClientMessage(auctionHouseMessageTypes.getAccessTokenMsgType, HandleGetAuctionAccessTokenAtClient);
        }

        [DevExtMethods("OnStartServer")]
        private void OnStartServer_AuctionHouse()
        {
            AuctionRestClientForServer.url = auctionHouseServiceUrl;
            AuctionRestClientForServer.accessToken = auctionHouseSecretKey;
        }

        [DevExtMethods("OnClientOnlineSceneLoaded")]
        private void OnClientOnlineSceneLoaded_AuctionHouse()
        {
            AuctionRestClientForClient.url = auctionHouseServiceUrl;
            ClientSendPacket(0, LiteNetLib.DeliveryMethod.ReliableUnordered, auctionHouseMessageTypes.getAccessTokenMsgType, (writer) =>
            {
                writer.Put(GameInstance.UserId);
            });
        }

        public void CreateAuction(CreateAuctionMessage createAuction)
        {
            if (!IsClientConnected)
                return;
            // Send create auction message to server
            ClientSendPacket(0, LiteNetLib.DeliveryMethod.ReliableUnordered, auctionHouseMessageTypes.createAuctionMsgType, createAuction);
        }

        private async void HandleCreateAuctionAtServer(MessageHandlerData messageHandler)
        {
            IPlayerCharacterData playerCharacterData;
            if (!ServerUserHandlers.TryGetPlayerCharacter(messageHandler.ConnectionId, out playerCharacterData))
            {
                // Do nothing, player character is not enter the game yet.
                ServerGameMessageHandlers.SendGameMessage(messageHandler.ConnectionId, UITextKeys.UI_ERROR_NOT_LOGGED_IN);
                return;
            }
            CreateAuctionMessage createAuction = messageHandler.ReadMessage<CreateAuctionMessage>();
            if (createAuction.amount <= 0)
                createAuction.amount = 1;
            // Reduce gold by create auction price
            RestClient.Result<DurationOptionsResponse> durationOptionsResult = await AuctionRestClientForServer.GetDurationOptions();
            if (durationOptionsResult.IsNetworkError || durationOptionsResult.IsHttpError)
            {
                ServerGameMessageHandlers.SendGameMessage(messageHandler.ConnectionId, UITextKeys.UI_ERROR_INTERNAL_SERVER_ERROR);
                return;
            }
            int createAuctionPrice = durationOptionsResult.Content.durationOptions[createAuction.durationOption].price;
            if (playerCharacterData.Gold < createAuctionPrice)
            {
                ServerGameMessageHandlers.SendGameMessage(messageHandler.ConnectionId, UITextKeys.UI_ERROR_NOT_ENOUGH_GOLD);
                return;
            }
            // Require index of non equip items, amount, starting auction price, buyout price (optional, 0 = no buyout)
            // Check player's item, then tell the service to add to bidding list, and remove it from inventory
            if (createAuction.indexOfItem >= playerCharacterData.NonEquipItems.Count ||
                playerCharacterData.NonEquipItems[createAuction.indexOfItem].amount < createAuction.amount)
            {
                // Do nothing, wrong index of item or item amount is over than it has
                ServerGameMessageHandlers.SendGameMessage(messageHandler.ConnectionId, UITextKeys.UI_ERROR_INVALID_ITEM_INDEX);
                return;
            }
            // Tell the service to add to bidding list
            Mail mail = new Mail();
            mail.Items.Add(playerCharacterData.NonEquipItems[createAuction.indexOfItem]);
            RestClient.Result createResult = await AuctionRestClientForServer.CreateAuction(
                mail.WriteItems(),
                playerCharacterData.NonEquipItems[createAuction.indexOfItem].GetItem().DefaultTitle,
                playerCharacterData.NonEquipItems[createAuction.indexOfItem].level,
                createAuction.startPrice,
                createAuction.buyoutPrice,
                playerCharacterData.UserId,
                playerCharacterData.CharacterName,
                createAuction.durationOption);
            if (createResult.IsNetworkError || createResult.IsHttpError)
            {
                ServerGameMessageHandlers.SendGameMessage(messageHandler.ConnectionId, UITextKeys.UI_ERROR_INTERNAL_SERVER_ERROR);
                return;
            }
            // Remove item from inventory
            playerCharacterData.DecreaseItemsByIndex(createAuction.indexOfItem, createAuction.amount);
            playerCharacterData.Gold -= createAuctionPrice;
        }

        public void Bid(BidMessage bid)
        {
            if (!IsClientConnected)
                return;
            // Send create auction message to server
            ClientSendPacket(0, LiteNetLib.DeliveryMethod.ReliableUnordered, auctionHouseMessageTypes.bidMsgType, bid);
        }

        private async void HandleBidAtServer(MessageHandlerData messageHandler)
        {
            IPlayerCharacterData playerCharacterData;
            if (!ServerUserHandlers.TryGetPlayerCharacter(messageHandler.ConnectionId, out playerCharacterData))
            {
                // Do nothing, player character is not enter the game yet.
                ServerGameMessageHandlers.SendGameMessage(messageHandler.ConnectionId, UITextKeys.UI_ERROR_NOT_LOGGED_IN);
                return;
            }
            BidMessage bid = messageHandler.ReadMessage<BidMessage>();
            // Get highest bidding price from service
            RestClient.Result<AuctionData> getResult = await AuctionRestClientForServer.GetAuction(bid.auctionId);
            if (getResult.IsNetworkError || getResult.IsHttpError)
            {
                ServerGameMessageHandlers.SendGameMessage(messageHandler.ConnectionId, UITextKeys.UI_ERROR_INVALID_DATA);
                return;
            }
            // Validate gold
            if (bid.price <= getResult.Content.bidPrice)
            {
                ServerGameMessageHandlers.SendGameMessage(messageHandler.ConnectionId, UITextKeys.UI_ERROR_NOT_ENOUGH_GOLD);
                return;
            }
            if (playerCharacterData.Gold < getResult.Content.bidPrice)
            {
                ServerGameMessageHandlers.SendGameMessage(messageHandler.ConnectionId, UITextKeys.UI_ERROR_NOT_ENOUGH_GOLD);
                return;
            }
            // Tell the service to add to bid
            RestClient.Result bidResult = await AuctionRestClientForServer.Bid(playerCharacterData.UserId, playerCharacterData.CharacterName, bid.auctionId, bid.price);
            if (bidResult.IsNetworkError || bidResult.IsHttpError)
            {
                ServerGameMessageHandlers.SendGameMessage(messageHandler.ConnectionId, UITextKeys.UI_ERROR_INTERNAL_SERVER_ERROR);
                return;
            }
            // Reduce gold
            playerCharacterData.Gold -= bid.price;
        }

        public void Buyout(BuyoutMessage buyout)
        {
            if (!IsClientConnected)
                return;
            // Send create auction message to server
            ClientSendPacket(0, LiteNetLib.DeliveryMethod.ReliableUnordered, auctionHouseMessageTypes.buyoutMsgType, buyout);
        }

        private async void HandleBuyoutAtServer(MessageHandlerData messageHandler)
        {
            IPlayerCharacterData playerCharacterData;
            if (!ServerUserHandlers.TryGetPlayerCharacter(messageHandler.ConnectionId, out playerCharacterData))
            {
                // Do nothing, player character is not enter the game yet.
                ServerGameMessageHandlers.SendGameMessage(messageHandler.ConnectionId, UITextKeys.UI_ERROR_NOT_LOGGED_IN);
                return;
            }
            BuyoutMessage buyout = messageHandler.ReadMessage<BuyoutMessage>();
            // Get buyout price from service
            RestClient.Result<AuctionData> getResult = await AuctionRestClientForServer.GetAuction(buyout.auctionId);
            if (getResult.IsNetworkError || getResult.IsHttpError)
            {
                ServerGameMessageHandlers.SendGameMessage(messageHandler.ConnectionId, UITextKeys.UI_ERROR_NOT_ENOUGH_GOLD);
                return;
            }
            int price = getResult.Content.buyoutPrice;
            // Validate gold
            if (playerCharacterData.Gold < getResult.Content.buyoutPrice)
            {
                ServerGameMessageHandlers.SendGameMessage(messageHandler.ConnectionId, UITextKeys.UI_ERROR_NOT_ENOUGH_GOLD);
                return;
            }
            // Tell the service to add to buyout
            RestClient.Result buyoutResult = await AuctionRestClientForServer.Buyout(playerCharacterData.UserId, playerCharacterData.CharacterName, buyout.auctionId);
            if (buyoutResult.IsNetworkError || buyoutResult.IsHttpError)
            {
                ServerGameMessageHandlers.SendGameMessage(messageHandler.ConnectionId, UITextKeys.UI_ERROR_INTERNAL_SERVER_ERROR);
                return;
            }
            // Reduce gold
            playerCharacterData.Gold -= price;
        }

        private async void HandleGetAuctionAccessTokenAtServer(MessageHandlerData messageHandler)
        {
            string userId = messageHandler.Reader.GetString();
            RestClient.Result<Dictionary<string, string>> getAccessTokenResult = await AuctionRestClientForServer.GetAccessToken(userId);
            if (getAccessTokenResult.IsNetworkError || getAccessTokenResult.IsHttpError)
            {
                ServerGameMessageHandlers.SendGameMessage(messageHandler.ConnectionId, UITextKeys.UI_ERROR_INTERNAL_SERVER_ERROR);
                return;
            }
            ServerSendPacket(messageHandler.ConnectionId, 0, LiteNetLib.DeliveryMethod.ReliableUnordered, auctionHouseMessageTypes.getAccessTokenMsgType, (writer) =>
            {
                writer.Put(getAccessTokenResult.Content["accessToken"]);
            });
        }

        private void HandleGetAuctionAccessTokenAtClient(MessageHandlerData messageHandler)
        {
            AuctionRestClientForClient.accessToken = messageHandler.Reader.GetString();
        }
    }
}
