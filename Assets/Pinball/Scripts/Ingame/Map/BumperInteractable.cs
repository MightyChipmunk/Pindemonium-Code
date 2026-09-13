using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Pinball.Audio;
using Pinball.FX;
using Pinball.Ingame.Shop;
using Pinball.Ingame.Stat;
using Pinball.Util;
using UnityEngine;

namespace Pinball.Ingame.Map
{
    public class BumperInteractable : CollisionInteractable, IDamageable
    {
        const float HIT_PUNCH_RATIO = 0.7f;
        const float HIT_PUNCH_DURATION = 0.2f;
        const int HIT_PUNCH_VIBRATO = 9;
        const float RESPAWN_PUNCH_RATIO = 0.6f;
        const float RESPAWN_PUNCH_DURATION = 0.15f;
        const int RESPAWN_PUNCH_VIBRATO = 6;
        const float PROMOTE_PUNCH_RATIO = 1f;
        const float PROMOTE_PUNCH_DURATION = 0.3f;
        const int PROMOTE_PUNCH_VIBRATO = 10;
        const float MIN_BURN_INTERVAL = 0.05f;
        const float MIN_RESPAWN_DELAY_SCALE = 0.1f;
        const float NO_OVERKILL_SCORE_RATIO = 0f;
        const float SNIPER_MARK_SPIN_SPEED = 90f;
        const float DEFAULT_HEALTH_MULTIPLIER = 1f;
        const float MIN_HEALTH_MULTIPLIER = 0.1f;

        static readonly Color BURNING_TINT = new Color(1f, 0.55f, 0.25f, 1f);
        static readonly Color FROZEN_TINT = new Color(0.5f, 0.85f, 1f, 1f);

        [SerializeField] BumperGradeData _gradeData = null;
        [SerializeField] BumperGradeData.EGrade _grade = BumperGradeData.EGrade.Yellow;
        [SerializeField] Collider2D _collider = null;
        [SerializeField] Transform _visual = null;
        [SerializeField] SpriteRenderer _visualRenderer = null;
        [SerializeField] Transform _sniperMark = null;
        [SerializeField] SpriteRenderer _fortifyRing = null;

        CancellationTokenSource _respawnTokenSource = null;
        CancellationTokenSource _burnTokenSource = null;
        EFXType _burnFX = EFXType.NonePooling;
        EFXType _shatterFX = EFXType.NonePooling;
        Tween _visualTween = null;
        BumperGradeData.GradeSpec _spec = default;
        BumperGradeData.EGrade _baseGrade = BumperGradeData.EGrade.Yellow;
        Vector3 _visualBaseScale = Vector3.one;
        Color _visualBaseColor = Color.white;
        float _baseRespawnDelay = 0f;
        float _currentHealth = 0f;
        float _healthMultiplier = DEFAULT_HEALTH_MULTIPLIER;
        float _frozenDamageMultiplier = DEFAULT_DAMAGE_MULTIPLIER;
        float _sniperMarkRemain = 0f;
        float _radius = 0f;
        bool _isBroken = false;
        bool _isFrozen = false;
        bool _isSniperMarked = false;

        public override EGimmickType GimmickType => EGimmickType.Bumper;
        public bool IsBroken => _isBroken;
        public bool IsSniperMarked => _isSniperMarked;

        public event Action OnBroken;
        public event Action<BumperInteractable, float, Collider2D> OnBallDamaged;

        public BumperGradeData.EGrade GetGrade()
        {
            return _grade;
        }

        protected override void OnInit()
        {
            _baseGrade = _grade;

            if (null != _gradeData)
                _spec = _gradeData.GetSpec(_grade);

            _baseRespawnDelay = _spec.RespawnDelay;

            if (null != _visual)
                _visualBaseScale = _visual.localScale;

            if (null != _visualRenderer)
                _visualBaseColor = _visualRenderer.color;

            ApplyGradeVisual();
            RefreshFortifyRing();

            _currentHealth = GetMaxHealth();
            _isBroken = false;
            ClearFrozen();
            ClearSniperMark();
            _collider.enabled = true;
            _radius = CalculateRadius();
        }

        protected override void OnRelease()
        {
            OnBroken = null;
            OnBallDamaged = null;
            ApplyGrade(_baseGrade);
            StopBurning();
            StopRespawnWait();
            ClearFrozen();
            ClearSniperMark();
            KillVisualTween(false);
            _isBroken = false;
        }

        protected override void OnDeactivated()
        {
            StopBurning();
            StopRespawnWait();
            ClearFrozen();
            ClearSniperMark();
            KillVisualTween(false);
            base.OnDeactivated();
        }

        protected override Vector2 GetBounceDirection(Collision2D collision)
        {
            return (collision.rigidbody.position - (Vector2)transform.position).normalized;
        }

        protected override EFXType GetBallHitFX()
        {
            if (_isBroken)
                return EFXType.NonePooling;

            return _spec.HitFX;
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
            float multiplier = _isFrozen ? _frozenDamageMultiplier : DEFAULT_DAMAGE_MULTIPLIER;
            if (!_isSniperMarked)
                return multiplier;

            return multiplier * Mathf.Max(DEFAULT_DAMAGE_MULTIPLIER, StatProvider.GetEffectValue(ShopItemData.EEffectType.BumperSniperMark, ShopItemData.MAIN_VALUE_INDEX));
        }

