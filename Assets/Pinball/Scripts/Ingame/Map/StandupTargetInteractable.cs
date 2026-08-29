using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Pinball.Ingame.Shop;
using Pinball.Ingame.Stat;
using UnityEngine;

namespace Pinball.Ingame.Map
{
    public class StandupTargetInteractable : CollisionInteractable, IDamageable
    {
        const float HIT_PUNCH_DISTANCE = 0.08f;
        const float HIT_PUNCH_DURATION = 0.18f;
        const int HIT_PUNCH_VIBRATO = 8;
        const float COLLISION_COOLDOWN = 0.5f;
        const float COOLDOWN_ALPHA = 0.35f;
        const float DEFAULT_ALPHA = 1f;
        const float BASE_PIERCE_MULTIPLIER = 1f;
        const float PIERCE_HOLE_MIN_SCALE_RATIO = 0.25f;
        const float PIERCE_HOLE_MAX_SCALE_RATIO = 1f;
        const float PIERCE_HOLE_FADE_DURATION = 0.45f;
        const float NO_PIERCE_ALPHA = 0f;

        [SerializeField] Collider2D _collider = null;
        [SerializeField] SpriteRenderer _visualRenderer = null;
        [SerializeField] Transform _visual = null;
        [SerializeField] SpriteRenderer _pierceOverlay = null;
        [SerializeField] SpriteRenderer _pierceHole = null;
        [SerializeField] int _scoreReward = 0;

        CancellationTokenSource _cancellationTokenSource = null;
        Tween _hitTween = null;
        Tween _pierceTween = null;
        Vector3 _pierceHoleBaseScale = Vector3.one;
        bool _hasCapturedPierceBase = false;

        public override EGimmickType GimmickType => EGimmickType.StandupTarget;

        protected override void OnInit()
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            ApplyAlpha(DEFAULT_ALPHA);
            CapturePierceBase();
            RefreshPassiveOverlay();
            _collider.enabled = true;
        }

        void CapturePierceBase()
        {
            if (_hasCapturedPierceBase || null == _pierceHole)
                return;

            _hasCapturedPierceBase = true;
            _pierceHoleBaseScale = _pierceHole.transform.localScale;
        }

        void RefreshPassiveOverlay()
        {
            KillPierceTween();
            bool hasPierce = StatProvider.HasEffect(ShopItemData.EEffectType.StandupDamageScore);

            if (null != _pierceOverlay)
                _pierceOverlay.gameObject.SetActive(hasPierce);

            if (null == _pierceHole)
                return;

            _pierceHole.gameObject.SetActive(hasPierce);
            ApplyPierceHoleAlpha(NO_PIERCE_ALPHA);
        }

        void ApplyPierceHoleAlpha(float alpha)
        {
            Color color = _pierceHole.color;
            color.a = alpha;
            _pierceHole.color = color;
        }

        float GetPierceRatio(float damage)
        {
            float maxMultiplier = StatProvider.GetEffectValue(ShopItemData.EEffectType.StandupDamageScore, ShopItemData.MAIN_VALUE_INDEX);
            if (maxMultiplier <= BASE_PIERCE_MULTIPLIER)
                return 0f;

            float multiplier = StatProvider.GetStandupDamageScoreMultiplier(damage);
            return Mathf.Clamp01((multiplier - BASE_PIERCE_MULTIPLIER) / (maxMultiplier - BASE_PIERCE_MULTIPLIER));
        }

        void PlayPierceHole(float damage)
        {
            if (null == _pierceHole || !_pierceHole.gameObject.activeSelf)
                return;

            KillPierceTween();
            float pierceRatio = GetPierceRatio(damage);
            _pierceHole.transform.localScale = _pierceHoleBaseScale * Mathf.Lerp(PIERCE_HOLE_MIN_SCALE_RATIO, PIERCE_HOLE_MAX_SCALE_RATIO, pierceRatio);
            ApplyPierceHoleAlpha(DEFAULT_ALPHA);
            _pierceTween = _pierceHole.DOFade(NO_PIERCE_ALPHA, PIERCE_HOLE_FADE_DURATION);
        }

        void KillPierceTween()
        {
            if (null == _pierceTween)
                return;

            _pierceTween.Kill(false);
            _pierceTween = null;
        }

        protected override void OnRelease()
        {
            if (null != _cancellationTokenSource)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }

            if (null != _hitTween)
            {
                _hitTween.Kill();
                _hitTween = null;
            }

            KillPierceTween();
        }

        public void TakeHit(float damage, Vector2 worldPoint)
        {
            if (IsDeactivated || !_collider.enabled)
                return;

            GainHitScore(damage, worldPoint);
            PlayHitMotion();

            if (null != _cancellationTokenSource)
                CooldownAsync(_cancellationTokenSource.Token).Forget();
        }

        protected override Vector2 GetBounceDirection(Collision2D collision)
        {
            return transform.up;
        }

        protected override void GainHitScore(float damage, Vector2 worldPoint)
        {
            GainScore(Mathf.RoundToInt(_scoreReward * StatProvider.GetStandupDamageScoreMultiplier(damage)), worldPoint);
            PlayPierceHole(damage);
        }

        protected override void OnBallHit(Collision2D collision, float damage)
        {
            PlayHitMotion();

            if (null != _cancellationTokenSource)
                CooldownAsync(_cancellationTokenSource.Token).Forget();
        }

        async UniTaskVoid CooldownAsync(CancellationToken cancellationToken)
        {
            _collider.enabled = false;
            ApplyAlpha(COOLDOWN_ALPHA);

            await UniTask.Delay(TimeSpan.FromSeconds(COLLISION_COOLDOWN), cancellationToken: cancellationToken);

            ApplyAlpha(DEFAULT_ALPHA);
            _collider.enabled = true;
        }

        void ApplyAlpha(float alpha)
        {
            if (null == _visualRenderer)
                return;

            Color color = _visualRenderer.color;
            color.a = alpha;
            _visualRenderer.color = color;
        }

        void PlayHitMotion()
        {
            if (null == _visual)
                return;

            if (null != _hitTween)
                _hitTween.Kill(true);

            _hitTween = _visual.DOPunchPosition(-transform.up * HIT_PUNCH_DISTANCE, HIT_PUNCH_DURATION, HIT_PUNCH_VIBRATO);
        }

#if UNITY_EDITOR
        protected override void OnBindSerializedField()
        {
            base.OnBindSerializedField();
            _collider = GetComponent<Collider2D>();
            _visualRenderer = GetComponentInChildren<SpriteRenderer>(true);

            if (null != _visualRenderer)
                _visual = _visualRenderer.transform;
        }
#endif // UNITY_EDITOR
    }
}
