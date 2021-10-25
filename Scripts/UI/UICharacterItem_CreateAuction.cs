using UnityEngine;

namespace MultiplayerARPG
{
    public partial class UICharacterItem
    {
        [Header("Create Auction")]
        public InputFieldWrapper inputCreateAuctionAmount;
        public InputFieldWrapper inputCreateAuctionStartPrice;
        public InputFieldWrapper inputCreateAuctionBuyoutPrice;

        public void OnClickCreateAuction()
        {
            short amount = short.Parse(inputCreateAuctionAmount.text);
            int startPrice = int.Parse(inputCreateAuctionStartPrice.text);
            int buyoutPrice = int.Parse(inputCreateAuctionBuyoutPrice.text);
            BaseGameNetworkManager.Singleton.CreateAuction(new CreateAuctionMessage()
            {
                indexOfItem = IndexOfData,
                amount = amount,
                startPrice = startPrice,
                buyoutPrice = buyoutPrice,
            });
        }
    }
}
