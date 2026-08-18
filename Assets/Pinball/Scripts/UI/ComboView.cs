using System;
using System.Globalization;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Pinball.UI
{
    public class ComboView : MonoBehaviour
    {
        const string MULTIPLIER_FORMAT = "x{0:0.##}";
        const string BONUS_FORMAT = "+{0}";
        const float BONUS_DISPLAY_SECONDS = 1.2f;
        const int INITIAL_COMBO_COUNT = 0;

        [SerializeField] GameObject _root = null;
        [SerializeField] TMP_Text _comboLabel = null;
        [SerializeField] TMP_Text _multiplierLabel = null;
        [SerializeField] TMP_Text _bonusLabel = null;
        [SerializeField] UIScalePunch _punch = null;
        [SerializeField] ComboFlameEffect _flameEffect = null;

        CancellationTokenSource _hideTokenSource = null;
        int _lastComboCount = INITIAL_COMBO_COUNT;

        void OnDestroy()
        {
            CancelScheduledHide();
        }

        public void SetCombo(int comboCount, float multiplier, int pendingBonus)
        {
            CancelScheduledHide();
            _root.SetActive(true);
            BindLabels(comboCount, multiplier);

            if (comboCount > _lastComboCount)
                _punch.Play();

            _lastComboCount = comboCount;
            _flameEffect.SetCombo(comboCount);
            BindBonusLabel(pendingBonus);
        }

        public void ShowBonus(int comboCount, float multiplier, int bonusScore)
        {
            CancelScheduledHide();
            _root.SetActive(true);
            BindLabels(comboCount, multiplier);
            BindBonusLabel(bonusScore);
            _hideTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            HideAfterDelayAsync(_hideTokenSource.Token).Forget();
        }

        public void Hide()
        {
            CancelScheduledHide();
            _lastComboCount = INITIAL_COMBO_COUNT;
            _bonusLabel.gameObject.SetActive(false);
            _flameEffect.Hide();
            _punch.Stop();
            _root.SetActive(false);
        }

        void BindLabels(int comboCount, float multiplier)
        {
            _comboLabel.text = comboCount.ToString(CultureInfo.InvariantCulture);
            _multiplierLabel.text = string.Format(MULTIPLIER_FORMAT, multiplier);
        }

        void BindBonusLabel(int bonusScore)
        {
            if (bonusScore <= 0)
            {
                _bonusLabel.gameObject.SetActive(false);
                return;
            }

            _bonusLabel.text = string.Format(BONUS_FORMAT, bonusScore);
            _bonusLabel.gameObject.SetActive(true);
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
            Hide();
        }
    }
}
