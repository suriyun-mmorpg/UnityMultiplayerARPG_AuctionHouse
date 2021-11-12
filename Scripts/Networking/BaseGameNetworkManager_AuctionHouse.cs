using LiteNetLibManager;
using MultiplayerARPG.Auction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityRestClient;

namespace MultiplayerARPG
{
    public partial class BaseGameNetworkManager
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
        public string auctionHouseServiceUrl = "http://localhost:9800/auction-house";
        public string auctionHouseSecretKey = "secret";

        public AuctionRestClient AuctionRestClientForClient { get; private set; } = new AuctionRestClient();
        public AuctionRestClient AuctionRestClientForServer { get; private set; } = new AuctionRestClient();
        public readonly List<int> AuctionDurationOptions = new List<int>();

        [DevExtMethods("RegisterMessages")]
        protected void RegisterMessages_AuctionHouse()
        {
            RegisterServerMessage(auctionHouseMessageTypes.createAuctionMsgType, HandleCreateAuctionAtServer);
            RegisterServerMessage(auctionHouseMessageTypes.bidMsgType, HandleBidAtServer);
            RegisterServerMessage(auctionHouseMessageTypes.buyoutMsgType, HandleBuyoutAtServer);
            RegisterServerMessage(auctionHouseMessageTypes.getAccessTokenMsgType, HandleGetAuctionAccessTokenAtServer);
            RegisterClientMessage(auctionHouseMessageTypes.getAccessTokenMsgType, HandleGetAuctionAccessTokenAtClient);
        }

        [DevExtMethods("OnStartServer")]
        protected void OnStartServer_AuctionHouse()
        {
            AuctionRestClientForServer.url = auctionHouseServiceUrl;
            AuctionRestClientForServer.accessToken = auctionHouseSecretKey;
        }

        [DevExtMethods("OnClientOnlineSceneLoaded")]
        protected void OnClientOnlineSceneLoaded_AuctionHouse()
        {
            AuctionRestClientForClient.url = auctionHouseServiceUrl;
            ClientSendPacket(0, LiteNetLib.DeliveryMethod.ReliableUnordered, auctionHouseMessageTypes.getAccessTokenMsgType, (writer) =>
            {
                writer.Put(GameInstance.UserId);
            });
            GetAuctionDurationOptions();
        }

        protected void GetAuctionDurationOptions()
        {
            AuctionRestClientForClient.GetDurationOptions().ContinueWith((response) =>
            {
                if (response.Result.IsNetworkError || response.Result.IsHttpError)
                {
                    // TODO: Send error messages to client
                    GetAuctionDurationOptions();
                    return;
                }
                AuctionDurationOptions.Clear();
                AuctionDurationOptions.AddRange(response.Result.Content.durationOptions);
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
                return;
            }
            // Require index of non equip items, amount, starting auction price, buyout price (optional, 0 = no buyout)
            // Check player's item, then tell the service to add to bidding list, and remove it from inventory
            CreateAuctionMessage createAuction = messageHandler.ReadMessage<CreateAuctionMessage>();
            if (createAuction.amount <= 0)
                createAuction.amount = 1;
            if (createAuction.indexOfItem >= playerCharacterData.NonEquipItems.Count ||
                playerCharacterData.NonEquipItems[createAuction.indexOfItem].amount < createAuction.amount)
            {
                // Do nothing, wrong index of item or item amount is over than it has
                return;
            }
            // Tell the service to add to bidding list
            Mail mail = new Mail();
            mail.Items.Add(playerCharacterData.NonEquipItems[createAuction.indexOfItem]);
            RestClient.Result createResult = await AuctionRestClientForServer.CreateAuction(
                mail.WriteItems(),
                createAuction.startPrice,
                createAuction.buyoutPrice,
                playerCharacterData.UserId,
                playerCharacterData.CharacterName,
                createAuction.durationOption);
            if (createResult.IsNetworkError || createResult.IsHttpError)
            {
                // TODO: Send error messages to client
                return;
            }
            // Remove item from inventory
            playerCharacterData.DecreaseItemsByIndex(createAuction.indexOfItem, createAuction.amount);
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
                return;
            }
            BidMessage bid = messageHandler.ReadMessage<BidMessage>();
            // Get highest bidding price from service
            RestClient.Result<AuctionData> getResult = await AuctionRestClientForServer.GetAuction(bid.auctionId);
            if (getResult.IsNetworkError || getResult.IsHttpError)
            {
                // TODO: Send error messages to client
                return;
            }
            // Validate gold
            if (bid.price <= getResult.Content.bidPrice)
            {
                // TODO: Send error messages to client
                return;
            }
            if (playerCharacterData.Gold < getResult.Content.bidPrice)
            {
                // TODO: Send error messages to client
                return;
            }
            // Tell the service to add to bid
            RestClient.Result bidResult = await AuctionRestClientForServer.Bid(playerCharacterData.UserId, playerCharacterData.CharacterName, bid.auctionId, bid.price);
            if (bidResult.IsNetworkError || bidResult.IsHttpError)
            {
                // TODO: Send error messages to client
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
                return;
            }
            BuyoutMessage buyout = messageHandler.ReadMessage<BuyoutMessage>();
            // Get buyout price from service
            RestClient.Result<AuctionData> getResult = await AuctionRestClientForServer.GetAuction(buyout.auctionId);
            if (getResult.IsNetworkError || getResult.IsHttpError)
            {
                // TODO: Send error messages to client
                return;
            }
            int price = getResult.Content.buyoutPrice;
            // Validate gold
            if (playerCharacterData.Gold < getResult.Content.buyoutPrice)
            {
                // TODO: Send error messages to client
                return;
            }
            // Tell the service to add to buyout
            RestClient.Result buyoutResult = await AuctionRestClientForServer.Buyout(playerCharacterData.UserId, playerCharacterData.CharacterName, buyout.auctionId);
            if (buyoutResult.IsNetworkError || buyoutResult.IsHttpError)
            {
                // TODO: Send error messages to client
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
                // TODO: Send error messages to client
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
