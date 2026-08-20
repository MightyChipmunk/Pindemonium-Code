#if UNITY_EDITOR
using System.Collections.Generic;
using Alchemy.Editor;
using Alchemy.Inspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pinball.Ingame.Cheat.Editor
{
    public class IngameCheatWindow : AlchemyEditorWindow
    {
        public enum EKindFilter
        {
            All = 0,
            Passive = 1,
            Upgrade = 2,
        }

        class ItemRow
        {
            public ShopItemData Item = null;
            public Label CountLabel = null;
            public int OwnedCount = INVALID_OWNED_COUNT;
        }

        const string MENU_PATH = "Window/Pinball Cheat";
        const string WINDOW_TITLE = "Pinball Cheat";
        const string STAGE_GROUP = "스테이지";
        const string MONEY_GROUP = "돈";
        const string ITEM_GROUP = "아이템 목록 필터";
        const string ITEM_ASSET_FILTER = "t:ShopItemData";
        const string CATALOG_ASSET_FILTER = "t:ShopCatalogData";
        const string READY_STATUS_FORMAT = "MONEY {0}      PASSIVE {1}/{2}      UPGRADE {3}종";
        const string IDLE_STATUS = "플레이 중이 아닙니다. Ingame 씬을 실행하면 활성화됩니다.";
        const string OWNED_FORMAT = "보유 {0}";
        const string PRICE_FORMAT = "COST {0}";
        const string PASSIVE_LABEL = "PASSIVE";
        const string UPGRADE_LABEL = "UPGRADE";
        const string UNREGISTERED_LABEL = "미등록";
        const string UNREGISTERED_TOOLTIP = "ShopCatalogData에 등록되지 않아 상점 목록에는 뜨지 않는 아이템입니다.";
        const string APPLY_LABEL = "적용";
        const string REMOVE_LABEL = "제거";
        const string EMPTY_LIST_LABEL = "조건에 맞는 아이템이 없습니다.";
        const string NOT_PLAYING_WARNING = "[IngameCheat] 플레이 중이 아니거나 인게임이 초기화되지 않았습니다.";
        const string APPLIED_LOG_FORMAT = "[IngameCheat] 적용: {0}";
        const string REMOVED_LOG_FORMAT = "[IngameCheat] 제거: {0}";
        const string NOT_OWNED_WARNING_FORMAT = "[IngameCheat] 보유하지 않은 아이템입니다: {0}";
        const string MONEY_LOG_FORMAT = "[IngameCheat] 돈 +{0}";
        const string STAGE_CLEAR_LOG = "[IngameCheat] 현재 스테이지 클리어";
        const string NOT_IN_STAGE_WARNING = "[IngameCheat] 스테이지 진행 중이 아닙니다. 일시정지 중이거나 상점/스테이지 선택 화면입니다.";
        const string SLOT_FULL_WARNING_FORMAT = "[IngameCheat] 패시브 슬롯이 가득 찼거나 이미 보유한 아이템입니다: {0}";
        const string INVALID_INPUT_WARNING = "[IngameCheat] 입력값이 올바르지 않습니다.";
        const string FILTER_KEY_FORMAT = "{0}|{1}";
        const int DEFAULT_MONEY_AMOUNT = 100;
        const int MIN_APPLY_COUNT = 1;
        const int INVALID_OWNED_COUNT = -1;
        const long REFRESH_INTERVAL_MS = 400;
        const float WINDOW_MIN_WIDTH = 420f;
        const float WINDOW_MIN_HEIGHT = 320f;
        const float KIND_LABEL_WIDTH = 74f;
        const float PRICE_LABEL_WIDTH = 64f;
        const float OWNED_LABEL_WIDTH = 56f;
        const float APPLY_BUTTON_WIDTH = 52f;
        const float REMOVE_BUTTON_WIDTH = 52f;
        const float ROW_PADDING = 3f;
        const float BADGE_PADDING = 4f;
        const float STATUS_PADDING = 6f;

        static readonly Color PASSIVE_COLOR = new Color(0.68f, 0.53f, 1f, 1f);
        static readonly Color UPGRADE_COLOR = new Color(0.35f, 0.82f, 0.68f, 1f);
        static readonly Color UNREGISTERED_COLOR = new Color(1f, 0.72f, 0.35f, 1f);

        /// <summary>
        /// 목록을 마지막으로 다시 만들 때 사용한 필터 키.
        /// AlchemyEditorWindow.CreateGUI는 만들어진 첫 요소를 항상 제거하므로(원래 대상인 EditorWindow의
        /// 직렬화 필드가 현재 Unity 버전에서는 생성되지 않음), UI가 필요 없는 이 필드를 맨 앞에 두어
        /// 실제 치트 UI가 지워지지 않게 한다. 선언 순서를 바꾸지 말 것.
        /// </summary>
        [SerializeField] string _appliedFilterKey = string.Empty;

        [SerializeField, BoxGroup(MONEY_GROUP), LabelText("추가할 금액")] int _moneyAmount = DEFAULT_MONEY_AMOUNT;
        [SerializeField, BoxGroup(ITEM_GROUP), LabelText("종류")] EKindFilter _kindFilter = EKindFilter.All;
        [SerializeField, BoxGroup(ITEM_GROUP), LabelText("이름 검색")] string _searchText = string.Empty;
        [SerializeField, BoxGroup(ITEM_GROUP), LabelText("업그레이드 적용/제거 횟수")] int _applyCount = MIN_APPLY_COUNT;

        Label _statusLabel = null;
        ScrollView _listView = null;
        List<ItemRow> _rows = null;
        ShopCatalogData _catalog = null;
        IngameCheatHandler _handler = null;

        [MenuItem(MENU_PATH)]
        public static void OpenWindow()
        {
            IngameCheatWindow window = GetWindow<IngameCheatWindow>();
            window.titleContent = new GUIContent(WINDOW_TITLE);
            window.minSize = new Vector2(WINDOW_MIN_WIDTH, WINDOW_MIN_HEIGHT);
        }

        protected override void CreateGUI()
        {
            base.CreateGUI();

            _rows = new List<ItemRow>();
            _statusLabel = CreateStatusLabel();
            rootVisualElement.Insert(0, _statusLabel);

            _listView = new ScrollView();
            _listView.style.flexGrow = 1f;
            _listView.style.marginTop = ROW_PADDING;
            rootVisualElement.Add(_listView);

            RebuildRows();
            rootVisualElement.schedule.Execute(Refresh).Every(REFRESH_INTERVAL_MS);
        }

        void OnDestroy()
        {
            if (null != _rows)
            {
                _rows.Clear();
                _rows = null;
            }

            _listView = null;
            _statusLabel = null;
            _catalog = null;
            _handler = null;
        }

        [Button, BoxGroup(MONEY_GROUP), LabelText("돈 추가")]
        void AddMoney()
        {
            IngameCheatHandler handler = GetHandler();
            if (null == handler)
            {
                Debug.LogWarning(NOT_PLAYING_WARNING);
                return;
            }

            IngameCheatHandler.EApplyResult result = handler.AddMoney(_moneyAmount);
            if (result != IngameCheatHandler.EApplyResult.Success)
            {
                LogFailure(result, string.Empty);
                return;
            }

            Debug.Log(string.Format(MONEY_LOG_FORMAT, _moneyAmount));
        }

        [Button, BoxGroup(STAGE_GROUP), LabelText("현재 스테이지 클리어")]
        void ClearStage()
        {
            IngameCheatHandler handler = GetHandler();
            if (null == handler)
            {
                Debug.LogWarning(NOT_PLAYING_WARNING);
                return;
            }

            IngameCheatHandler.EApplyResult result = handler.ClearStage();
            if (result != IngameCheatHandler.EApplyResult.Success)
            {
                LogFailure(result, string.Empty);
                return;
            }

            Debug.Log(STAGE_CLEAR_LOG);
        }

        [Button, BoxGroup(ITEM_GROUP), LabelText("목록 새로고침")]
        void ReloadItems()
        {
            RebuildRows();
        }

        void Refresh()
        {
            IngameCheatHandler handler = GetHandler();
            UpdateStatus(handler);
            UpdateOwnedCounts(handler);

            if (_appliedFilterKey == GetFilterKey())
                return;

            RebuildRows();
        }

        void UpdateStatus(IngameCheatHandler handler)
        {
            if (null == _statusLabel)
                return;

            if (null == handler)
            {
                _statusLabel.text = IDLE_STATUS;
                return;
            }

            _statusLabel.text = string.Format(READY_STATUS_FORMAT,
                handler.GetMoney(),
                handler.GetPassiveCount(),
                handler.GetPassiveSlotCount(),
                handler.GetUpgradeKindCount());
        }

        void UpdateOwnedCounts(IngameCheatHandler handler)
        {
            if (null == _rows)
                return;

            for (int i = 0; i < _rows.Count; ++i)
            {
                int ownedCount = null == handler ? 0 : handler.GetOwnedCount(_rows[i].Item);
                if (_rows[i].OwnedCount == ownedCount)
                    continue;

                _rows[i].OwnedCount = ownedCount;
                _rows[i].CountLabel.text = string.Format(OWNED_FORMAT, ownedCount);
            }
        }

        void RebuildRows()
        {
            if (null == _listView || null == _rows)
                return;

            _appliedFilterKey = GetFilterKey();
            _rows.Clear();
            _listView.Clear();
            _catalog = LoadCatalog();

            string[] guids = AssetDatabase.FindAssets(ITEM_ASSET_FILTER);
            for (int i = 0; i < guids.Length; ++i)
            {
                ShopItemData item = AssetDatabase.LoadAssetAtPath<ShopItemData>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (!IsMatched(item))
                    continue;

                _listView.Add(CreateRowElement(item));
            }

            if (_rows.Count <= 0)
                _listView.Add(new Label(EMPTY_LIST_LABEL));
        }

        VisualElement CreateRowElement(ShopItemData item)
        {
            bool isPassive = item.Kind == ShopItemData.EItemKind.Passive;
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingTop = ROW_PADDING;
            row.style.paddingBottom = ROW_PADDING;

            row.Add(CreateBadgeLabel(isPassive ? PASSIVE_LABEL : UPGRADE_LABEL, isPassive ? PASSIVE_COLOR : UPGRADE_COLOR, KIND_LABEL_WIDTH));

            Label nameLabel = new Label(item.GetDisplayName());
            nameLabel.tooltip = item.GetDescription();
            nameLabel.style.flexGrow = 1f;
            nameLabel.style.overflow = Overflow.Hidden;
            row.Add(nameLabel);

            if (!IsRegisteredItem(item))
                row.Add(CreateBadgeLabel(UNREGISTERED_LABEL, UNREGISTERED_COLOR, KIND_LABEL_WIDTH));

            Label priceLabel = new Label(string.Format(PRICE_FORMAT, item.Price));
            priceLabel.style.width = PRICE_LABEL_WIDTH;
            priceLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            row.Add(priceLabel);

            Label countLabel = new Label(string.Format(OWNED_FORMAT, 0));
            countLabel.style.width = OWNED_LABEL_WIDTH;
            countLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            row.Add(countLabel);

            Button applyButton = new Button(() => ApplyItem(item));
            applyButton.text = APPLY_LABEL;
            applyButton.style.width = APPLY_BUTTON_WIDTH;
            row.Add(applyButton);

            Button removeButton = new Button(() => RemoveItem(item));
            removeButton.text = REMOVE_LABEL;
            removeButton.style.width = REMOVE_BUTTON_WIDTH;
            row.Add(removeButton);

            ItemRow itemRow = new ItemRow();
            itemRow.Item = item;
            itemRow.CountLabel = countLabel;
            _rows.Add(itemRow);
            return row;
        }

        Label CreateBadgeLabel(string text, Color color, float width)
        {
            Label label = new Label(text);
            label.style.width = width;
            label.style.color = color;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.paddingLeft = BADGE_PADDING;
            label.style.paddingRight = BADGE_PADDING;
            return label;
        }

        Label CreateStatusLabel()
        {
            Label label = new Label(IDLE_STATUS);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.paddingTop = STATUS_PADDING;
            label.style.paddingBottom = STATUS_PADDING;
            label.style.paddingLeft = STATUS_PADDING;
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }

        void ApplyItem(ShopItemData item)
        {
            IngameCheatHandler handler = GetHandler();
            if (null == handler)
            {
                Debug.LogWarning(NOT_PLAYING_WARNING);
                return;
            }

            IngameCheatHandler.EApplyResult result = handler.ApplyItem(item, _applyCount);
            if (result != IngameCheatHandler.EApplyResult.Success)
            {
                LogFailure(result, item.GetDisplayName());
                return;
            }

            Debug.Log(string.Format(APPLIED_LOG_FORMAT, item.GetDisplayName()));
        }

        void RemoveItem(ShopItemData item)
        {
            IngameCheatHandler handler = GetHandler();
            if (null == handler)
            {
                Debug.LogWarning(NOT_PLAYING_WARNING);
                return;
            }

            IngameCheatHandler.EApplyResult result = handler.RemoveItem(item, _applyCount);
            if (result != IngameCheatHandler.EApplyResult.Success)
            {
                LogFailure(result, item.GetDisplayName());
                return;
            }

            Debug.Log(string.Format(REMOVED_LOG_FORMAT, item.GetDisplayName()));
        }

        void LogFailure(IngameCheatHandler.EApplyResult result, string itemName)
        {
            switch (result)
            {
                case IngameCheatHandler.EApplyResult.NotReady:
                    Debug.LogWarning(NOT_PLAYING_WARNING);
                    break;
                case IngameCheatHandler.EApplyResult.PassiveSlotFull:
                    Debug.LogWarning(string.Format(SLOT_FULL_WARNING_FORMAT, itemName));
                    break;
                case IngameCheatHandler.EApplyResult.NotInStage:
                    Debug.LogWarning(NOT_IN_STAGE_WARNING);
                    break;
                case IngameCheatHandler.EApplyResult.NotOwned:
                    Debug.LogWarning(string.Format(NOT_OWNED_WARNING_FORMAT, itemName));
                    break;
                default:
                    Debug.LogWarning(INVALID_INPUT_WARNING);
                    break;
            }
        }

        IngameCheatHandler GetHandler()
        {
            if (!EditorApplication.isPlaying)
                return null;

            if (null == _handler)
                _handler = FindAnyObjectByType<IngameCheatHandler>();

            if (null == _handler || !_handler.IsReady())
                return null;

            return _handler;
        }

        ShopCatalogData LoadCatalog()
        {
            string[] guids = AssetDatabase.FindAssets(CATALOG_ASSET_FILTER);
            if (guids.Length <= 0)
                return null;

            return AssetDatabase.LoadAssetAtPath<ShopCatalogData>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        bool IsRegisteredItem(ShopItemData item)
        {
            if (null == _catalog)
                return true;

            return _catalog.IndexOf(item) != ShopCatalogData.INVALID_INDEX;
        }

        bool IsMatched(ShopItemData item)
        {
            if (null == item)
                return false;

            if (_kindFilter == EKindFilter.Passive && item.Kind != ShopItemData.EItemKind.Passive)
                return false;

            if (_kindFilter == EKindFilter.Upgrade && item.Kind != ShopItemData.EItemKind.Upgrade)
                return false;

            if (string.IsNullOrEmpty(_searchText))
                return true;

            string keyword = _searchText.ToLowerInvariant();
            return item.GetDisplayName().ToLowerInvariant().Contains(keyword) || item.name.ToLowerInvariant().Contains(keyword);
        }

        string GetFilterKey()
        {
            return string.Format(FILTER_KEY_FORMAT, _kindFilter, _searchText);
        }
    }
}
#endif // UNITY_EDITOR
