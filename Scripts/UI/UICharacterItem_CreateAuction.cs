using MultiplayerARPG.Auction;
using MultiplayerARPG.MMO;
using UnityEngine;

namespace MultiplayerARPG
{
    public partial class UICharacterItem
    {
        [Header("Create Auction")]
        public InputFieldWrapper inputCreateAuctionAmount;
        public InputFieldWrapper inputCreateAuctionStartPrice;
        public InputFieldWrapper inputCreateAuctionBuyoutPrice;

        protected short maxCreateAuctionAmount = 0;

        [DevExtMethods("Show")]
        protected virtual void Show_CreateAuction()
        {
            short amount = CharacterItem == null ? (short)1 : CharacterItem.amount;
            maxCreateAuctionAmount = amount;
            inputCreateAuctionAmount.text = amount.ToString();
            inputCreateAuctionAmount.onValueChanged.RemoveListener(InputCreateAuctionAmountOnValueChanged);
            inputCreateAuctionAmount.onValueChanged.AddListener(InputCreateAuctionAmountOnValueChanged);
            inputCreateAuctionStartPrice.text = "0";
            inputCreateAuctionStartPrice.onValueChanged.RemoveListener(InputCreateAuctionStartPriceOnValueChanged);
            inputCreateAuctionStartPrice.onValueChanged.AddListener(InputCreateAuctionStartPriceOnValueChanged);
            inputCreateAuctionBuyoutPrice.text = "0";
            inputCreateAuctionBuyoutPrice.onValueChanged.RemoveListener(InputCreateAuctionBuyoutPriceOnValueChanged);
            inputCreateAuctionBuyoutPrice.onValueChanged.AddListener(InputCreateAuctionBuyoutPriceOnValueChanged);
        }

        [DevExtMethods("UpdateData")]
        protected virtual void UpdateData_CreateAuction()
        {
            short amount = CharacterItem == null ? (short)0 : CharacterItem.amount;
            maxCreateAuctionAmount = amount;
            inputCreateAuctionAmount.text = amount.ToString();
        }

        protected void InputCreateAuctionAmountOnValueChanged(string text)
        {
            short amount;
            if (!short.TryParse(text, out amount) || amount < 1)
                amount = 1;
            inputCreateAuctionAmount.SetTextWithoutNotify(amount.ToString());
        }

        protected void InputCreateAuctionStartPriceOnValueChanged(string text)
        {
            int amount;
            if (!int.TryParse(text, out amount) || amount < 0)
                amount = 0;
            inputCreateAuctionStartPrice.SetTextWithoutNotify(amount.ToString());
        }

        protected void InputCreateAuctionBuyoutPriceOnValueChanged(string text)
        {
            int amount;
            if (!int.TryParse(text, out amount) || amount < 0)
                amount = 0;
            inputCreateAuctionBuyoutPrice.SetTextWithoutNotify(amount.ToString());
        }

        public void OnClickCreateAuction()
        {
            short amount = short.Parse(inputCreateAuctionAmount.text);
            int startPrice = int.Parse(inputCreateAuctionStartPrice.text);
            int buyoutPrice = int.Parse(inputCreateAuctionBuyoutPrice.text);
            (BaseGameNetworkManager.Singleton as MapNetworkManager).CreateAuction(new CreateAuctionMessage()
            {
                indexOfItem = IndexOfData,
                amount = amount,
                startPrice = startPrice,
                buyoutPrice = buyoutPrice,
            });
            Hide();
        }
    }
}