        protected override void OnBallHit(Collision2D collision, float damage)
        {
            if (_isBroken)
                return;

            PlayVisualPunch(HIT_PUNCH_RATIO, HIT_PUNCH_DURATION, HIT_PUNCH_VIBRATO);

            if (null == _gradeData)
                return;

            float overkillScoreRatio = ConsumeSniperMark();
            ReduceHealth(damage, overkillScoreRatio);
            OnBallDamaged?.Invoke(this, damage, collision.collider);
        }

        public void SetHealthMultiplier(float multiplier)
        {
            _healthMultiplier = Mathf.Max(MIN_HEALTH_MULTIPLIER, multiplier);
        }

        public float GetRadius()
        {
            return _radius;
        }

        public int GetBreakScore()
        {
            return _spec.BreakScore;
        }

        public void ResetGrade()
        {
            if (_grade == _baseGrade)
                return;

            ApplyGrade(_baseGrade);
            _currentHealth = Mathf.Min(_currentHealth, GetMaxHealth());
        }

        void RefreshFortifyRing()
        {
            if (null == _fortifyRing)
                return;

            _fortifyRing.gameObject.SetActive(StatProvider.HasEffect(ShopItemData.EEffectType.BumperFortify));
        }

        bool TryPromoteGrade()
        {
            if (_grade >= BumperGradeData.MAX_GRADE)
                return false;

            ApplyGrade(_grade + 1);
            return true;
        }

        void ApplyGrade(BumperGradeData.EGrade grade)
        {
            if (_grade == grade)
                return;

            _grade = grade;
            if (null == _gradeData)
                return;

            _spec = _gradeData.GetSpec(_grade);
            ApplyGradeVisual();
            _radius = CalculateRadius();
        }

        float GetMaxHealth()
        {
            return _spec.MaxHealth * _healthMultiplier;
        }

        float CalculateRadius()
        {
            CircleCollider2D circleCollider = _collider as CircleCollider2D;
            if (null == circleCollider)
                return _collider.bounds.extents.x;

            return circleCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
        }

        public bool CanApplySniperMark()
        {
            return !_isBroken && !IsDeactivated && !_isSniperMarked;
        }

        public void ApplySniperMark(float durationSeconds)
        {
            if (!CanApplySniperMark())
                return;

            if (durationSeconds <= 0f)
                return;

            _isSniperMarked = true;
            _sniperMarkRemain = durationSeconds;
            AudioManager.Play(AudioStorage.EAudioClipType.EffectSniperMark);
            RefreshSniperMarkVisual();
        }

        public void TickSniperMark(float deltaTime)
        {
            if (!_isSniperMarked)
                return;

            if (null != _sniperMark)
                _sniperMark.Rotate(Vector3.forward, SNIPER_MARK_SPIN_SPEED * deltaTime);

            _sniperMarkRemain -= deltaTime;
            if (_sniperMarkRemain > 0f)
                return;

            AudioManager.Play(AudioStorage.EAudioClipType.EffectSniperExpire);
            ClearSniperMark();
        }

        float ConsumeSniperMark()
        {
            if (!_isSniperMarked)
                return NO_OVERKILL_SCORE_RATIO;

            AudioManager.Play(AudioStorage.EAudioClipType.EffectSniperHit);
            ClearSniperMark();
            return StatProvider.GetEffectValue(ShopItemData.EEffectType.BumperSniperMark, ShopItemData.BONUS_VALUE_INDEX);
        }

        void ClearSniperMark()
        {
            _isSniperMarked = false;
            _sniperMarkRemain = 0f;
            RefreshSniperMarkVisual();
        }

        void RefreshSniperMarkVisual()
        {
            if (null == _sniperMark)
                return;

            if (!_isSniperMarked)
                _sniperMark.localRotation = Quaternion.identity;

            _sniperMark.gameObject.SetActive(_isSniperMarked);
        }

        public void TakeHit(float damage, Vector2 worldPoint)
        {
            if (IsDeactivated)
                return;

            PlayGimmickHitSound();
            ApplyDamage(damage, EFXType.NonePooling);
        }

        public void ApplyDamage(float damage, EFXType extraFX)
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
            GainScore(ScoreMath.RoundToScore(damage * GetScoreMultiplier()), transform.position, EScoreSource.PassiveEffect);
            ReduceHealth(damage, NO_OVERKILL_SCORE_RATIO);
        }

        public void GainHitScore(int score)
        {
            if (_isBroken)
                return;

            if (score <= 0)
                return;

            GainScore(score, transform.position, EScoreSource.PassiveEffect);
        }

