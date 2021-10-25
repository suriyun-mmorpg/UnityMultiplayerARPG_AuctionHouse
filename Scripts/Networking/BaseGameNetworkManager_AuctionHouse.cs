using LiteNetLibManager;
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

        [DevExtMethods("RegisterServerMessages")]
        protected void RegisterServerMessages_AuctionHouse()
        {
            RegisterServerMessage(createAuctionMsgType, HandleCreateAuctionAtServer);
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
    }
}
