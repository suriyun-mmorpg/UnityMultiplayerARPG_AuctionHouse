using UnityEngine;

namespace MultiplayerARPG.Auction
{
    public class UIAuction : UISelectionEntry<Auction>
    {
        [Header("String Formats")]
        [Tooltip("Format => {0} = {Bid Price}")]
        public UILocaleKeySetting formatKeyBidPrice = new UILocaleKeySetting(UIFormatKeys.UI_FORMAT_SIMPLE);
        [Tooltip("Format => {0} = {Buyout Price}")]
        public UILocaleKeySetting formatKeyBuyoutPrice = new UILocaleKeySetting(UIFormatKeys.UI_FORMAT_SIMPLE);
        [Tooltip("Format => {0} = {Seller Name}")]
        public UILocaleKeySetting formatKeySellerName = new UILocaleKeySetting(UIFormatKeys.UI_FORMAT_SIMPLE);
        [Tooltip("Format => {0} = {Ends In}")]
        public UILocaleKeySetting formatKeyTimeLeft = new UILocaleKeySetting(UIFormatKeys.UI_FORMAT_SIMPLE);

        [Header("UI Elements")]
        public InputFieldWrapper inputBidPrice;
        public TextWrapper textBidPrice;
        public TextWrapper textBuyoutPrice;
        public TextWrapper textSellerName;
        public TextWrapper textTimeLeft;

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

            if (textTimeLeft != null)
            {
                System.DateTime timeLeft = System.DateTime.Now.AddMilliseconds(Data.timeLeft);
                var diff = timeLeft - System.DateTime.Now;
                textTimeLeft.text = string.Format(
                    LanguageManager.GetText(formatKeyTimeLeft),
                    string.Format("{0} Days {1} Hrs. {2} Min.", diff.Days, diff.Hours, diff.Minutes));
            }
        }

        public void OnClickBid()
        {
            int bidPrice = int.Parse(inputBidPrice.text);
            BaseGameNetworkManager.Singleton.Bid(new BidMessage()
            {
                auctionId = Data.id,
                price = bidPrice,
            });
        }

        public void OnClickBuyout()
        {
            BaseGameNetworkManager.Singleton.Buyout(new BuyoutMessage()
            {
                auctionId = Data.id,
            });
        }
    }
}
