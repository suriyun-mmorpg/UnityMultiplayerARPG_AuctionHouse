using Cysharp.Threading.Tasks;
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
        public int reconnectDelayInMilliseconds = 5000;
        private int _page = 1;
        public int Page
        {
            get { return _page; }
            set
            {
                _page = value;
            }
        }
        public int TotalPage
        {
            get; set;
        }

        private UIList _cacheList;
        public UIList CacheList
        {
            get
            {
                if (_cacheList == null)
                {
                    _cacheList = gameObject.AddComponent<UIList>();
                    _cacheList.uiPrefab = uiPrefab.gameObject;
                    _cacheList.uiContainer = uiContainer;
                }
                return _cacheList;
            }
        }

        private UIAuctionSelectionManager _cacheSelectionManager;
        public UIAuctionSelectionManager CacheSelectionManager
        {
            get
            {
                if (_cacheSelectionManager == null)
                    _cacheSelectionManager = gameObject.GetOrAddComponent<UIAuctionSelectionManager>();
                _cacheSelectionManager.selectionMode = UISelectionMode.Toggle;
                return _cacheSelectionManager;
            }
        }

        public AuctionRestClient RestClient
        {
            get { return BaseGameNetworkManager.Singleton.AuctionRestClientForClient; }
        }

        private void OnEnable()
        {
            CacheSelectionManager.eventOnSelected.RemoveListener(OnSelect);
            CacheSelectionManager.eventOnSelected.AddListener(OnSelect);
            CacheSelectionManager.eventOnDeselected.RemoveListener(OnDeselect);
            CacheSelectionManager.eventOnDeselected.AddListener(OnDeselect);
            if (uiDialog != null)
                uiDialog.onHide.AddListener(OnDialogHide);
            _page = 1;
            if (textPage)
                textPage.text = string.Format(formatKeyPage.ToFormat(), 1, 1);
            Refresh();
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

        public void GetClientConfig()
        {
            BaseGameNetworkManager.Singleton.GetAuctionClientConfig(OnGetClientConfig);
        }

        private async void OnGetClientConfig(ResponseHandlerData requestHandler, AckResponseCode responseCode, ResponseClientConfigMessage response)
        {
            if (responseCode != AckResponseCode.Success)
            {
                // Cannot get access token
                await UniTask.Delay(reconnectDelayInMilliseconds);
                GetClientConfig();
                return;
            }
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
            if (string.IsNullOrEmpty(RestClient.apiUrl) ||
                string.IsNullOrEmpty(RestClient.secretKey))
            {
                GetClientConfig();
                return;
            }
            RestClient.Result<AuctionListResponse> result;
            switch (listMode)
            {
                case ListMode.SellHistory:
                    result = await RestClient.GetSellHistoryList(limitPerPage, page);
                    break;
                case ListMode.BuyHistory:
                    result = await RestClient.GetBuyHistoryList(limitPerPage, page);
                    break;
                default:
                    result = await RestClient.GetAuctionList(limitPerPage, page);
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
                GetClientConfig();
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
            if (_page + 1 > TotalPage)
                Page = TotalPage;
            else
                Page = Page + 1;
            Refresh();
        }

        public void OnClickPreviousPage()
        {
            if (_page - 1 < 1)
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
