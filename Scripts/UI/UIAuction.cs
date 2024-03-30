using LiteNetLibManager;
using MultiplayerARPG.MMO;
using UnityEngine;
using UnityEngine.Events;

namespace MultiplayerARPG.Auction
{
    public class UIAuction : UISelectionEntry<AuctionData>
    {
        [Header("String Formats")]
        [Tooltip("Format => {0} = {Bid Price}")]
        public UILocaleKeySetting formatKeyBidPrice = new UILocaleKeySetting(UIFormatKeys.UI_FORMAT_SIMPLE);
        [Tooltip("Format => {0} = {Buyout Price}")]
        public UILocaleKeySetting formatKeyBuyoutPrice = new UILocaleKeySetting(UIFormatKeys.UI_FORMAT_SIMPLE);
        [Tooltip("Format => {0} = {Seller Name}")]
        public UILocaleKeySetting formatKeySellerName = new UILocaleKeySetting(UIFormatKeys.UI_FORMAT_SIMPLE);
        [Tooltip("Format => {0} = {Buyer Name}")]
        public UILocaleKeySetting formatKeyBuyerName = new UILocaleKeySetting(UIFormatKeys.UI_FORMAT_SIMPLE);
        [Tooltip("Format => {0} = {Ends In}")]
        public UILocaleKeySetting formatKeyTimeLeft = new UILocaleKeySetting(UIFormatKeys.UI_FORMAT_SIMPLE);

        [Header("UI Elements")]
        public InputFieldWrapper inputBidPrice;
        public TextWrapper textBidPrice;
        public TextWrapper textBuyoutPrice;
        public TextWrapper textSellerName;
        public TextWrapper textBuyerName;
        public TextWrapper textTimeLeft;
        public UICharacterItem uiItem;
        public GameObject[] auctionEndedObjects;
        public GameObject[] underAuctioningObjects;
        public GameObject[] boughtOutObjects;
        public GameObject[] notBoughtOutObjects;

        protected override void OnDestroy()
        {
            base.OnDestroy();
            inputBidPrice = null;
            textBidPrice = null;
            textBuyoutPrice = null;
            textSellerName = null;
            textBuyerName = null;
            textTimeLeft = null;
            uiItem = null;
            auctionEndedObjects.Nulling();
            underAuctioningObjects.Nulling();
            boughtOutObjects.Nulling();
            notBoughtOutObjects.Nulling();
        }

        protected override void UpdateData()
        {
            if (textBidPrice != null)
            {
                textBidPrice.text = string.Format(
                    LanguageManager.GetText(formatKeyBidPrice),
                    Data.bidPrice.ToString("N0"));
            }

            if (textBuyoutPrice != null)
            {
                textBuyoutPrice.text = string.Format(
                    LanguageManager.GetText(formatKeyBuyoutPrice),
                    Data.buyoutPrice.ToString("N0"));
            }

            if (textSellerName != null)
            {
                textSellerName.text = string.Format(
                    LanguageManager.GetText(formatKeySellerName),
                    Data.sellerName);
            }

            if (textBuyerName != null)
            {
                textBuyerName.text = string.Format(
                    LanguageManager.GetText(formatKeyBuyerName),
                    Data.buyerName);
            }

            if (textTimeLeft != null)
            {
                System.DateTime timeLeft = System.DateTime.Now.AddMilliseconds(Data.timeLeft);
                var diff = timeLeft - System.DateTime.Now;
                textTimeLeft.text = string.Format(
                    LanguageManager.GetText(formatKeyTimeLeft),
                    string.Format("{0} Days {1} Hrs. {2} Min.", diff.Days, diff.Hours, diff.Minutes));
            }

            if (uiItem != null)
            {
                Mail mail = new Mail();
                mail.ReadItems(Data.itemData);
                if (mail.Items.Count > 0)
                {
                    uiItem.Setup(new UICharacterItemData(mail.Items[0], InventoryType.Unknow), GameInstance.PlayingCharacter, 0);
                    uiItem.Show();
                }
                else
                {
                    uiItem.Hide();
                }
            }

            if (auctionEndedObjects != null)
            {
                foreach (GameObject obj in auctionEndedObjects)
                {
                    obj.SetActive(Data.isEnd);
                }
            }

            if (underAuctioningObjects != null)
            {
                foreach (GameObject obj in underAuctioningObjects)
                {
                    obj.SetActive(!Data.isEnd);
                }
            }

            if (boughtOutObjects != null)
            {
                foreach (GameObject obj in boughtOutObjects)
                {
                    obj.SetActive(Data.isBuyout);
                }
            }

            if (notBoughtOutObjects != null)
            {
                foreach (GameObject obj in notBoughtOutObjects)
                {
                    obj.SetActive(!Data.isBuyout);
                }
            }
        }

        public void OnClickBid()
        {
            int bidPrice = int.Parse(inputBidPrice.text);
            OnClickBid(bidPrice);
        }

        public void OnClickBid(int bidPrice)
        {
            BaseGameNetworkManager.Singleton.Bid(new BidMessage()
            {
                auctionId = Data.id,
                price = bidPrice,
            }, OnBid);
        }

        private void OnBid(ResponseHandlerData requestHandler, AckResponseCode responseCode, ResponseBidMessage response)
        {
            if (responseCode.ShowUnhandledResponseMessageDialog(response.message))
                return;
            Refresh();
        }

        public void OnClickBuyout()
        {
            BaseGameNetworkManager.Singleton.Buyout(new BuyoutMessage()
            {
                auctionId = Data.id,
            }, OnBuyout);
        }

        private void OnBuyout(ResponseHandlerData requestHandler, AckResponseCode responseCode, ResponseBuyoutMessage response)
        {
            if (responseCode.ShowUnhandledResponseMessageDialog(response.message))
                return;
            Refresh();
        }

        public void OnClickCancelAuction()
        {
            BaseGameNetworkManager.Singleton.CancelAuction(new CancelAuctionMessage()
            {
                auctionId = Data.id,
            }, OnCancelAuction);
        }

        private void OnCancelAuction(ResponseHandlerData requestHandler, AckResponseCode responseCode, ResponseCancelAuctionMessage response)
        {
            if (responseCode.ShowUnhandledResponseMessageDialog(response.message))
                return;
            Refresh();
        }

        private async void Refresh()
        {
            UnityRestClient.RestClient.Result<AuctionData> result = await BaseGameNetworkManager.Singleton.AuctionRestClientForClient.GetAuction(Data.id);
            if (result.IsNetworkError || result.IsHttpError)
                return;
            Data = result.Content;
        }

        [System.Serializable]
        public class DataEvent : UnityEvent<AuctionData> { }

        [System.Serializable]
        public class UIEvent : UnityEvent<UIAuction> { }
    }
}
