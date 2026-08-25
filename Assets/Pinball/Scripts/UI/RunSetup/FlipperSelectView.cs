using Pinball.Ingame.Reward;
using Pinball.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.UI
{
    public class FlipperSelectView : UIViewBase
    {
        public record FlipperEntry(
            string NameKey,
            Sprite Icon,
            string AbilityKey,
            string DrawbackKey,
            bool IsUnlocked,
            int UnlockTier,
            int BestStageNumber,
            bool IsClearRecorded,
            int BestEndlessStageNumber);

        public record Args(FlipperEntry[] Flippers) : UIViewArgs
        {
            public override UIViewCatalogData.EViewType ViewType => UIViewCatalogData.EViewType.FlipperSelect;
        }

        const string PAGE_FORMAT = "{0} / {1}";
        const string LOCKED_NAME = "???";
        const string LOCKED_ABILITY = "LOCKED";
        const string LOCKED_ABILITY_KEY = "ui.flipper_select.locked_ability";
        const string DEFAULT_LINE = "STANDARD ISSUE";
        const string DEFAULT_LINE_KEY = "ui.flipper_select.default_line";
        const string UNLOCKED_REWARD_LINE = "REWARD: RUN CLEAR";
        const string UNLOCKED_REWARD_LINE_KEY = "ui.flipper_select.unlocked_line";
        const string FIRST_CLEAR_HINT = "CLEAR THE RUN TO UNLOCK";
        const string FIRST_CLEAR_HINT_KEY = "ui.lobby.unlock_hint_first";
        const string CLEAR_COUNT_HINT_FORMAT = "CLEAR THE RUN x{0} TO UNLOCK";
        const string CLEAR_COUNT_HINT_KEY = "ui.lobby.unlock_hint_count";
        const string BEST_STAGE_FORMAT = "BEST STAGE {0}";
        const string BEST_STAGE_KEY = "ui.flipper_select.best_stage";
        const string BEST_CLEAR_TEXT = "GAME CLEAR";
        const string BEST_CLEAR_KEY = "ui.flipper_select.best_clear";
        const string BEST_ENDLESS_FORMAT = "ENDLESS STAGE {0}";
        const string BEST_ENDLESS_KEY = "ui.flipper_select.best_endless";
        const int FIRST_CLEAR_TIER = 0;
        const int DEFAULT_INDEX = 0;
        const int NO_BEST_STAGE = 0;

        [SerializeField] FlipperCardView _card = null;
        [SerializeField] Button _prevButton = null;
        [SerializeField] Button _nextButton = null;
        [SerializeField] TMP_Text _pageLabel = null;
        [SerializeField] TMP_Text _flipperNameLabel = null;
        [SerializeField] TMP_Text _abilityLabel = null;
        [SerializeField] TMP_Text _drawbackLabel = null;
        [SerializeField] TMP_Text _unlockLineLabel = null;
        [SerializeField] TMP_Text _bestStageLabel = null;
        [SerializeField] Button _playButton = null;
        [SerializeField] Button _backButton = null;

        Args _args = null;
        int _selectedIndex = DEFAULT_INDEX;

        public override void OnGenerate()
        {
            _prevButton.onClick.AddListener(OnPrevClicked);
            _nextButton.onClick.AddListener(OnNextClicked);
            _playButton.onClick.AddListener(OnPlayClicked);
            _backButton.onClick.AddListener(OnBackClicked);
        }

        public override void OnTerminate()
        {
            _prevButton.onClick.RemoveListener(OnPrevClicked);
            _nextButton.onClick.RemoveListener(OnNextClicked);
            _playButton.onClick.RemoveListener(OnPlayClicked);
            _backButton.onClick.RemoveListener(OnBackClicked);
        }

        public override void OnReturn()
        {
            _args = null;
        }

        public override void Bind(UIViewArgs args)
        {
            Args viewArgs = args as Args;
            if (null == viewArgs)
                return;

            _args = viewArgs;
            _selectedIndex = WrapFlipperIndex(_selectedIndex);
            Refresh();
        }

        void Refresh()
        {
            FlipperEntry flipper = GetFlipper(_selectedIndex);
            bool isUnlocked = null != flipper && flipper.IsUnlocked;
            string hint = GetUnlockHint(flipper);
            string displayName = null == flipper ? string.Empty : LocalizationTable.GetOrKey(flipper.NameKey);

            _card.Bind(new FlipperCardView.BindParam(
                DisplayName: displayName,
                Icon: null == flipper ? null : flipper.Icon,
                IsUnlocked: isUnlocked,
                LockedHint: hint));
            _pageLabel.text = string.Format(PAGE_FORMAT, _selectedIndex + 1, GetFlipperCount());
            _flipperNameLabel.text = isUnlocked ? displayName : LOCKED_NAME;
            _abilityLabel.text = isUnlocked ? LocalizationTable.GetOrKey(flipper.AbilityKey) : LocalizationTable.Get(LOCKED_ABILITY_KEY, LOCKED_ABILITY);
            BindDrawback(isUnlocked ? LocalizationTable.GetOrKey(flipper.DrawbackKey) : string.Empty);
            _unlockLineLabel.text = GetUnlockLine(isUnlocked, hint);
            BindBestRecord(isUnlocked ? flipper : null);
            _playButton.interactable = isUnlocked;

            bool canCycle = GetFlipperCount() > 1;
            _prevButton.interactable = canCycle;
            _nextButton.interactable = canCycle;
        }

        void BindBestRecord(FlipperEntry flipper)
        {
            string recordText = GetBestRecordText(flipper);
            bool hasBest = !string.IsNullOrEmpty(recordText);
            _bestStageLabel.gameObject.SetActive(hasBest);
            if (hasBest)
                _bestStageLabel.text = recordText;
        }

        static string GetBestRecordText(FlipperEntry flipper)
        {
            if (null == flipper)
                return string.Empty;

            if (flipper.BestEndlessStageNumber > NO_BEST_STAGE)
                return LocalizationTable.Format(BEST_ENDLESS_KEY, BEST_ENDLESS_FORMAT, flipper.BestEndlessStageNumber);

            if (flipper.IsClearRecorded)
                return LocalizationTable.Get(BEST_CLEAR_KEY, BEST_CLEAR_TEXT);

            if (flipper.BestStageNumber > NO_BEST_STAGE)
                return LocalizationTable.Format(BEST_STAGE_KEY, BEST_STAGE_FORMAT, flipper.BestStageNumber);

            return string.Empty;
        }

        void BindDrawback(string drawback)
        {
            _drawbackLabel.gameObject.SetActive(!string.IsNullOrEmpty(drawback));
            _drawbackLabel.text = drawback;
        }

        static string GetUnlockHint(FlipperEntry flipper)
        {
            if (null == flipper)
                return string.Empty;

            if (flipper.UnlockTier == ClearRewardCatalogData.INVALID_TIER)
                return string.Empty;

            if (flipper.UnlockTier <= FIRST_CLEAR_TIER)
                return LocalizationTable.Get(FIRST_CLEAR_HINT_KEY, FIRST_CLEAR_HINT);

            return LocalizationTable.Format(CLEAR_COUNT_HINT_KEY, CLEAR_COUNT_HINT_FORMAT, flipper.UnlockTier + 1);
        }

        static string GetUnlockLine(bool isUnlocked, string hint)
        {
            if (!isUnlocked)
                return hint;

            return string.IsNullOrEmpty(hint) ? LocalizationTable.Get(DEFAULT_LINE_KEY, DEFAULT_LINE) : LocalizationTable.Get(UNLOCKED_REWARD_LINE_KEY, UNLOCKED_REWARD_LINE);
        }

        int GetFlipperCount()
        {
            if (null == _args || null == _args.Flippers)
                return 0;

            return _args.Flippers.Length;
        }

        FlipperEntry GetFlipper(int index)
        {
            if (index < 0 || index >= GetFlipperCount())
                return null;

            return _args.Flippers[index];
        }

        int WrapFlipperIndex(int index)
        {
            int count = GetFlipperCount();
            if (count <= 0)
                return DEFAULT_INDEX;

            return ((index % count) + count) % count;
        }

        void OnPrevClicked()
        {
            _selectedIndex = WrapFlipperIndex(_selectedIndex - 1);
            Refresh();
        }

        void OnNextClicked()
        {
            _selectedIndex = WrapFlipperIndex(_selectedIndex + 1);
            Refresh();
        }

        void OnPlayClicked()
        {
            FlipperEntry flipper = GetFlipper(_selectedIndex);
            if (null == flipper || !flipper.IsUnlocked)
                return;

            int flipperIndex = _selectedIndex;
            UIManager.Hide(UIViewCatalogData.EViewType.FlipperSelect);
            UIManager.RaiseEvent(UIManager.EEvent.FlipperSelectPlayRequested, flipperIndex);
        }

        void OnBackClicked()
        {
            UIManager.Hide(UIViewCatalogData.EViewType.FlipperSelect);
        }
    }
}
