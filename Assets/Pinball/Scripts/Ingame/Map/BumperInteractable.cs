using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Pinball.FX;
using UnityEngine;

namespace Pinball.Ingame.Map
{
    public class BumperInteractable : CollisionInteractable
    {
        const float HIT_PUNCH_RATIO = 0.7f;
        const float HIT_PUNCH_DURATION = 0.2f;
        const int HIT_PUNCH_VIBRATO = 9;
        const float RESPAWN_PUNCH_RATIO = 0.6f;
        const float RESPAWN_PUNCH_DURATION = 0.15f;
        const int RESPAWN_PUNCH_VIBRATO = 6;
        const float MIN_BURN_INTERVAL = 0.05f;
        const float BURN_DURATION = 3f;

        static readonly Color BURNING_TINT = new Color(1f, 0.55f, 0.25f, 1f);
        static readonly Color FROZEN_TINT = new Color(0.5f, 0.85f, 1f, 1f);

        [SerializeField] BumperGradeData _gradeData = null;
        [SerializeField] BumperGradeData.EGrade _grade = BumperGradeData.EGrade.Yellow;
        [SerializeField] Collider2D _collider = null;
        [SerializeField] Transform _visual = null;
        [SerializeField] SpriteRenderer _visualRenderer = null;

        CancellationTokenSource _cancellationTokenSource = null;
        CancellationTokenSource _burnTokenSource = null;
        PoolableFX.EFXType _burnFX = PoolableFX.EFXType.NonePooling;
        Tween _visualTween = null;
        BumperGradeData.GradeSpec _spec = default;
        Vector3 _visualBaseScale = Vector3.one;
        Color _visualBaseColor = Color.white;
        float _currentHealth = 0f;
        float _frozenDamageMultiplier = DEFAULT_DAMAGE_MULTIPLIER;
        bool _isBroken = false;
        bool _isFrozen = false;
        bool _isUnbreakable = false;

        public override EGimmickType GimmickType => EGimmickType.Bumper;
        public bool IsBroken => _isBroken;

        public event Action OnBroken;
        public event Action<BumperInteractable, float> OnBallDamaged;

        public void SetUnbreakable(bool isUnbreakable)
        {
            _isUnbreakable = isUnbreakable;
        }

        protected override void OnInit()
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());

            if (null != _gradeData)
                _spec = _gradeData.GetSpec(_grade);

            if (null != _visual)
                _visualBaseScale = _visual.localScale;

            if (null != _visualRenderer)
                _visualBaseColor = _visualRenderer.color;

            ApplyGradeVisual();

            _currentHealth = _spec.MaxHealth;
            _isBroken = false;
            ClearFrozen();
            _collider.enabled = true;
        }

        protected override void OnRelease()
        {
            OnBroken = null;
            OnBallDamaged = null;
            StopBurning();
            ClearFrozen();

            if (null != _cancellationTokenSource)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }

            KillVisualTween(false);
            _isBroken = false;
            _isUnbreakable = false;
        }

        protected override void OnDeactivated()
        {
            StopBurning();
            ClearFrozen();
            KillVisualTween(false);
            base.OnDeactivated();
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

        protected override float GetDamageMultiplier()
        {
            if (!_isFrozen)
                return DEFAULT_DAMAGE_MULTIPLIER;

            return _frozenDamageMultiplier;
        }

        protected override void OnBallHit(Collision2D collision, float damage)
        {
            if (_isBroken)
                return;

            PlayFX(_spec.HitFX, collision.GetContact(0).point);
            PlayVisualPunch(HIT_PUNCH_RATIO, HIT_PUNCH_DURATION, HIT_PUNCH_VIBRATO);

            if (null == _gradeData)
                return;

            ReduceHealth(damage);
            OnBallDamaged?.Invoke(this, damage);
        }

        public void ApplyDamage(float damage, PoolableFX.EFXType extraFX)
        {
            if (_isBroken)
                return;

            if (null == _gradeData)
                return;

            if (damage <= 0f)
                return;

            PlayFX(_spec.HitFX, transform.position);
            PlayFX(extraFX, transform.position);
            PlayVisualPunch(HIT_PUNCH_RATIO, HIT_PUNCH_DURATION, HIT_PUNCH_VIBRATO);
            GainScore(Mathf.RoundToInt(damage * GetScoreMultiplier()), transform.position, EScoreSource.PassiveEffect);
            ReduceHealth(damage);
        }

        public void Ignite(float damagePerTick, float intervalSeconds, PoolableFX.EFXType burnFX)
        {
            if (_isBroken)
                return;

            if (damagePerTick <= 0f)
                return;

            StopBurning();
            _burnFX = burnFX;
            _burnTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            RefreshVisualTint();
            PlayFX(_burnFX, transform.position);
            BurnAsync(damagePerTick, Mathf.Max(MIN_BURN_INTERVAL, intervalSeconds), _burnTokenSource.Token).Forget();
        }

        public void ApplyFrostHit(float shatterDamageMultiplier, PoolableFX.EFXType freezeFX, PoolableFX.EFXType shatterFX)
        {
            if (_isBroken)
                return;

            if (_isFrozen)
            {
                PlayFX(shatterFX, transform.position);
                ClearFrozen();
                return;
            }

            _isFrozen = true;
            _frozenDamageMultiplier = Mathf.Max(DEFAULT_DAMAGE_MULTIPLIER, shatterDamageMultiplier);
            PlayFX(freezeFX, transform.position);
            RefreshVisualTint();
        }

        void ClearFrozen()
        {
            _isFrozen = false;
            _frozenDamageMultiplier = DEFAULT_DAMAGE_MULTIPLIER;
            RefreshVisualTint();
        }

        void ReduceHealth(float damage)
        {
            if (_isUnbreakable)
                return;

            _currentHealth -= damage;
            if (_currentHealth >= 0f)
                return;

            Break();
        }

        async UniTaskVoid BurnAsync(float damagePerTick, float intervalSeconds, CancellationToken cancellationToken)
        {
            int tickCount = Mathf.Max(1, Mathf.RoundToInt(BURN_DURATION / intervalSeconds));
            for (int i = 0; i < tickCount; ++i)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(intervalSeconds), cancellationToken: cancellationToken);

                if (_isBroken)
                    return;

                PlayFX(_burnFX, transform.position);
                GainScore(Mathf.RoundToInt(damagePerTick * GetScoreMultiplier()), transform.position, EScoreSource.PassiveEffect);
                ReduceHealth(damagePerTick);
            }

            StopBurning();
        }

        void StopBurning()
        {
            if (null != _burnTokenSource)
            {
                _burnTokenSource.Cancel();
                _burnTokenSource.Dispose();
                _burnTokenSource = null;
            }

            _burnFX = PoolableFX.EFXType.NonePooling;
            RefreshVisualTint();
        }

        void RefreshVisualTint()
        {
            if (null == _visualRenderer)
                return;

            if (_isFrozen)
            {
                _visualRenderer.color = FROZEN_TINT;
                return;
            }

            if (null != _burnTokenSource)
            {
                _visualRenderer.color = BURNING_TINT;
                return;
            }

            _visualRenderer.color = _visualBaseColor;
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
            StopBurning();
            ClearFrozen();
            _collider.enabled = false;
            GainScore(_spec.BreakScore);
            PlayFX(_spec.BreakFX, transform.position);
            KillVisualTween(false);

            if (null != _visual)
                _visual.gameObject.SetActive(false);

            OnBroken?.Invoke();
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
