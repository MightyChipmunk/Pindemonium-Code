using System;
using UnityEngine;

namespace Pinball.Ingame.Score
{
    public class ComboTracker
    {
        const float DEFAULT_GAIN_RATE = 1f;
        const float DEFAULT_BONUS_SCALE = 1f;
        const float BASE_MULTIPLIER = 1f;

        ComboConfigData _config = null;
        Func<float> _bonusScaleGetter = null;
        Func<float> _timeoutBonusGetter = null;
        Func<float> _gainRateGetter = null;
        int _comboCount = 0;
        float _gainProgress = 0f;
        float _timeoutRemaining = 0f;

        public int ComboCount => _comboCount;

        public event Action<int, float> OnComboChanged;
        public event Action OnComboEnded;

        public ComboTracker(ComboConfigData config, Func<float> bonusScaleGetter, Func<float> timeoutBonusGetter, Func<float> gainRateGetter)
        {
            _config = config;
            _bonusScaleGetter = bonusScaleGetter;
            _timeoutBonusGetter = timeoutBonusGetter;
            _gainRateGetter = gainRateGetter;
            ResetCombo();
        }

        public void AddBounce()
        {
            if (null == _config)
                return;

            _gainProgress += GetGainRate();
            _comboCount = Mathf.FloorToInt(_gainProgress);
            _timeoutRemaining = _config.TimeoutSeconds + GetTimeoutBonus();
            OnComboChanged?.Invoke(_comboCount, GetCurrentMultiplier());
        }

        public void Tick(float deltaTime)
        {
            if (_comboCount <= 0)
                return;

            _timeoutRemaining -= deltaTime;
            if (_timeoutRemaining > 0f)
                return;

            EndCombo();
        }

        public void EndCombo()
        {
            if (_comboCount <= 0)
                return;

            ResetCombo();
            OnComboEnded?.Invoke();
        }

        public float GetCurrentMultiplier()
        {
            if (null == _config)
                return BASE_MULTIPLIER;

            if (_config.ComboCountPerStep <= 0)
                return BASE_MULTIPLIER;

            int stepCount = _comboCount / _config.ComboCountPerStep;
            if (stepCount <= 0)
                return BASE_MULTIPLIER;

            return BASE_MULTIPLIER + _config.StepMultiplierAdd * stepCount * GetBonusScale();
        }

        float GetBonusScale()
        {
            if (null == _bonusScaleGetter)
                return DEFAULT_BONUS_SCALE;

            return _bonusScaleGetter();
        }

        float GetTimeoutBonus()
        {
            if (null == _timeoutBonusGetter)
                return 0f;

            return _timeoutBonusGetter();
        }

        float GetGainRate()
        {
            if (null == _gainRateGetter)
                return DEFAULT_GAIN_RATE;

            return Mathf.Max(DEFAULT_GAIN_RATE, _gainRateGetter());
        }

        void ResetCombo()
        {
            _comboCount = 0;
            _gainProgress = 0f;
            _timeoutRemaining = 0f;
        }
    }
}
