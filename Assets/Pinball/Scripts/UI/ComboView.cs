using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Pinball.UI
{
    public class ComboView : UIViewBase
    {
        public record Args(int ComboCount, float Multiplier, int PendingBonus) : UIViewArgs
        {
            public override UIViewCatalogData.EViewType ViewType => UIViewCatalogData.EViewType.Combo;
        }

        public record BonusArgs(int BonusScore) : UIViewArgs
        {
            public override UIViewCatalogData.EViewType ViewType => UIViewCatalogData.EViewType.Combo;
        }

        const string COMBO_FORMAT = "{0} COMBO";
        const string MULTIPLIER_FORMAT = "x{0:0.##}";
        const string BONUS_FORMAT = "+{0}";
        const float BONUS_DISPLAY_SECONDS = 1.2f;

        [SerializeField] TMP_Text _comboLabel = null;
        [SerializeField] TMP_Text _multiplierLabel = null;
        [SerializeField] TMP_Text _bonusLabel = null;

        CancellationTokenSource _hideTokenSource = null;

        public override void Bind(UIViewArgs args)
        {
            Args comboArgs = args as Args;
            if (null != comboArgs)
            {
                BindCombo(comboArgs);
                return;
            }

            BonusArgs bonusArgs = args as BonusArgs;
            if (null != bonusArgs)
                BindBonus(bonusArgs);
        }

        public override void OnReturn()
        {
            CancelScheduledHide();
            _bonusLabel.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            CancelScheduledHide();
        }

        void BindCombo(Args args)
        {
            CancelScheduledHide();
            _comboLabel.text = string.Format(COMBO_FORMAT, args.ComboCount);
            _multiplierLabel.text = string.Format(MULTIPLIER_FORMAT, args.Multiplier);

            if (args.PendingBonus <= 0)
            {
                _bonusLabel.gameObject.SetActive(false);
                return;
            }

            _bonusLabel.text = string.Format(BONUS_FORMAT, args.PendingBonus);
            _bonusLabel.gameObject.SetActive(true);
        }

        void BindBonus(BonusArgs args)
        {
            CancelScheduledHide();
            _bonusLabel.text = string.Format(BONUS_FORMAT, args.BonusScore);
            _bonusLabel.gameObject.SetActive(true);
            _hideTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            HideAfterDelayAsync(_hideTokenSource.Token).Forget();
        }

        void CancelScheduledHide()
        {
            if (null == _hideTokenSource)
                return;

            _hideTokenSource.Cancel();
            _hideTokenSource.Dispose();
            _hideTokenSource = null;
        }

        async UniTaskVoid HideAfterDelayAsync(CancellationToken cancellationToken)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(BONUS_DISPLAY_SECONDS), cancellationToken: cancellationToken);
            UIManager.Hide(UIViewCatalogData.EViewType.Combo);
        }
    }
}
