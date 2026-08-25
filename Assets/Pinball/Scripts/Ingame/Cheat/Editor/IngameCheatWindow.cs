#if UNITY_EDITOR
using System.Collections.Generic;
using Alchemy.Editor;
using Alchemy.Inspector;
using Pinball.Ingame.Shop;
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

        public enum EPassiveCategoryFilter
        {
            All = 0,
            Ball = 1,
            Map = 2,
            Common = 3,
        }

        class ItemRow
        {
            public ShopItemData Item = null;
            public VisualElement Root = null;
            public Label CountLabel = null;
            public Button ApplyButton = null;
            public Button RemoveButton = null;
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
        const string BALL_CATEGORY_LABEL = "볼";
        const string MAP_CATEGORY_LABEL = "맵";
        const string COMMON_CATEGORY_LABEL = "공용";
        const string UNREGISTERED_LABEL = "미등록";
        const string UNREGISTERED_TOOLTIP = "ShopCatalogData에 등록되지 않아 상점 목록에는 뜨지 않는 아이템입니다.";
        const string APPLY_LABEL = "적용";
        const string REMOVE_LABEL = "제거";
        const string OWNED_SECTION_FORMAT = "■ 보유 중 ({0})";
        const string UNOWNED_SECTION_FORMAT = "□ 미보유 ({0})";
        const string EMPTY_LIST_LABEL = "조건에 맞는 아이템이 없습니다.";
        const string NOT_PLAYING_WARNING = "[IngameCheat] 플레이 중이 아니거나 인게임이 초기화되지 않았습니다.";
        const string APPLIED_LOG_FORMAT = "[IngameCheat] 적용: {0}";
        const string REMOVED_LOG_FORMAT = "[IngameCheat] 제거: {0}";
        const string NOT_OWNED_WARNING_FORMAT = "[IngameCheat] 보유하지 않은 아이템입니다: {0}";
        const string MONEY_LOG_FORMAT = "[IngameCheat] 돈 +{0}";
        const string STAGE_CLEAR_LOG = "[IngameCheat] 현재 스테이지 클리어";
        const string ROUND_CLEAR_LOG = "[IngameCheat] 현재 라운드 클리어";
        const string GAME_CLEAR_LOG = "[IngameCheat] 게임 클리어";
        const string STAGE_FAIL_LOG = "[IngameCheat] 현재 스테이지 패배";
        const string NOT_IN_STAGE_WARNING = "[IngameCheat] 스테이지 진행 중이 아닙니다. 일시정지 중이거나 상점/스테이지 선택 화면입니다.";
        const string SLOT_FULL_WARNING_FORMAT = "[IngameCheat] 패시브 슬롯이 가득 찼거나 이미 보유한 아이템입니다: {0}";
        const string INVALID_INPUT_WARNING = "[IngameCheat] 입력값이 올바르지 않습니다.";
        const string FILTER_KEY_FORMAT = "{0}|{1}|{2}";
        const int DEFAULT_MONEY_AMOUNT = 100;
        const int MIN_APPLY_COUNT = 1;
        const int INVALID_OWNED_COUNT = -1;
        const long REFRESH_INTERVAL_MS = 400;
        const float WINDOW_MIN_WIDTH = 420f;
        const float WINDOW_MIN_HEIGHT = 320f;
        const float KIND_LABEL_WIDTH = 74f;
        const float CATEGORY_LABEL_WIDTH = 40f;
        const float PRICE_LABEL_WIDTH = 64f;
        const float OWNED_LABEL_WIDTH = 56f;
        const float APPLY_BUTTON_WIDTH = 52f;
        const float REMOVE_BUTTON_WIDTH = 52f;
        const float ROW_PADDING = 3f;
        const float BADGE_PADDING = 4f;
        const float STATUS_PADDING = 6f;
        const float SECTION_HEADER_TOP_PADDING = 8f;
        const float SECTION_HEADER_BOTTOM_PADDING = 2f;
        const float SECTION_EMPTY_LEFT_PADDING = 12f;

        static readonly Color PASSIVE_COLOR = new Color(0.68f, 0.53f, 1f, 1f);
        static readonly Color UPGRADE_COLOR = new Color(0.35f, 0.82f, 0.68f, 1f);
        static readonly Color UNREGISTERED_COLOR = new Color(1f, 0.72f, 0.35f, 1f);
        static readonly Color BALL_CATEGORY_COLOR = new Color(0.44f, 0.72f, 1f, 1f);
        static readonly Color MAP_CATEGORY_COLOR = new Color(0.56f, 0.86f, 0.45f, 1f);
        static readonly Color COMMON_CATEGORY_COLOR = new Color(0.76f, 0.76f, 0.8f, 1f);
        static readonly Color SECTION_HEADER_COLOR = new Color(0.85f, 0.85f, 0.9f, 1f);

        /// <summary>
        /// 목록을 마지막으로 다시 만들 때 사용한 필터 키.
        /// AlchemyEditorWindow.CreateGUI는 만들어진 첫 요소를 항상 제거하므로(원래 대상인 EditorWindow의
        /// 직렬화 필드가 현재 Unity 버전에서는 생성되지 않음), UI가 필요 없는 이 필드를 맨 앞에 두어
        /// 실제 치트 UI가 지워지지 않게 한다. 선언 순서를 바꾸지 말 것.
        /// </summary>
        [SerializeField] string _appliedFilterKey = string.Empty;

        [SerializeField, BoxGroup(MONEY_GROUP), LabelText("추가할 금액")] int _moneyAmount = DEFAULT_MONEY_AMOUNT;
        [SerializeField, BoxGroup(ITEM_GROUP), LabelText("종류")] EKindFilter _kindFilter = EKindFilter.All;
        [SerializeField, BoxGroup(ITEM_GROUP), LabelText("패시브 분류"), ShowIf(nameof(ShowsPassiveCategoryFilter))] EPassiveCategoryFilter _passiveCategoryFilter = EPassiveCategoryFilter.All;
        [SerializeField, BoxGroup(ITEM_GROUP), LabelText("이름 검색")] string _searchText = string.Empty;
        [SerializeField, BoxGroup(ITEM_GROUP), LabelText("업그레이드 적용/제거 횟수")] int _applyCount = MIN_APPLY_COUNT;

        Label _statusLabel = null;
        Label _ownedHeaderLabel = null;
        Label _unownedHeaderLabel = null;
        Label _ownedEmptyLabel = null;
        Label _unownedEmptyLabel = null;
        VisualElement _ownedSection = null;
        VisualElement _unownedSection = null;
        List<ItemRow> _rows = null;
        ShopCatalogData _catalog = null;
        IngameCheatHandler _handler = null;

        bool ShowsPassiveCategoryFilter() => _kindFilter != EKindFilter.Upgrade;

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

            ScrollView scrollView = new ScrollView();
            scrollView.style.flexGrow = 1f;
            scrollView.style.marginTop = ROW_PADDING;
            rootVisualElement.Add(scrollView);

            _ownedHeaderLabel = CreateSectionHeaderLabel();
            _ownedSection = new VisualElement();
            _ownedEmptyLabel = CreateSectionEmptyLabel();
            scrollView.Add(_ownedHeaderLabel);
            scrollView.Add(_ownedSection);
            scrollView.Add(_ownedEmptyLabel);

            _unownedHeaderLabel = CreateSectionHeaderLabel();
            _unownedSection = new VisualElement();
            _unownedEmptyLabel = CreateSectionEmptyLabel();
            scrollView.Add(_unownedHeaderLabel);
            scrollView.Add(_unownedSection);
            scrollView.Add(_unownedEmptyLabel);

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

            _ownedSection = null;
            _unownedSection = null;
            _ownedHeaderLabel = null;
            _unownedHeaderLabel = null;
            _ownedEmptyLabel = null;
            _unownedEmptyLabel = null;
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

        [Button, BoxGroup(STAGE_GROUP), LabelText("현재 라운드 클리어")]
        void ClearRound()
        {
            IngameCheatHandler handler = GetHandler();
            if (null == handler)
            {
                Debug.LogWarning(NOT_PLAYING_WARNING);
                return;
            }

            IngameCheatHandler.EApplyResult result = handler.ClearRound();
            if (result != IngameCheatHandler.EApplyResult.Success)
            {
                LogFailure(result, string.Empty);
                return;
            }

            Debug.Log(ROUND_CLEAR_LOG);
        }

        [Button, BoxGroup(STAGE_GROUP), LabelText("게임 클리어")]
        void ClearGame()
        {
            IngameCheatHandler handler = GetHandler();
            if (null == handler)
            {
                Debug.LogWarning(NOT_PLAYING_WARNING);
                return;
            }

            IngameCheatHandler.EApplyResult result = handler.ClearGame();
            if (result != IngameCheatHandler.EApplyResult.Success)
            {
                LogFailure(result, string.Empty);
                return;
            }

            Debug.Log(GAME_CLEAR_LOG);
        }

        [Button, BoxGroup(STAGE_GROUP), LabelText("현재 스테이지 패배")]
        void FailStage()
        {
            IngameCheatHandler handler = GetHandler();
            if (null == handler)
            {
                Debug.LogWarning(NOT_PLAYING_WARNING);
                return;
            }

            IngameCheatHandler.EApplyResult result = handler.FailStage();
            if (result != IngameCheatHandler.EApplyResult.Success)
            {
                LogFailure(result, string.Empty);
                return;
            }

            Debug.Log(STAGE_FAIL_LOG);
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

            if (_appliedFilterKey != GetFilterKey())
            {
                RebuildRows();
                return;
            }

            UpdateOwnedCounts(handler);
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
                handler.GetTotalPassiveSlotCount(),
                handler.GetUpgradeKindCount());
        }

        void UpdateOwnedCounts(IngameCheatHandler handler)
        {
            if (null == _rows)
                return;

            bool sectionChanged = false;
            for (int i = 0; i < _rows.Count; ++i)
            {
                int ownedCount = null == handler ? 0 : handler.GetOwnedCount(_rows[i].Item);
                if (_rows[i].OwnedCount == ownedCount)
                    continue;

                bool wasOwned = _rows[i].OwnedCount > 0;
                _rows[i].OwnedCount = ownedCount;
                _rows[i].CountLabel.text = string.Format(OWNED_FORMAT, ownedCount);
                if (wasOwned == (ownedCount > 0))
                    continue;

                AttachRowToSection(_rows[i]);
                sectionChanged = true;
            }

            if (sectionChanged)
                UpdateSectionState();
        }

        void RebuildRows()
        {
            if (null == _ownedSection || null == _unownedSection || null == _rows)
                return;

            _appliedFilterKey = GetFilterKey();
            _rows.Clear();
            _ownedSection.Clear();
            _unownedSection.Clear();
            _catalog = LoadCatalog();

            IngameCheatHandler handler = GetHandler();
            string[] guids = AssetDatabase.FindAssets(ITEM_ASSET_FILTER);
            for (int i = 0; i < guids.Length; ++i)
            {
                ShopItemData item = AssetDatabase.LoadAssetAtPath<ShopItemData>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (!IsMatched(item))
                    continue;

                AddRow(item, null == handler ? 0 : handler.GetOwnedCount(item));
            }

            UpdateSectionState();
        }

        void AddRow(ShopItemData item, int ownedCount)
        {
            bool isPassive = item.Kind == ShopItemData.EItemKind.Passive;
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingTop = ROW_PADDING;
            row.style.paddingBottom = ROW_PADDING;

            row.Add(CreateBadgeLabel(isPassive ? PASSIVE_LABEL : UPGRADE_LABEL, isPassive ? PASSIVE_COLOR : UPGRADE_COLOR, KIND_LABEL_WIDTH));

            if (isPassive)
                row.Add(CreateBadgeLabel(GetCategoryLabel(item.PassiveCategory), GetCategoryColor(item.PassiveCategory), CATEGORY_LABEL_WIDTH));
            else
                row.Add(CreateSpacerElement(CATEGORY_LABEL_WIDTH));

            Label nameLabel = new Label(item.GetDisplayName());
            nameLabel.tooltip = item.GetDescription();
            nameLabel.style.flexGrow = 1f;
            nameLabel.style.overflow = Overflow.Hidden;
            row.Add(nameLabel);

            if (!IsRegisteredItem(item))
            {
                Label unregisteredLabel = CreateBadgeLabel(UNREGISTERED_LABEL, UNREGISTERED_COLOR, KIND_LABEL_WIDTH);
                unregisteredLabel.tooltip = UNREGISTERED_TOOLTIP;
                row.Add(unregisteredLabel);
            }

            Label priceLabel = new Label(string.Format(PRICE_FORMAT, item.Price));
            priceLabel.style.width = PRICE_LABEL_WIDTH;
            priceLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            row.Add(priceLabel);

            Label countLabel = new Label(string.Format(OWNED_FORMAT, ownedCount));
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
            itemRow.Root = row;
            itemRow.CountLabel = countLabel;
            itemRow.ApplyButton = applyButton;
            itemRow.RemoveButton = removeButton;
            itemRow.OwnedCount = ownedCount;
            _rows.Add(itemRow);
            AttachRowToSection(itemRow);
        }

        void AttachRowToSection(ItemRow row)
        {
            bool isOwned = row.OwnedCount > 0;
            row.ApplyButton.style.display = isOwned ? DisplayStyle.None : DisplayStyle.Flex;
            row.RemoveButton.style.display = isOwned ? DisplayStyle.Flex : DisplayStyle.None;

            row.Root.RemoveFromHierarchy();
            if (isOwned)
                _ownedSection.Add(row.Root);
            else
                _unownedSection.Add(row.Root);
        }

        void UpdateSectionState()
        {
            _ownedHeaderLabel.text = string.Format(OWNED_SECTION_FORMAT, _ownedSection.childCount);
            _unownedHeaderLabel.text = string.Format(UNOWNED_SECTION_FORMAT, _unownedSection.childCount);
            _ownedEmptyLabel.style.display = _ownedSection.childCount > 0 ? DisplayStyle.None : DisplayStyle.Flex;
            _unownedEmptyLabel.style.display = _unownedSection.childCount > 0 ? DisplayStyle.None : DisplayStyle.Flex;
        }

        VisualElement CreateSpacerElement(float width)
        {
            VisualElement spacer = new VisualElement();
            spacer.style.width = width;
            return spacer;
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

        Label CreateSectionHeaderLabel()
        {
            Label label = new Label(string.Empty);
            label.style.color = SECTION_HEADER_COLOR;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.paddingTop = SECTION_HEADER_TOP_PADDING;
            label.style.paddingBottom = SECTION_HEADER_BOTTOM_PADDING;
            return label;
        }

        Label CreateSectionEmptyLabel()
        {
            Label label = new Label(EMPTY_LIST_LABEL);
            label.style.paddingLeft = SECTION_EMPTY_LEFT_PADDING;
            label.style.paddingTop = ROW_PADDING;
            label.style.paddingBottom = ROW_PADDING;
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

            if (ShowsPassiveCategoryFilter() && !IsCategoryMatched(item))
                return false;

            if (string.IsNullOrEmpty(_searchText))
                return true;

            string keyword = _searchText.ToLowerInvariant();
            return item.GetDisplayName().ToLowerInvariant().Contains(keyword) || item.name.ToLowerInvariant().Contains(keyword);
        }

        bool IsCategoryMatched(ShopItemData item)
        {
            if (_passiveCategoryFilter == EPassiveCategoryFilter.All)
                return true;

            if (item.Kind != ShopItemData.EItemKind.Passive)
                return false;

            return item.PassiveCategory == ToPassiveCategory(_passiveCategoryFilter);
        }

        ShopItemData.EPassiveCategory ToPassiveCategory(EPassiveCategoryFilter filter)
        {
            switch (filter)
            {
                case EPassiveCategoryFilter.Ball:
                    return ShopItemData.EPassiveCategory.Ball;
                case EPassiveCategoryFilter.Map:
                    return ShopItemData.EPassiveCategory.Map;
                default:
                    return ShopItemData.EPassiveCategory.Common;
            }
        }

        string GetCategoryLabel(ShopItemData.EPassiveCategory category)
        {
            switch (category)
            {
                case ShopItemData.EPassiveCategory.Ball:
                    return BALL_CATEGORY_LABEL;
                case ShopItemData.EPassiveCategory.Map:
                    return MAP_CATEGORY_LABEL;
                default:
                    return COMMON_CATEGORY_LABEL;
            }
        }

        Color GetCategoryColor(ShopItemData.EPassiveCategory category)
        {
            switch (category)
            {
                case ShopItemData.EPassiveCategory.Ball:
                    return BALL_CATEGORY_COLOR;
                case ShopItemData.EPassiveCategory.Map:
                    return MAP_CATEGORY_COLOR;
                default:
                    return COMMON_CATEGORY_COLOR;
            }
        }

        string GetFilterKey()
        {
            return string.Format(FILTER_KEY_FORMAT, _kindFilter, _passiveCategoryFilter, _searchText);
        }
    }
}
#endif // UNITY_EDITOR
