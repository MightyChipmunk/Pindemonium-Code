using System;
using DG.Tweening;
using Pinball.Ingame.Shop;
using Pinball.Ingame.Stat;
using UnityEngine;

namespace Pinball.Ingame.Map
{
    public class SlingshotInteractable : CollisionInteractable, IDamageable
    {
        public enum ESide
        {
            Left = 0,
            Right = 1,
        }

        const float KICK_PUNCH_DISTANCE = 0.12f;
        const float KICK_PUNCH_DURATION = 0.15f;
        const int KICK_PUNCH_VIBRATO = 6;
        const float DEFAULT_HIT_SCORE_MULTIPLIER = 1f;
        const int FIRST_GROWTH_TIER = 0;
        const float GROWTH_PUNCH_RATIO = 0.25f;
        const float GROWTH_PUNCH_DURATION = 0.3f;
        const int GROWTH_PUNCH_VIBRATO = 8;
        const float RALLY_ACTIVE_ALPHA = 1f;
        const float RALLY_IDLE_ALPHA = 0.18f;
        const float RALLY_FADE_DURATION = 0.15f;

        [SerializeField] Transform _visual = null;
        [SerializeField] SpriteRenderer _visualRenderer = null;
        [SerializeField] Transform _directionTransform = null;
        [SerializeField] ESide _side = ESide.Left;
        [SerializeField] SpriteRenderer _growthOutline = null;
        [SerializeField] Color[] _growthTierColors = null;
        [SerializeField] int _growthHitsPerTier = 12;
        [SerializeField] SpriteRenderer _rallyIcon = null;

        Tween _kickTween = null;
        Tween _growthPunchTween = null;
        Tween _rallyTween = null;
        Func<ESide, float> _hitScoreMultiplierConsumer = null;
        Vector3 _growthOutlineBaseScale = Vector3.one;
        int _growthTier = FIRST_GROWTH_TIER;
        bool _hasCapturedGrowthBase = false;
        bool _hasAppliedGrowthTier = false;

        public override EGimmickType GimmickType => EGimmickType.Slingshot;

        public event Action<ESide, Collider2D> OnBallBounced;

        public ESide GetSide()
        {
            return _side;
        }

        public void SetHitScoreMultiplierConsumer(Func<ESide, float> consumer)
        {
            _hitScoreMultiplierConsumer = consumer;
        }

        public void TakeHit(float damage, Vector2 worldPoint)
        {
            if (IsDeactivated)
                return;

            GainScore(Mathf.RoundToInt(damage * GetScoreMultiplier()), worldPoint, EScoreSource.PassiveEffect);
        }

        protected override void OnInit()
        {
            _growthTier = FIRST_GROWTH_TIER;
            _hasAppliedGrowthTier = false;

            if (null != _growthOutline && !_hasCapturedGrowthBase)
            {
                _hasCapturedGrowthBase = true;
                _growthOutlineBaseScale = _growthOutline.transform.localScale;
            }

            SyncGrowthOutlineShape();
            RefreshPassiveOverlay();
        }

        protected override void OnRelease()
        {
            OnBallBounced = null;
            _hitScoreMultiplierConsumer = null;
            KillKickTween();
            KillGrowthPunchTween();
            KillRallyTween();
        }

        public void SetGrowthHitCount(int hitCount)
        {
            if (null == _growthOutline || !_growthOutline.gameObject.activeSelf)
                return;

            if (null == _growthTierColors || _growthTierColors.Length == 0)
                return;

            int tier = _growthHitsPerTier <= 0
                ? FIRST_GROWTH_TIER
                : Mathf.Clamp(hitCount / _growthHitsPerTier, FIRST_GROWTH_TIER, _growthTierColors.Length - 1);

            if (_hasAppliedGrowthTier && tier == _growthTier)
                return;

            bool isPromoted = _hasAppliedGrowthTier && tier > _growthTier;
            _hasAppliedGrowthTier = true;
            _growthTier = tier;
            _growthOutline.color = _growthTierColors[tier];

            if (isPromoted)
                PlayGrowthPunch();
        }

        public void SetRallyActive(bool isActive)
        {
            if (null == _rallyIcon || !_rallyIcon.gameObject.activeSelf)
                return;

            KillRallyTween();
            _rallyTween = _rallyIcon.DOFade(isActive ? RALLY_ACTIVE_ALPHA : RALLY_IDLE_ALPHA, RALLY_FADE_DURATION);
        }

        void SyncGrowthOutlineShape()
        {
            if (null == _growthOutline || null == _visualRenderer)
                return;

            _growthOutline.sprite = _visualRenderer.sprite;
            _growthOutline.flipX = _visualRenderer.flipX;
            _growthOutline.flipY = _visualRenderer.flipY;
        }

        void RefreshPassiveOverlay()
        {
            KillGrowthPunchTween();
            KillRallyTween();

            if (null != _growthOutline)
            {
                _growthOutline.gameObject.SetActive(StatProvider.HasEffect(ShopItemData.EEffectType.SlingshotScoreGrowth));
                _growthOutline.transform.localScale = _growthOutlineBaseScale;
            }

            if (null == _rallyIcon)
                return;

            _rallyIcon.gameObject.SetActive(StatProvider.HasEffect(ShopItemData.EEffectType.SlingshotAlternateScore));

            Color color = _rallyIcon.color;
            color.a = RALLY_ACTIVE_ALPHA;
            _rallyIcon.color = color;
        }

        void PlayGrowthPunch()
        {
            KillGrowthPunchTween();
            _growthOutline.transform.localScale = _growthOutlineBaseScale;
            _growthPunchTween = _growthOutline.transform.DOPunchScale(_growthOutlineBaseScale * GROWTH_PUNCH_RATIO, GROWTH_PUNCH_DURATION, GROWTH_PUNCH_VIBRATO);
        }

        protected override Vector2 GetBounceDirection(Collision2D collision)
        {
            if (null == _directionTransform)
            {
                Debug.LogError($"{nameof(_directionTransform)} is unsigned");
                return transform.up;
            }

            return _directionTransform.up;
        }

        protected override void GainHitScore(float damage, Vector2 worldPoint)
        {
            float multiplier = null == _hitScoreMultiplierConsumer ? DEFAULT_HIT_SCORE_MULTIPLIER : _hitScoreMultiplierConsumer(_side);
            base.GainHitScore(damage * multiplier, worldPoint);
        }

        protected override void OnBallHit(Collision2D collision, float damage)
        {
            OnBallBounced?.Invoke(_side, collision.collider);

            if (null == _visual)
                return;

            KillKickTween();
            _kickTween = _visual.DOPunchPosition(transform.up * KICK_PUNCH_DISTANCE, KICK_PUNCH_DURATION, KICK_PUNCH_VIBRATO);
        }

        void KillKickTween()
        {
            if (null == _kickTween)
                return;

            _kickTween.Kill(true);
            _kickTween = null;
        }

        void KillGrowthPunchTween()
        {
            if (null == _growthPunchTween)
                return;

            _growthPunchTween.Kill(true);
            _growthPunchTween = null;
        }

        void KillRallyTween()
        {
            if (null == _rallyTween)
                return;

            _rallyTween.Kill(false);
            _rallyTween = null;
        }
    }
}
