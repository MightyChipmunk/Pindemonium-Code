using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Pinball.Ingame.Map
{
    public class BumperInteractable : CollisionInteractable
    {
        const float HIT_PUNCH_RATIO = 0.7f;
        const float HIT_PUNCH_DURATION = 0.2f;
        const int HIT_PUNCH_VIBRATO = 10;
        const float RESPAWN_PUNCH_RATIO = 1.2f;
        const float RESPAWN_PUNCH_DURATION = 0.4f;
        const int RESPAWN_PUNCH_VIBRATO = 6;

        [SerializeField] BumperGradeData _gradeData = null;
        [SerializeField] BumperGradeData.EGrade _grade = BumperGradeData.EGrade.Yellow;
        [SerializeField] Collider2D _collider = null;
        [SerializeField] Transform _visual = null;
        [SerializeField] SpriteRenderer _visualRenderer = null;

        CancellationTokenSource _cancellationTokenSource = null;
        Tween _visualTween = null;
        BumperGradeData.GradeSpec _spec = default;
        Vector3 _visualBaseScale = Vector3.one;
        float _currentHealth = 0f;
        bool _isBroken = false;

        protected override void OnInit()
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());

            if (null != _gradeData)
                _spec = _gradeData.GetSpec(_grade);

            if (null != _visual)
                _visualBaseScale = _visual.localScale;

            ApplyGradeVisual();

            _currentHealth = _spec.MaxHealth;
            _isBroken = false;
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

            KillVisualTween(false);
            _isBroken = false;
        }

        protected override Vector2 GetBounceDirection(Collision2D collision)
        {
            return (collision.rigidbody.position - (Vector2)transform.position).normalized;
        }

        protected override float GetScoreMultiplier()
        {
            if (null == _gradeData)
                return base.GetScoreMultiplier();

            return _spec.ScoreMultiplier;
        }

        protected override float GetBounceForce()
        {
            if (null == _gradeData)
                return base.GetBounceForce();

            return _spec.BounceForce;
        }

        protected override void OnBallHit(Collision2D collision, float damage)
        {
            if (_isBroken)
                return;

            PlayFX(_spec.HitFX, collision.GetContact(0).point);
            PlayVisualPunch(HIT_PUNCH_RATIO, HIT_PUNCH_DURATION, HIT_PUNCH_VIBRATO);

            if (null == _gradeData)
                return;

            _currentHealth -= damage;
            if (_currentHealth >= 0f)
                return;

            Break();
        }

        void ApplyGradeVisual()
        {
            if (null == _gradeData)
                return;

            transform.localScale = Vector3.one * _spec.RootScale;

            if (null == _visualRenderer)
                return;

            if (null != _spec.VisualSprite)
                _visualRenderer.sprite = _spec.VisualSprite;
        }

        void Break()
        {
            _isBroken = true;
            _collider.enabled = false;
            GainScore(_spec.BreakScore);
            PlayFX(_spec.BreakFX, transform.position);
            KillVisualTween(false);

            if (null != _visual)
                _visual.gameObject.SetActive(false);

            RespawnAsync(_cancellationTokenSource.Token).Forget();
        }

        async UniTaskVoid RespawnAsync(CancellationToken cancellationToken)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_spec.RespawnDelay), cancellationToken: cancellationToken);

            _currentHealth = _spec.MaxHealth;
            _isBroken = false;
            _collider.enabled = true;

            if (null == _visual)
                return;

            _visual.gameObject.SetActive(true);
            _visual.localScale = _visualBaseScale;
            PlayVisualPunch(RESPAWN_PUNCH_RATIO, RESPAWN_PUNCH_DURATION, RESPAWN_PUNCH_VIBRATO);
        }

        void PlayVisualPunch(float punchRatio, float duration, int vibrato)
        {
            if (null == _visual)
                return;

            KillVisualTween(true);
            _visualTween = _visual.DOPunchScale(_visualBaseScale * punchRatio, duration, vibrato);
        }

        void KillVisualTween(bool isComplete)
        {
            if (null == _visualTween)
                return;

            _visualTween.Kill(isComplete);
            _visualTween = null;
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
