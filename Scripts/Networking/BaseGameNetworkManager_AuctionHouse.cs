using LiteNetLibManager;
using MultiplayerARPG.Auction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        private void HandleCreateAuctionAtServer(MessageHandlerData messageHandler)
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

        private void HandleBidAtServer(MessageHandlerData messageHandler)
        {
            IPlayerCharacterData playerCharacterData;
            if (!ServerUserHandlers.TryGetPlayerCharacter(messageHandler.ConnectionId, out playerCharacterData))
            {
                // Do nothing, player character is not enter the game yet.
                return;
            }
            BidMessage bid = messageHandler.ReadMessage<BidMessage>();
            // Get highest bidding price from service

            // Validate gold

            // Tell the service to add to bid

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

        private void HandleBuyoutAtServer(MessageHandlerData messageHandler)
        {
            IPlayerCharacterData playerCharacterData;
            if (!ServerUserHandlers.TryGetPlayerCharacter(messageHandler.ConnectionId, out playerCharacterData))
            {
                // Do nothing, player character is not enter the game yet.
                return;
            }
            BuyoutMessage buyout = messageHandler.ReadMessage<BuyoutMessage>();
            // Get buyout price from service
            int price = 0;
            // Validate gold

            // Tell the service to add to buyout

            // Reduce gold
            playerCharacterData.Gold -= price;
        }
    }
}