        public void Ignite(float damagePerTick, float intervalSeconds, float durationSeconds, EFXType burnFX)
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
            AudioManager.Play(AudioStorage.EAudioClipType.EffectBurn);
            float tickInterval = Mathf.Max(MIN_BURN_INTERVAL, intervalSeconds);
            BurnAsync(damagePerTick, tickInterval, Mathf.Max(tickInterval, durationSeconds), _burnTokenSource.Token).Forget();
        }

        public void ApplyFrostHit(float shatterDamageMultiplier, EFXType freezeFX, EFXType shatterFX)
        {
            if (_isBroken)
                return;

            if (_isFrozen)
            {
                ShatterFrozen();
                return;
            }

            _isFrozen = true;
            _frozenDamageMultiplier = Mathf.Max(DEFAULT_DAMAGE_MULTIPLIER, shatterDamageMultiplier);
            _shatterFX = shatterFX;
            PlayFX(freezeFX, transform.position);
            AudioManager.Play(AudioStorage.EAudioClipType.EffectFreeze);
            RefreshVisualTint();
        }

        void ShatterFrozen()
        {
            PlayFX(_shatterFX, transform.position);
            AudioManager.Play(AudioStorage.EAudioClipType.EffectFrostShatter);
            ClearFrozen();
        }

        void ClearFrozen()
        {
            _isFrozen = false;
            _frozenDamageMultiplier = DEFAULT_DAMAGE_MULTIPLIER;
            _shatterFX = EFXType.NonePooling;
            RefreshVisualTint();
        }

        void ReduceHealth(float damage, float overkillScoreRatio)
        {
            if (StatProvider.HasEffect(ShopItemData.EEffectType.BumperFortify))
                return;

            _currentHealth -= damage;
            if (_currentHealth >= 0f)
                return;

            if (overkillScoreRatio > NO_OVERKILL_SCORE_RATIO)
                GainScore(ScoreMath.RoundToScore(-_currentHealth * GetScoreMultiplier() * overkillScoreRatio), transform.position, EScoreSource.PassiveEffect);

            Break();
        }

        async UniTaskVoid BurnAsync(float damagePerTick, float intervalSeconds, float durationSeconds, CancellationToken cancellationToken)
        {
            int tickCount = Mathf.Max(1, Mathf.RoundToInt(durationSeconds / intervalSeconds));
            for (int i = 0; i < tickCount; ++i)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(intervalSeconds), cancellationToken: cancellationToken);

                if (_isBroken)
                    return;

                PlayFX(_burnFX, transform.position);
                GainScore(ScoreMath.RoundToScore(damagePerTick * GetScoreMultiplier()), transform.position, EScoreSource.PassiveEffect);
                ReduceHealth(damagePerTick, NO_OVERKILL_SCORE_RATIO);
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

            _burnFX = EFXType.NonePooling;
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

            if (_isFrozen)
                ShatterFrozen();

            ClearSniperMark();
            _collider.enabled = false;
            GainScore(_spec.BreakScore, transform.position, EScoreSource.GimmickBreak);
            PlayFX(_spec.BreakFX, transform.position);
            AudioManager.Play(AudioStorage.EAudioClipType.BumperBreak);
            KillVisualTween(false);

            if (null != _visual)
                _visual.gameObject.SetActive(false);

            StopRespawnWait();
            _respawnTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            RespawnAsync(_respawnTokenSource.Token).Forget();

            OnBroken?.Invoke();
        }

        void StopRespawnWait()
        {
            if (null == _respawnTokenSource)
                return;

            _respawnTokenSource.Cancel();
            _respawnTokenSource.Dispose();
            _respawnTokenSource = null;
        }

        float GetRespawnDelay()
        {
            float scale = Mathf.Max(MIN_RESPAWN_DELAY_SCALE, 1f - StatProvider.GetEffectSum(ShopItemData.EEffectType.BumperRespawnTimeRatio));
            return _baseRespawnDelay * scale * StatProvider.GetBumperRespawnDelayScale();
        }

        public void RespawnImmediately()
        {
            if (!_isBroken || IsDeactivated)
                return;

            StopRespawnWait();
            Respawn();
        }

        async UniTaskVoid RespawnAsync(CancellationToken cancellationToken)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(GetRespawnDelay()), cancellationToken: cancellationToken);

            StopRespawnWait();
            Respawn();
        }

        void Respawn()
        {
            bool isPromoted = StatProvider.HasEffect(ShopItemData.EEffectType.BumperForge) && TryPromoteGrade();
            _currentHealth = GetMaxHealth();
            _isBroken = false;
            _collider.enabled = true;

            if (null == _visual)
                return;

            _visual.gameObject.SetActive(true);
            _visual.localScale = _visualBaseScale;

            if (!isPromoted)
            {
                PlayVisualPunch(RESPAWN_PUNCH_RATIO, RESPAWN_PUNCH_DURATION, RESPAWN_PUNCH_VIBRATO);
                return;
            }

            PlayFX(_spec.HitFX, transform.position);
            PlayVisualPunch(PROMOTE_PUNCH_RATIO, PROMOTE_PUNCH_DURATION, PROMOTE_PUNCH_VIBRATO);
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
            base.OnBindSerializedField();
            _collider = GetComponent<Collider2D>();
            _visualRenderer = GetComponentInChildren<SpriteRenderer>(true);

            if (null != _visualRenderer)
                _visual = _visualRenderer.transform;
        }
#endif // UNITY_EDITOR
    }
}
