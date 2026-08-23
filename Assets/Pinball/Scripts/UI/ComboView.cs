using System;
using System.Globalization;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.UI
{
    public class ComboView : MonoBehaviour
    {
        const string MULTIPLIER_FORMAT = "x{0:0.##}";
        const string BONUS_FORMAT = "+{0:N0}";
        const float BONUS_DISPLAY_SECONDS = 1.2f;
        const int INITIAL_COMBO_COUNT = 0;
        const float GAUGE_START_ANGLE = 180f;
        const float GAUGE_SWEEP_ANGLE = 180f;
        const float FLAME_ROTATION_OFFSET = -90f;

        [SerializeField] GameObject _root = null;
        [SerializeField] TMP_Text _comboLabel = null;
        [SerializeField] TMP_Text _multiplierLabel = null;
        [SerializeField] TMP_Text _bonusLabel = null;
        [SerializeField] Image _gaugeFill = null;
        [SerializeField] RectTransform _flamePivot = null;
        [SerializeField] float _flameRadius = 162f;
        [SerializeField] UIScalePunch _punch = null;
        [SerializeField] ComboFlameEffect _flameEffect = null;

        CancellationTokenSource _hideTokenSource = null;
        int _lastComboCount = INITIAL_COMBO_COUNT;

        void OnDestroy()
        {
            CancelScheduledHide();
        }

        public void SetCombo(int comboCount, float multiplier, int pendingBonus, float stepProgress)
        {
            CancelScheduledHide();
            _root.SetActive(true);
            BindLabels(comboCount, multiplier);
            BindGauge(stepProgress);

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
            _multiplierLabel.text = string.Format(CultureInfo.InvariantCulture, MULTIPLIER_FORMAT, multiplier);
        }

        void BindGauge(float stepProgress)
        {
            float progress = Mathf.Clamp01(stepProgress);
            _gaugeFill.fillAmount = progress;

            float angle = GAUGE_START_ANGLE - GAUGE_SWEEP_ANGLE * progress;
            float radian = angle * Mathf.Deg2Rad;
            _flamePivot.anchoredPosition = new Vector2(Mathf.Cos(radian), Mathf.Sin(radian)) * _flameRadius;
            _flamePivot.localRotation = Quaternion.Euler(0f, 0f, angle + FLAME_ROTATION_OFFSET);
        }

        void BindBonusLabel(int bonusScore)
        {
            if (bonusScore <= 0)
            {
                _bonusLabel.gameObject.SetActive(false);
                return;
            }

            _bonusLabel.text = string.Format(CultureInfo.InvariantCulture, BONUS_FORMAT, bonusScore);
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
