using LiteNetLibManager;
using MultiplayerARPG.MMO;
using UnityEngine;
using UnityRestClient;

namespace MultiplayerARPG.Auction
{
    public class UIAuctionList : UIBase
    {
        public enum ListMode
        {
            Browse,
            SellHistory,
            BuyHistory,
        }

        [Header("String Formats")]
        [Tooltip("Format => {0} = {Page} / {Total Page}")]
        public UILocaleKeySetting formatKeyPage = new UILocaleKeySetting(UIFormatKeys.UI_FORMAT_SIMPLE_MIN_BY_MAX);

        [Header("UI Elements")]
        public ListMode listMode;
        public GameObject listEmptyObject;
        public UIAuction uiDialog;
        public UIAuction uiPrefab;
        public Transform uiContainer;
        public InputFieldWrapper inputBidPrice;
        public TextWrapper textPage;
        public int limitPerPage = 20;
        private int page = 1;
        public int Page
        {
            get { return page; }
            set
            {
                page = value;
            }
        }
        public int TotalPage
        {
            get; set;
        }

        private UIList cacheList;
        public UIList CacheList
        {
            get
            {
                if (cacheList == null)
                {
                    cacheList = gameObject.AddComponent<UIList>();
                    cacheList.uiPrefab = uiPrefab.gameObject;
                    cacheList.uiContainer = uiContainer;
                }
                return cacheList;
            }
        }

        private UIAuctionSelectionManager cacheSelectionManager;
        public UIAuctionSelectionManager CacheSelectionManager
        {
            get
            {
                if (cacheSelectionManager == null)
                    cacheSelectionManager = gameObject.GetOrAddComponent<UIAuctionSelectionManager>();
                cacheSelectionManager.selectionMode = UISelectionMode.Toggle;
                return cacheSelectionManager;
            }
        }

        private float lastGetAccessToken = float.MinValue;

        private void OnEnable()
        {
            CacheSelectionManager.eventOnSelected.RemoveListener(OnSelect);
            CacheSelectionManager.eventOnSelected.AddListener(OnSelect);
            CacheSelectionManager.eventOnDeselected.RemoveListener(OnDeselect);
            CacheSelectionManager.eventOnDeselected.AddListener(OnDeselect);
            if (uiDialog != null)
                uiDialog.onHide.AddListener(OnDialogHide);
            page = 1;
            if (textPage)
                textPage.text = string.Format(formatKeyPage.ToFormat(), 1, 1);
            GetAccessToken();
        }

        private void OnDisable()
        {
            if (uiDialog != null)
                uiDialog.onHide.RemoveListener(OnDialogHide);
            CacheSelectionManager.DeselectSelectedUI();
        }

        private void OnDialogHide()
        {
            CacheSelectionManager.DeselectSelectedUI();
        }

        private void OnSelect(UIAuction ui)
        {
            if (uiDialog != null)
            {
                uiDialog.selectionManager = CacheSelectionManager;
                uiDialog.Data = ui.Data;
                uiDialog.Show();
            }
        }

        private void OnDeselect(UIAuction ui)
        {
            if (uiDialog != null)
            {
                uiDialog.onHide.RemoveListener(OnDialogHide);
                uiDialog.Hide();
                uiDialog.onHide.AddListener(OnDialogHide);
            }
        }

        public void GetAccessToken()
        {
            if (Time.unscaledTime - lastGetAccessToken < 30)
                return;
            (BaseGameNetworkManager.Singleton as MapNetworkManager).GetAccessToken(OnGetAccessToken);
        }

        private void OnGetAccessToken(ResponseHandlerData requestHandler, AckResponseCode responseCode, ResponseClientConfigMessage response)
        {
            if (responseCode != AckResponseCode.Success)
            {
                // Cannot get access token
                GetAccessToken();
                return;
            }
            lastGetAccessToken = Time.unscaledTime;
            (BaseGameNetworkManager.Singleton as MapNetworkManager).AuctionRestClientForClient.url = response.serviceUrl;
            (BaseGameNetworkManager.Singleton as MapNetworkManager).AuctionRestClientForClient.accessToken = response.accessToken;
            Refresh();
        }

        public void Refresh()
        {
            GoToPageRoutine(Page);
        }

        public void RefreshIfActive()
        {
            if (!gameObject.activeSelf)
                return;
            Refresh();
        }

        public void GoToPage(int page)
        {
            GoToPageRoutine(page);
        }

        private async void GoToPageRoutine(int page)
        {
            RestClient.Result<AuctionListResponse> result;
            switch (listMode)
            {
                case ListMode.SellHistory:
                    result = await (BaseGameNetworkManager.Singleton as MapNetworkManager).AuctionRestClientForClient.GetSellHistoryList(limitPerPage, page);
                    break;
                case ListMode.BuyHistory:
                    result = await (BaseGameNetworkManager.Singleton as MapNetworkManager).AuctionRestClientForClient.GetBuyHistoryList(limitPerPage, page);
                    break;
                default:
                    result = await (BaseGameNetworkManager.Singleton as MapNetworkManager).AuctionRestClientForClient.GetAuctionList(limitPerPage, page);
                    break;
            }
            int selectedId = CacheSelectionManager.SelectedUI != null ? CacheSelectionManager.SelectedUI.Data.id : 0;
            CacheSelectionManager.DeselectSelectedUI();
            CacheSelectionManager.Clear();
            CacheList.HideAll();
            if (listEmptyObject != null)
                listEmptyObject.SetActive(true);
            if (result.IsNetworkError)
                return;
            if (result.IsHttpError)
            {
                GetAccessToken();
                return;
            }
            if (result.Content.list.Count == 0)
                return;
            UIAuction tempUi;
            CacheList.Generate(result.Content.list, (index, data, ui) =>
            {
                tempUi = ui.GetComponent<UIAuction>();
                tempUi.Data = data;
                tempUi.Show();
                CacheSelectionManager.Add(tempUi);
                if (selectedId == data.id)
                    tempUi.OnClickSelect();
            });
            if (listEmptyObject != null)
                listEmptyObject.SetActive(result.Content.list.Count == 0);
            if (textPage)
                textPage.text = string.Format(formatKeyPage.ToFormat(), result.Content.page, result.Content.totalPage);
            TotalPage = result.Content.totalPage;
        }

        public void OnClickNextPage()
        {
            if (page + 1 > TotalPage)
                Page = TotalPage;
            else
                Page = Page + 1;
            Refresh();
        }

        public void OnClickPreviousPage()
        {
            if (page - 1 < 1)
                Page = 1;
            else
                Page = Page - 1;
            Refresh();
        }

        public void OnClickBid()
        {
            if (!CacheSelectionManager.SelectedUI)
                return;
            int bidPrice = int.Parse(inputBidPrice.text);
            CacheSelectionManager.SelectedUI.OnClickBid(bidPrice);
        }

        public void OnClickBuyout()
        {
            if (!CacheSelectionManager.SelectedUI)
                return;
            CacheSelectionManager.SelectedUI.OnClickBuyout();
        }

        public void OnClickCancelAuction()
        {
            if (!CacheSelectionManager.SelectedUI)
                return;
            CacheSelectionManager.SelectedUI.OnClickCancelAuction();
        }
    }
}
