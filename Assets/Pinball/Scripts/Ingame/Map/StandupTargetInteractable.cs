using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Pinball.Ingame.Map
{
    public class StandupTargetInteractable : CollisionInteractable
    {
        const float HIT_PUNCH_DISTANCE = 0.08f;
        const float HIT_PUNCH_DURATION = 0.18f;
        const int HIT_PUNCH_VIBRATO = 8;
        const float COLLISION_COOLDOWN = 0.5f;
        const float COOLDOWN_ALPHA = 0.35f;
        const float DEFAULT_ALPHA = 1f;

        [SerializeField] Collider2D _collider = null;
        [SerializeField] SpriteRenderer _visualRenderer = null;
        [SerializeField] Transform _visual = null;
        [SerializeField] int _scoreReward = 0;

        CancellationTokenSource _cancellationTokenSource = null;
        Tween _hitTween = null;

        public override EGimmickType GimmickType => EGimmickType.StandupTarget;

        protected override void OnInit()
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            ApplyAlpha(DEFAULT_ALPHA);
            _collider.enabled = true;
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
        }

        protected override Vector2 GetBounceDirection(Collision2D collision)
        {
            return transform.up;
        }

        protected override void GainHitScore(float damage, Vector2 worldPoint)
        {
            GainScore(_scoreReward, worldPoint);
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
            _collider = GetComponent<Collider2D>();
            _visualRenderer = GetComponentInChildren<SpriteRenderer>(true);

            if (null != _visualRenderer)
                _visual = _visualRenderer.transform;
        }
#endif // UNITY_EDITOR
    }
}
