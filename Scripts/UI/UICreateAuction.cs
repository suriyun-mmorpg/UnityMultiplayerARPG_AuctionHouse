using MultiplayerARPG.MMO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityRestClient;

namespace MultiplayerARPG.Auction
{
    public partial class UICreateAuction : MonoBehaviour
    {
        [Header("Create Auction")]
        public InputFieldWrapper inputCreateAuctionAmount;
        public InputFieldWrapper inputCreateAuctionStartPrice;
        public InputFieldWrapper inputCreateAuctionBuyoutPrice;
        public UILocaleKeySetting formatKeyDuration = new UILocaleKeySetting(UIFormatKeys.UI_FORMAT_SIMPLE);
        public TextWrapper textAuctionCreateDuration;
        public UILocaleKeySetting formatKeyRequireGold = new UILocaleKeySetting(UIFormatKeys.UI_FORMAT_REQUIRE_GOLD);
        public UILocaleKeySetting formatKeyRequireGoldNotEnough = new UILocaleKeySetting(UIFormatKeys.UI_FORMAT_REQUIRE_GOLD_NOT_ENOUGH);
        public TextWrapper textAuctionCreatePrice;
        public UICharacterItem uiItem;
        public UnityEvent onCreateAuction = new UnityEvent();

        protected bool isReady = false;
        protected readonly List<DurationOption> durationOptions = new List<DurationOption>();
        protected int durationOptionIndex = 0;
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
            LoadDurationOptions();
        }

        protected virtual void OnDisable()
        {
            uiItem.onUpdateData -= OnUpdateData;
        }

        protected async void LoadDurationOptions()
        {
            isReady = false;
            RestClient.Result<DurationOptionsResponse> durationOptionsResult = await (BaseGameNetworkManager.Singleton as MapNetworkManager).AuctionRestClientForClient.GetDurationOptions();
            durationOptions.Clear();
            durationOptions.AddRange(durationOptionsResult.Content.durationOptions);
            SelectDurationOption(0);
            isReady = true;
        }

        public void OnClickNextDurationOption()
        {
            durationOptionIndex++;
            if (durationOptionIndex > durationOptions.Count - 1)
                durationOptionIndex = durationOptions.Count - 1;
            SelectDurationOption(durationOptionIndex);
        }

        public void OnClickPreviousDurationOption()
        {
            durationOptionIndex--;
            if (durationOptionIndex < 0)
                durationOptionIndex = 0;
            SelectDurationOption(durationOptionIndex);
        }

        public void SelectDurationOption(int index)
        {
            durationOptionIndex = index;
            if (textAuctionCreateDuration)
                textAuctionCreateDuration.text = string.Format(LanguageManager.GetText(formatKeyDuration), durationOptions[index].hours.ToString("N0"));
            if (textAuctionCreatePrice)
            {
                int requireGold = durationOptions[index].price;
                textAuctionCreatePrice.text = string.Format(
                        LanguageManager.GetText(GameInstance.PlayingCharacter.Gold >= requireGold ?
                        formatKeyRequireGold : formatKeyRequireGoldNotEnough),
                    GameInstance.PlayingCharacter.Gold.ToString("N0"),
                    requireGold.ToString("N0"));
            }
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
                durationOption = durationOptionIndex,
            });
            onCreateAuction.Invoke();
        }
    }
}
