using System.Globalization;
using Pinball.Ingame;
using Pinball.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.UI
{
    public class ContinueRunView : UIViewBase
    {
        public record Args(
            int FlipperIndex,
            int StageNumber,
            int TotalStageCount,
            int Money,
            int Score,
            string GimmickText) : UIViewArgs
        {
            public override UIViewCatalogData.EViewType ViewType => UIViewCatalogData.EViewType.ContinueRun;
        }

        const string STAGE_FORMAT = "{0} OF {1}";
        const string STAGE_KEY = "ui.continue.stage";
        const string MONEY_FORMAT = "${0}";
        const string SCORE_FORMAT = "N0";
        const string EMPTY_GIMMICK_TEXT = "NONE";
        const string EMPTY_GIMMICK_KEY = "ui.continue.empty_gimmick";

        [SerializeField] FlipperCatalogData _flipperCatalog = null;
        [SerializeField] FlipperCardView _card = null;
        [SerializeField] TMP_Text _stageLabel = null;
        [SerializeField] TMP_Text _moneyLabel = null;
        [SerializeField] TMP_Text _scoreLabel = null;
        [SerializeField] TMP_Text _gimmickLabel = null;
        [SerializeField] Button _continueButton = null;
        [SerializeField] Button _backButton = null;

        public override void OnGenerate()
        {
            _continueButton.onClick.AddListener(OnContinueClicked);
            _backButton.onClick.AddListener(OnBackClicked);
        }

        public override void OnTerminate()
        {
            _continueButton.onClick.RemoveListener(OnContinueClicked);
            _backButton.onClick.RemoveListener(OnBackClicked);
        }

        public override void Bind(UIViewArgs args)
        {
            Args viewArgs = args as Args;
            if (null == viewArgs)
                return;

            FlipperData flipper = null == _flipperCatalog ? null : _flipperCatalog.GetFlipper(viewArgs.FlipperIndex);
            _card.Bind(new FlipperCardView.BindParam(
                DisplayName: null == flipper ? string.Empty : flipper.GetDisplayName(),
                Icon: null == flipper ? null : flipper.GetIcon(),
                IsUnlocked: true,
                LockedHint: string.Empty));
            _stageLabel.text = LocalizationTable.Format(STAGE_KEY, STAGE_FORMAT, viewArgs.StageNumber, viewArgs.TotalStageCount);
            _moneyLabel.text = string.Format(MONEY_FORMAT, viewArgs.Money);
            _scoreLabel.text = viewArgs.Score.ToString(SCORE_FORMAT, CultureInfo.InvariantCulture);
            _gimmickLabel.text = string.IsNullOrEmpty(viewArgs.GimmickText) ? LocalizationTable.Get(EMPTY_GIMMICK_KEY, EMPTY_GIMMICK_TEXT) : viewArgs.GimmickText;
        }

        void OnContinueClicked()
        {
            UIManager.Hide(UIViewCatalogData.EViewType.ContinueRun);
            UIManager.RaiseEvent(UIManager.EEvent.ContinueRunRequested);
        }

        void OnBackClicked()
        {
            UIManager.Hide(UIViewCatalogData.EViewType.ContinueRun);
        }
    }
}
