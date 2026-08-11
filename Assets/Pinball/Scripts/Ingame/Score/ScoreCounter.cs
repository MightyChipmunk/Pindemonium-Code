using System;
using Pinball.Ingame.Map;
using Pinball.Ingame.Stat;
using UnityEngine;

namespace Pinball.Ingame
{
    public class ScoreCounter
    {
        ComboTracker _comboTracker = null;
        StatProvider _stats = null;
        Action<int, Vector2> _scorePopupPlayer = null;
        int _current = 0;

        public int Current => _current;
        public event Action<int> OnChanged;
        public event Action<int, float, int> OnComboChanged;
        public event Action<int, int> OnComboEnded;

        public void Init(int initialScore, ComboConfigData comboConfig, StatProvider stats, Action<int, Vector2> scorePopupPlayer)
        {
            _current = initialScore;
            _stats = stats;
            _scorePopupPlayer = scorePopupPlayer;
            _comboTracker = new ComboTracker();
            _comboTracker.Init(comboConfig, stats.GetComboMultiplierBonus, stats.GetComboTimeoutBonus);
            _comboTracker.OnComboChanged += HandleComboChanged;
            _comboTracker.OnComboEnded += HandleComboEnded;
        }

        public void Release()
        {
            if (null != _comboTracker)
            {
                _comboTracker.Release();
                _comboTracker = null;
            }

            _stats = null;
            _scorePopupPlayer = null;
            OnChanged = null;
            OnComboChanged = null;
            OnComboEnded = null;
        }

        public void Tick(float deltaTime)
        {
            if (null == _comboTracker)
                return;

            _comboTracker.Tick(deltaTime);
        }

        public void HandleMapScoreGained(MapInteractable.EGimmickType gimmickType, int score, Vector2 worldPoint, MapInteractable.EScoreSource scoreSource)
        {
            int finalScore = Mathf.RoundToInt(score * (1f + _stats.GetGimmickScoreRatio(gimmickType) + _stats.GetEffectSum(ShopItemData.EEffectType.FragileBallScoreRatio)));

            if (scoreSource == MapInteractable.EScoreSource.BallHit)
                _comboTracker.AddBounce(finalScore);

            Add(finalScore);
            _scorePopupPlayer?.Invoke(finalScore, worldPoint);
        }

        public void HandlePerfectParry(Flipper.EHitGrade grade, Vector2 worldPoint)
        {
            if (grade != Flipper.EHitGrade.Perfect)
                return;

            int parryScore = Mathf.RoundToInt(_stats.GetEffectSum(ShopItemData.EEffectType.PerfectParryScoreAdd));
            if (parryScore <= 0)
                return;

            Add(parryScore);
            _scorePopupPlayer?.Invoke(parryScore, worldPoint);
        }

        public void EndCombo()
        {
            if (null == _comboTracker)
                return;

            _comboTracker.EndCombo();
        }

        void HandleComboChanged(int comboCount, float multiplier, int pendingBonus)
        {
            OnComboChanged?.Invoke(comboCount, multiplier, pendingBonus);
        }

        void HandleComboEnded(int comboCount, int bonusScore)
        {
            Add(bonusScore);
            OnComboEnded?.Invoke(comboCount, bonusScore);
        }

        void Add(int amount)
        {
            if (amount <= 0)
                return;

            _current += amount;
            OnChanged?.Invoke(_current);
        }
    }
}
