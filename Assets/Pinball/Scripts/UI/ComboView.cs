using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameCore.UI;

namespace Pinball.UI
{
    public class ComboView : MonoBehaviour
    {
        const string MULTIPLIER_FORMAT = "x{0:0.##}";
        const int INITIAL_COMBO_COUNT = 0;
        const float GAUGE_START_ANGLE = 180f;
        const float GAUGE_SWEEP_ANGLE = 180f;
        const float FLAME_ROTATION_OFFSET = -90f;

        [SerializeField] GameObject _root = null;
        [SerializeField] TMP_Text _comboLabel = null;
        [SerializeField] TMP_Text _multiplierLabel = null;
        [SerializeField] Image _gaugeFill = null;
        [SerializeField] RectTransform _flamePivot = null;
        [SerializeField] float _flameRadius = 162f;
        [SerializeField] UIScalePunch _punch = null;
        [SerializeField] ComboFlameEffect _flameEffect = null;

        int _lastComboCount = INITIAL_COMBO_COUNT;

        public void SetCombo(int comboCount, float multiplier, float stepProgress)
        {
            _root.SetActive(true);
            BindLabels(comboCount, multiplier);
            BindGauge(stepProgress);

            if (comboCount > _lastComboCount)
                _punch.Play();

            _lastComboCount = comboCount;
            _flameEffect.SetCombo(comboCount);
        }

        public void Hide()
        {
            _lastComboCount = INITIAL_COMBO_COUNT;
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
    }
}
