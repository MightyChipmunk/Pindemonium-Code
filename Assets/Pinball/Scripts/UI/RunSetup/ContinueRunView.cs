using System.Globalization;
using System.Text;
using Pinball.Ingame.Stage;
using Pinball.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.UI
{
    public class ContinueRunView : UIViewBase
    {
        public record Args(
            string FlipperNameKey,
            Sprite FlipperIcon,
            int StageNumber,
            bool IsEndless,
            int TotalStageCount,
            int Money,
            int Score,
            StageGimmickData[] Gimmicks) : UIViewArgs
        {
            public override UIViewCatalogData.EViewType ViewType => UIViewCatalogData.EViewType.ContinueRun;
        }

        const string STAGE_FORMAT = "{0} OF {1}";
        const string STAGE_KEY = "ui.continue.stage";
        const string ENDLESS_STAGE_FORMAT = "ENDLESS {0}";
        const string ENDLESS_STAGE_KEY = "ui.continue.endless_stage";
        const string MONEY_FORMAT = "${0}";
        const string SCORE_FORMAT = "N0";
        const string EMPTY_GIMMICK_TEXT = "NONE";
        const string EMPTY_GIMMICK_KEY = "ui.continue.empty_gimmick";
        const string GIMMICK_SEPARATOR = " / ";

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

            _card.Bind(new FlipperCardView.BindParam(
                DisplayName: LocalizationTable.GetOrKey(viewArgs.FlipperNameKey),
                Icon: viewArgs.FlipperIcon,
                IsUnlocked: true,
                LockedHint: string.Empty));
            _stageLabel.text = viewArgs.IsEndless
                ? LocalizationTable.Format(ENDLESS_STAGE_KEY, ENDLESS_STAGE_FORMAT, viewArgs.StageNumber)
                : LocalizationTable.Format(STAGE_KEY, STAGE_FORMAT, viewArgs.StageNumber, viewArgs.TotalStageCount);
            _moneyLabel.text = string.Format(MONEY_FORMAT, viewArgs.Money);
            _scoreLabel.text = viewArgs.Score.ToString(SCORE_FORMAT, CultureInfo.InvariantCulture);
            BindGimmicks(viewArgs.Gimmicks);
        }

        void BindGimmicks(StageGimmickData[] gimmicks)
        {
            StringBuilder builder = new StringBuilder();
            int gimmickCount = null == gimmicks ? 0 : gimmicks.Length;
            for (int i = 0; i < gimmickCount; ++i)
            {
                if (null == gimmicks[i])
                    continue;

                if (builder.Length > 0)
                    builder.Append(GIMMICK_SEPARATOR);

                builder.Append(StageGimmickLabelFormatter.Format(gimmicks[i]));
            }

            _gimmickLabel.text = builder.Length > 0 ? builder.ToString() : LocalizationTable.Get(EMPTY_GIMMICK_KEY, EMPTY_GIMMICK_TEXT);
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
