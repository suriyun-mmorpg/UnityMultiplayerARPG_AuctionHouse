using MultiplayerARPG.MMO;
using UnityEngine;
using UnityEngine.Events;

namespace MultiplayerARPG.Auction
{
    public partial class UICreateAuction : MonoBehaviour
    {
        [Header("Create Auction")]
        public InputFieldWrapper inputCreateAuctionAmount;
        public InputFieldWrapper inputCreateAuctionStartPrice;
        public InputFieldWrapper inputCreateAuctionBuyoutPrice;
        public UICharacterItem uiItem;
        public UnityEvent onCreateAuction = new UnityEvent();

        protected short maxCreateAuctionAmount = 0;

        protected virtual void OnEnable()
        {
            uiItem.onUpdateData += OnUpdateData;
            short amount = uiItem.CharacterItem == null ? (short)1 : uiItem.CharacterItem.amount;
            maxCreateAuctionAmount = amount;
            if (inputCreateAuctionAmount)
            {
                inputCreateAuctionAmount.text = amount.ToString();
                inputCreateAuctionAmount.onValueChanged.RemoveListener(InputCreateAuctionAmountOnValueChanged);
                inputCreateAuctionAmount.onValueChanged.AddListener(InputCreateAuctionAmountOnValueChanged);
            }
            if (inputCreateAuctionStartPrice)
            {
                inputCreateAuctionStartPrice.SetTextWithoutNotify("0");
                inputCreateAuctionStartPrice.onValueChanged.RemoveListener(InputCreateAuctionStartPriceOnValueChanged);
                inputCreateAuctionStartPrice.onValueChanged.AddListener(InputCreateAuctionStartPriceOnValueChanged);
            }
            if (inputCreateAuctionBuyoutPrice)
            {
                inputCreateAuctionBuyoutPrice.SetTextWithoutNotify("0");
                inputCreateAuctionBuyoutPrice.onValueChanged.RemoveListener(InputCreateAuctionBuyoutPriceOnValueChanged);
                inputCreateAuctionBuyoutPrice.onValueChanged.AddListener(InputCreateAuctionBuyoutPriceOnValueChanged);
            }
        }

        protected virtual void OnDisable()
        {
            uiItem.onUpdateData -= OnUpdateData;
        }

        protected void OnUpdateData(UICharacterItemData data)
        {
            short amount = uiItem.CharacterItem == null ? (short)1 : data.characterItem.amount;
            maxCreateAuctionAmount = amount;
            if (inputCreateAuctionAmount)
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
                indexOfItem = uiItem.IndexOfData,
                amount = amount,
                startPrice = startPrice,
                buyoutPrice = buyoutPrice,
            });
            onCreateAuction.Invoke();
        }
    }
}
