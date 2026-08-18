using Pinball.Ingame;
using Pinball.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.UI
{
    public class FlipperSelectView : UIViewBase
    {
        public record Args(bool[] IsFlipperUnlocked, string[] UnlockHints) : UIViewArgs
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

        [SerializeField] FlipperCatalogData _flipperCatalog = null;
        [SerializeField] FlipperCardView _card = null;
        [SerializeField] Button _prevButton = null;
        [SerializeField] Button _nextButton = null;
        [SerializeField] TMP_Text _pageLabel = null;
        [SerializeField] TMP_Text _flipperNameLabel = null;
        [SerializeField] TMP_Text _abilityLabel = null;
        [SerializeField] TMP_Text _unlockLineLabel = null;
        [SerializeField] Button _playButton = null;
        [SerializeField] Button _backButton = null;

        Args _args = null;
        int _selectedIndex = FlipperCatalogData.DEFAULT_INDEX;

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
            FlipperData flipper = GetFlipper(_selectedIndex);
            bool isUnlocked = IsUnlocked(_selectedIndex);
            string hint = GetUnlockHint(_selectedIndex);

            _card.Bind(new FlipperCardView.BindParam(
                DisplayName: GetDisplayName(flipper),
                Icon: null == flipper ? null : flipper.GetIcon(),
                IsUnlocked: isUnlocked,
                LockedHint: hint));
            _pageLabel.text = string.Format(PAGE_FORMAT, _selectedIndex + 1, GetFlipperCount());
            _flipperNameLabel.text = isUnlocked ? GetDisplayName(flipper) : LOCKED_NAME;
            _abilityLabel.text = isUnlocked ? GetAbilityDescription(flipper) : LocalizationTable.Get(LOCKED_ABILITY_KEY, LOCKED_ABILITY);
            _unlockLineLabel.text = GetUnlockLine(isUnlocked, hint);
            _playButton.interactable = isUnlocked;

            bool canCycle = GetFlipperCount() > 1;
            _prevButton.interactable = canCycle;
            _nextButton.interactable = canCycle;
        }

        static string GetDisplayName(FlipperData flipper)
        {
            return null == flipper ? string.Empty : flipper.GetDisplayName();
        }

        static string GetAbilityDescription(FlipperData flipper)
        {
            return null == flipper ? string.Empty : flipper.GetAbilityDescription();
        }

        static string GetUnlockLine(bool isUnlocked, string hint)
        {
            if (!isUnlocked)
                return hint;

            return string.IsNullOrEmpty(hint) ? LocalizationTable.Get(DEFAULT_LINE_KEY, DEFAULT_LINE) : LocalizationTable.Get(UNLOCKED_REWARD_LINE_KEY, UNLOCKED_REWARD_LINE);
        }

        int GetFlipperCount()
        {
            return null == _flipperCatalog ? 0 : _flipperCatalog.Count;
        }

        FlipperData GetFlipper(int index)
        {
            return null == _flipperCatalog ? null : _flipperCatalog.GetFlipper(index);
        }

        bool IsUnlocked(int index)
        {
            if (null == _args || null == _args.IsFlipperUnlocked)
                return index == FlipperCatalogData.DEFAULT_INDEX;

            if (index < 0 || index >= _args.IsFlipperUnlocked.Length)
                return false;

            return _args.IsFlipperUnlocked[index];
        }

        string GetUnlockHint(int index)
        {
            if (null == _args || null == _args.UnlockHints)
                return string.Empty;

            if (index < 0 || index >= _args.UnlockHints.Length)
                return string.Empty;

            return _args.UnlockHints[index] ?? string.Empty;
        }

        int WrapFlipperIndex(int index)
        {
            int count = GetFlipperCount();
            if (count <= 0)
                return FlipperCatalogData.DEFAULT_INDEX;

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
            if (!IsUnlocked(_selectedIndex))
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
