using Pinball.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.UI
{
    public class FlipperSelectView : UIViewBase
    {
        public record FlipperEntry(
            string DisplayName,
            Sprite Icon,
            string AbilityDescription,
            string DrawbackDescription,
            bool IsUnlocked,
            string UnlockHint);

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
        const int DEFAULT_INDEX = 0;

        [SerializeField] FlipperCardView _card = null;
        [SerializeField] Button _prevButton = null;
        [SerializeField] Button _nextButton = null;
        [SerializeField] TMP_Text _pageLabel = null;
        [SerializeField] TMP_Text _flipperNameLabel = null;
        [SerializeField] TMP_Text _abilityLabel = null;
        [SerializeField] TMP_Text _drawbackLabel = null;
        [SerializeField] TMP_Text _unlockLineLabel = null;
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
            string hint = null == flipper || null == flipper.UnlockHint ? string.Empty : flipper.UnlockHint;
            string displayName = null == flipper ? string.Empty : flipper.DisplayName;

            _card.Bind(new FlipperCardView.BindParam(
                DisplayName: displayName,
                Icon: null == flipper ? null : flipper.Icon,
                IsUnlocked: isUnlocked,
                LockedHint: hint));
            _pageLabel.text = string.Format(PAGE_FORMAT, _selectedIndex + 1, GetFlipperCount());
            _flipperNameLabel.text = isUnlocked ? displayName : LOCKED_NAME;
            _abilityLabel.text = isUnlocked ? flipper.AbilityDescription : LocalizationTable.Get(LOCKED_ABILITY_KEY, LOCKED_ABILITY);
            BindDrawback(isUnlocked ? flipper.DrawbackDescription : string.Empty);
            _unlockLineLabel.text = GetUnlockLine(isUnlocked, hint);
            _playButton.interactable = isUnlocked;

            bool canCycle = GetFlipperCount() > 1;
            _prevButton.interactable = canCycle;
            _nextButton.interactable = canCycle;
        }

        void BindDrawback(string drawback)
        {
            _drawbackLabel.gameObject.SetActive(!string.IsNullOrEmpty(drawback));
            _drawbackLabel.text = drawback;
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
