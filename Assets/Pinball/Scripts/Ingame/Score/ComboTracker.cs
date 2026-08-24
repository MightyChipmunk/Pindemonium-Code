using System;
using UnityEngine;

namespace Pinball.Ingame.Score
{
    public class ComboTracker
    {
        const float DEFAULT_GAIN_RATE = 1f;

        ComboConfigData _config = null;
        Func<float> _multiplierBonusGetter = null;
        Func<float> _timeoutBonusGetter = null;
        Func<float> _gainRateGetter = null;
        int _comboCount = 0;
        int _accumulatedScore = 0;
        float _gainProgress = 0f;
        float _timeoutRemaining = 0f;

        public int ComboCount => _comboCount;

        public event Action<int, float, int> OnComboChanged;
        public event Action<int, float, int> OnComboEnded;

        public ComboTracker(ComboConfigData config, Func<float> multiplierBonusGetter, Func<float> timeoutBonusGetter, Func<float> gainRateGetter)
        {
            _config = config;
            _multiplierBonusGetter = multiplierBonusGetter;
            _timeoutBonusGetter = timeoutBonusGetter;
            _gainRateGetter = gainRateGetter;
            ResetCombo();
        }

        public void AddBounce(int score)
        {
            if (null == _config)
                return;

            _gainProgress += GetGainRate();
            _comboCount = Mathf.FloorToInt(_gainProgress);
            _accumulatedScore += Mathf.Max(0, score);
            _timeoutRemaining = _config.TimeoutSeconds + GetTimeoutBonus();
            OnComboChanged?.Invoke(_comboCount, GetCurrentMultiplier(), CalculateBonusScore());
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

            int comboCount = _comboCount;
            float multiplier = GetCurrentMultiplier();
            int bonusScore = CalculateBonusScore();
            ResetCombo();
            OnComboEnded?.Invoke(comboCount, multiplier, bonusScore);
        }

        int CalculateBonusScore()
        {
            float multiplier = GetCurrentMultiplier();
            if (multiplier <= 1f)
                return 0;

            return Mathf.RoundToInt(_accumulatedScore * (multiplier - 1f));
        }

        float GetCurrentMultiplier()
        {
            if (null == _config)
                return 1f;

            if (_config.ComboCountPerStep <= 0)
                return 1f;

            int stepCount = _comboCount / _config.ComboCountPerStep;
            if (stepCount <= 0)
                return 1f;

            return 1f + _config.StepMultiplierAdd * stepCount + GetMultiplierBonus();
        }

        float GetMultiplierBonus()
        {
            if (null == _multiplierBonusGetter)
                return 0f;

            return _multiplierBonusGetter();
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
            _accumulatedScore = 0;
            _gainProgress = 0f;
            _timeoutRemaining = 0f;
        }
    }
}
