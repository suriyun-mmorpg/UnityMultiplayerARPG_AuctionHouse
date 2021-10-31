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
        /*
         * Do `Item Listing`, `Sell History`, `Bid Listing`, `Bidding`, `Buying` directly with service
         */
        [Header("Auction House")]
        public ushort createAuctionMsgType = 300;
        public ushort bidMsgType = 301;
        public ushort buyoutMsgType = 301;
        public string auctionHouseServiceUrl = "http://localhost:9800/auction-house";

        public AuctionRestClient RestClientForClient { get; private set; }
        public AuctionRestClient RestClientForServer { get; private set; }

        [DevExtMethods("RegisterServerMessages")]
        protected void RegisterServerMessages_AuctionHouse()
        {
            RegisterServerMessage(createAuctionMsgType, HandleCreateAuctionAtServer);
            RegisterServerMessage(bidMsgType, HandleCreateAuctionAtServer);
            RegisterServerMessage(buyoutMsgType, HandleCreateAuctionAtServer);
        }

        public void CreateAuction(CreateAuctionMessage createAuction)
        {
            if (!IsClientConnected)
                return;
            // Send create auction message to server
            ClientSendPacket(0, LiteNetLib.DeliveryMethod.ReliableUnordered, createAuctionMsgType, createAuction);
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
            RestClient.Result createResult = await RestClientForServer.CreateAuction(
                playerCharacterData.NonEquipItems[createAuction.indexOfItem].dataId,
                playerCharacterData.NonEquipItems[createAuction.indexOfItem].level,
                createAuction.amount,
                playerCharacterData.NonEquipItems[createAuction.indexOfItem].durability,
                playerCharacterData.NonEquipItems[createAuction.indexOfItem].exp,
                playerCharacterData.NonEquipItems[createAuction.indexOfItem].lockRemainsDuration,
                playerCharacterData.NonEquipItems[createAuction.indexOfItem].expireTime,
                playerCharacterData.NonEquipItems[createAuction.indexOfItem].randomSeed,
                playerCharacterData.NonEquipItems[createAuction.indexOfItem].WriteSockets(),
                createAuction.startPrice,
                createAuction.buyoutPrice,
                playerCharacterData.Id,
                playerCharacterData.CharacterName);
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
            ClientSendPacket(0, LiteNetLib.DeliveryMethod.ReliableUnordered, bidMsgType, bid);
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
            RestClient.Result<AuctionData> getResult = await RestClientForServer.GetAuction(bid.auctionId);
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
            RestClient.Result bidResult = await RestClientForServer.Bid(playerCharacterData.Id, bid.auctionId, bid.price);
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
            ClientSendPacket(0, LiteNetLib.DeliveryMethod.ReliableUnordered, buyoutMsgType, buyout);
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
            RestClient.Result<AuctionData> getResult = await RestClientForServer.GetAuction(buyout.auctionId);
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
            RestClient.Result buyoutResult = await RestClientForServer.Buyout(playerCharacterData.Id, buyout.auctionId);
            if (buyoutResult.IsNetworkError || buyoutResult.IsHttpError)
            {
                // TODO: Send error messages to client
                return;
            }
            // Reduce gold
            playerCharacterData.Gold -= price;
        }
    }
}
