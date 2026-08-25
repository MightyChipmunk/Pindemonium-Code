using System;
using Pinball.Ingame.Map;
using Pinball.Ingame.Shop;
using Pinball.Ingame.Stat;
using UnityEngine;

namespace Pinball.Ingame.Score
{
    public class ScoreCounter
    {
        const float DEFAULT_SCORE_MULTIPLIER = 1f;

        ComboTracker _comboTracker = null;
        SlingshotScoreGrowth _slingshotGrowth = null;
        Action<int, Vector2> _scorePopupPlayer = null;
        Func<MapInteractable.EGimmickType, float> _stageGimmickScoreMultiplierGetter = null;
        Action<MapInteractable.EGimmickType, int> _gimmickScoreReporter = null;
        int _current = 0;

        public int Current => _current;
        public int ComboCount => null == _comboTracker ? 0 : _comboTracker.ComboCount;
        public event Action<int> OnChanged;
        public event Action<int, float, int> OnComboChanged;
        public event Action<int, float, int> OnComboEnded;

        public ScoreCounter(int initialScore, ComboConfigData comboConfig, Action<int, Vector2> scorePopupPlayer, Func<MapInteractable.EGimmickType, float> stageGimmickScoreMultiplierGetter, Action<MapInteractable.EGimmickType, int> gimmickScoreReporter, SlingshotScoreGrowth slingshotGrowth)
        {
            _current = initialScore;
            _slingshotGrowth = slingshotGrowth;
            _scorePopupPlayer = scorePopupPlayer;
            _stageGimmickScoreMultiplierGetter = stageGimmickScoreMultiplierGetter;
            _gimmickScoreReporter = gimmickScoreReporter;
            _comboTracker = new ComboTracker(comboConfig, StatProvider.GetComboMultiplierBonus, StatProvider.GetComboTimeoutBonus, StatProvider.GetComboGainRate);
            _comboTracker.OnComboChanged += HandleComboChanged;
            _comboTracker.OnComboEnded += HandleComboEnded;
        }

        public void Tick(float deltaTime)
        {
            if (null == _comboTracker)
                return;

            _comboTracker.Tick(deltaTime);
        }

        public void HandleMapScoreGained(MapInteractable.EGimmickType gimmickType, int score, Vector2 worldPoint, MapInteractable.EScoreSource scoreSource)
        {
            CountSlingshotHit(gimmickType, scoreSource);
            float bonusRatio = GetSourceScoreRatio(gimmickType, scoreSource)
                + GetSlingshotGrowthRatio(gimmickType)
                + StatProvider.GetEffectSum(ShopItemData.EEffectType.FragileBallScoreRatio)
                + StatProvider.GetLowBallScoreRatio();
            int finalScore = Mathf.RoundToInt(score * (1f + bonusRatio) * GetStageGimmickScoreMultiplier(gimmickType));

            if (scoreSource != MapInteractable.EScoreSource.PassiveEffect)
                _comboTracker.AddBounce(finalScore);

            Add(finalScore);
            _gimmickScoreReporter?.Invoke(gimmickType, finalScore);
            _scorePopupPlayer?.Invoke(finalScore, worldPoint);
        }

        public void HandleBallStruck(Flipper.EHitGrade grade, Vector2 worldPoint)
        {
            AddParryScore(grade, worldPoint);
        }

        void CountSlingshotHit(MapInteractable.EGimmickType gimmickType, MapInteractable.EScoreSource scoreSource)
        {
            if (null == _slingshotGrowth)
                return;

            if (gimmickType != MapInteractable.EGimmickType.Slingshot)
                return;

            if (scoreSource != MapInteractable.EScoreSource.BallHit)
                return;

            _slingshotGrowth.CountHit();
        }

        float GetSlingshotGrowthRatio(MapInteractable.EGimmickType gimmickType)
        {
            if (null == _slingshotGrowth)
                return 0f;

            if (gimmickType != MapInteractable.EGimmickType.Slingshot)
                return 0f;

            return _slingshotGrowth.GetScoreRatio();
        }

        static float GetSourceScoreRatio(MapInteractable.EGimmickType gimmickType, MapInteractable.EScoreSource scoreSource)
        {
            if (scoreSource == MapInteractable.EScoreSource.GimmickBreak)
                return StatProvider.GetEffectSum(ShopItemData.EEffectType.GimmickBreakScoreRatio);

            return StatProvider.GetGimmickScoreRatio(gimmickType);
        }

        float GetStageGimmickScoreMultiplier(MapInteractable.EGimmickType gimmickType)
        {
            if (null == _stageGimmickScoreMultiplierGetter)
                return DEFAULT_SCORE_MULTIPLIER;

            return _stageGimmickScoreMultiplierGetter(gimmickType);
        }

        void AddParryScore(Flipper.EHitGrade grade, Vector2 worldPoint)
        {
            if (grade != Flipper.EHitGrade.Perfect)
                return;

            int parryScore = Mathf.RoundToInt(StatProvider.GetEffectSum(ShopItemData.EEffectType.PerfectParryScoreAdd));
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void AddCheatScore(int amount)
        {
            Add(amount);
        }
#endif

        void HandleComboChanged(int comboCount, float multiplier, int pendingBonus)
        {
            OnComboChanged?.Invoke(comboCount, multiplier, pendingBonus);
        }

        void HandleComboEnded(int comboCount, float multiplier, int bonusScore)
        {
            Add(bonusScore);
            OnComboEnded?.Invoke(comboCount, multiplier, bonusScore);
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
