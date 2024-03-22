using LiteNetLibManager;
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
        [Tooltip("Format => {0} = {Current Gold Amount}, {1} = {Target Amount}")]
        public UILocaleKeySetting formatKeyRequireGold = new UILocaleKeySetting(UIFormatKeys.UI_FORMAT_REQUIRE_GOLD);
        [Tooltip("Format => {0} = {Current Gold Amount}, {1} = {Target Amount}")]
        public UILocaleKeySetting formatKeyRequireGoldNotEnough = new UILocaleKeySetting(UIFormatKeys.UI_FORMAT_REQUIRE_GOLD_NOT_ENOUGH);
        public TextWrapper textAuctionCreatePrice;
        public UICharacterItem uiItem;
        public UnityEvent onCreateAuction = new UnityEvent();

        protected bool _isReady = false;
        protected readonly List<DurationOption> _durationOptions = new List<DurationOption>();
        protected int _durationOptionIndex = 0;
        protected int _maxCreateAuctionAmount = 0;

        protected virtual void OnEnable()
        {
            uiItem.onUpdateData += OnUpdateData;
            int amount = uiItem.CharacterItem.amount;
            _maxCreateAuctionAmount = amount;
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
            _isReady = false;
            RestClient.Result<DurationOptionsResponse> durationOptionsResult = await BaseGameNetworkManager.Singleton.AuctionRestClientForClient.GetDurationOptions();
            _durationOptions.Clear();
            _durationOptions.AddRange(durationOptionsResult.Content.durationOptions);
            SelectDurationOption(0);
            _isReady = true;
        }

        public void OnClickNextDurationOption()
        {
            _durationOptionIndex++;
            if (_durationOptionIndex > _durationOptions.Count - 1)
                _durationOptionIndex = _durationOptions.Count - 1;
            SelectDurationOption(_durationOptionIndex);
        }

        public void OnClickPreviousDurationOption()
        {
            _durationOptionIndex--;
            if (_durationOptionIndex < 0)
                _durationOptionIndex = 0;
            SelectDurationOption(_durationOptionIndex);
        }

        public void SelectDurationOption(int index)
        {
            _durationOptionIndex = index;
            if (textAuctionCreateDuration)
                textAuctionCreateDuration.text = string.Format(LanguageManager.GetText(formatKeyDuration), _durationOptions[index].hours.ToString("N0"));
            if (textAuctionCreatePrice)
            {
                int requireGold = _durationOptions[index].price;
                textAuctionCreatePrice.text = string.Format(
                        LanguageManager.GetText(GameInstance.PlayingCharacter.Gold >= requireGold ?
                        formatKeyRequireGold : formatKeyRequireGoldNotEnough),
                    GameInstance.PlayingCharacter.Gold.ToString("N0"),
                    requireGold.ToString("N0"));
            }
        }

        protected void OnUpdateData(UICharacterItemData data)
        {
            int amount = data.characterItem.amount;
            _maxCreateAuctionAmount = amount;
            if (inputCreateAuctionAmount)
                inputCreateAuctionAmount.text = amount.ToString();
        }

        protected void InputCreateAuctionAmountOnValueChanged(string text)
        {
            int amount;
            if (!int.TryParse(text, out amount) || amount < 1)
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
            int amount = int.Parse(inputCreateAuctionAmount.text);
            int startPrice = int.Parse(inputCreateAuctionStartPrice.text);
            int buyoutPrice = int.Parse(inputCreateAuctionBuyoutPrice.text);
            BaseGameNetworkManager.Singleton.CreateAuction(new CreateAuctionMessage()
            {
                indexOfItem = uiItem.IndexOfData,
                amount = amount,
                startPrice = startPrice,
                buyoutPrice = buyoutPrice,
                durationOption = _durationOptionIndex,
            }, OnCreateAuction);
        }

        private void OnCreateAuction(ResponseHandlerData requestHandler, AckResponseCode responseCode, ResponseCreateAuctionMessage response)
        {
            if (responseCode.ShowUnhandledResponseMessageDialog(response.message))
                return;
            onCreateAuction.Invoke();
        }
    }
}
