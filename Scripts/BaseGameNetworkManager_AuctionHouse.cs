using LiteNetLibManager;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerARPG
{
    public partial class BaseGameNetworkManager
    {
        /*
         * Do `Item Listing`, `Bidding`, `Buying` directly with service
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

        }
    }
}
