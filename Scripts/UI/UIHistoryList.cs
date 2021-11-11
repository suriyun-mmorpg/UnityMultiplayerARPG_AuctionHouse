using UnityEngine;
using UnityRestClient;

namespace MultiplayerARPG.Auction
{
    public class UIHistoryList : MonoBehaviour
    {
        [Header("String Formats")]
        [Tooltip("Format => {0} = {Page} / {Total Page}")]
        public UILocaleKeySetting formatKeyPage = new UILocaleKeySetting(UIFormatKeys.UI_FORMAT_SIMPLE_MIN_BY_MAX);

        [Header("UI Elements")]
        public GameObject listEmptyObject;
        public UIAuction uiDialog;
        public UIAuction uiPrefab;
        public Transform uiContainer;
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

        private void OnEnable()
        {
            CacheSelectionManager.eventOnSelected.RemoveListener(OnSelect);
            CacheSelectionManager.eventOnSelected.AddListener(OnSelect);
            CacheSelectionManager.eventOnDeselected.RemoveListener(OnDeselect);
            CacheSelectionManager.eventOnDeselected.AddListener(OnDeselect);
            if (uiDialog != null)
                uiDialog.onHide.AddListener(OnDialogHide);
            page = 1;
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

        public void Refresh()
        {
            GoToPageRoutine(Page);
        }

        public void GoToPage(int page)
        {
            GoToPageRoutine(page);
        }

        private async void GoToPageRoutine(int page)
        {
            RestClient.Result<AuctionListResponse> result = await BaseGameNetworkManager.Singleton.AuctionRestClientForClient.GetHistoryList(limitPerPage, page);
            int selectedId = CacheSelectionManager.SelectedUI != null ? CacheSelectionManager.SelectedUI.Data.id : 0;
            CacheSelectionManager.DeselectSelectedUI();
            CacheSelectionManager.Clear();
            if (listEmptyObject != null)
                listEmptyObject.SetActive(true);
            if (result.IsNetworkError || result.IsHttpError || result.Content.list.Count == 0)
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
        }

        public void OnClickNextPage()
        {
            if (page + 1 > TotalPage)
                Page = TotalPage;
            else
                Page = Page + 1;
        }

        public void OnClickPreviousPage()
        {
            if (page - 1 < 1)
                Page = 1;
            else
                Page = Page - 1;
        }
    }
}
