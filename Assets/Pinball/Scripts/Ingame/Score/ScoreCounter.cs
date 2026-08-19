using System;
using Pinball.Ingame.Map;
using Pinball.Ingame.Stat;
using UnityEngine;

namespace Pinball.Ingame
{
    public class ScoreCounter
    {
        const float DEFAULT_SCORE_MULTIPLIER = 1f;

        ComboTracker _comboTracker = null;
        Action<int, Vector2> _scorePopupPlayer = null;
        Func<MapInteractable.EGimmickType, float> _stageGimmickScoreMultiplierGetter = null;
        int _current = 0;
        float _hitGradeScoreMultiplier = DEFAULT_SCORE_MULTIPLIER;

        public int Current => _current;
        public int ComboCount => null == _comboTracker ? 0 : _comboTracker.ComboCount;
        public event Action<int> OnChanged;
        public event Action<int, float, int> OnComboChanged;
        public event Action<int, float, int> OnComboEnded;

        public ScoreCounter(int initialScore, ComboConfigData comboConfig, Action<int, Vector2> scorePopupPlayer, Func<MapInteractable.EGimmickType, float> stageGimmickScoreMultiplierGetter)
        {
            _current = initialScore;
            _scorePopupPlayer = scorePopupPlayer;
            _stageGimmickScoreMultiplierGetter = stageGimmickScoreMultiplierGetter;
            _hitGradeScoreMultiplier = DEFAULT_SCORE_MULTIPLIER;
            _comboTracker = new ComboTracker(comboConfig, StatProvider.GetComboMultiplierBonus, StatProvider.GetComboTimeoutBonus);
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
            float bonusRatio = StatProvider.GetGimmickScoreRatio(gimmickType)
                + StatProvider.GetEffectSum(ShopItemData.EEffectType.FragileBallScoreRatio)
                + StatProvider.GetLowBallScoreRatio();
            int finalScore = Mathf.RoundToInt(score * (1f + bonusRatio) * _hitGradeScoreMultiplier * GetStageGimmickScoreMultiplier(gimmickType));

            if (scoreSource == MapInteractable.EScoreSource.BallHit)
                _comboTracker.AddBounce(finalScore);

            Add(finalScore);
            _scorePopupPlayer?.Invoke(finalScore, worldPoint);
        }

        public void HandleBallStruck(Flipper.EHitGrade grade, Vector2 worldPoint)
        {
            RefreshHitGradeMultiplier(grade);
            AddParryScore(grade, worldPoint);
        }

        float GetStageGimmickScoreMultiplier(MapInteractable.EGimmickType gimmickType)
        {
            if (null == _stageGimmickScoreMultiplierGetter)
                return DEFAULT_SCORE_MULTIPLIER;

            return _stageGimmickScoreMultiplierGetter(gimmickType);
        }

        void RefreshHitGradeMultiplier(Flipper.EHitGrade grade)
        {
            if (!StatProvider.HasEffect(ShopItemData.EEffectType.HitGradeScoreRatio))
                return;

            if (grade == Flipper.EHitGrade.Perfect)
            {
                _hitGradeScoreMultiplier = DEFAULT_SCORE_MULTIPLIER + StatProvider.GetEffectSum(ShopItemData.EEffectType.HitGradeScoreRatio);
                return;
            }

            if (grade == Flipper.EHitGrade.Normal)
            {
                _hitGradeScoreMultiplier = DEFAULT_SCORE_MULTIPLIER - StatProvider.GetEffectValue(ShopItemData.EEffectType.HitGradeScoreRatio, ShopItemData.SUB_VALUE_INDEX);
                return;
            }

            _hitGradeScoreMultiplier = DEFAULT_SCORE_MULTIPLIER;
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
            _hitGradeScoreMultiplier = DEFAULT_SCORE_MULTIPLIER;

            if (null == _comboTracker)
                return;

            _comboTracker.EndCombo();
        }

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
