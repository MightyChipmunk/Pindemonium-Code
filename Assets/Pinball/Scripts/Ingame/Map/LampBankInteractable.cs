using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Pinball.FX;
using Pinball.Ingame.Shop;
using Pinball.Ingame.Stat;
using UnityEngine;

namespace Pinball.Ingame.Map
{
    public class LampBankInteractable : MapInteractable, IDamageable
    {
        const int SIREN_CYCLE_COUNT = 3;
        const float SIREN_STEP_INTERVAL = 0.08f;
        const float SIREN_CYCLE_INTERVAL = 0.12f;
        const int NO_LIT_LAMP = 0;
        const int MIN_REQUIRED_COUNT = 1;
        const int ROW_REFERENCE_INDEX = 0;
        const float ROW_CENTER_RATIO = 0.5f;
        const float FULL_LAMP_SCALE_RATIO = 1f;
        const float NO_WARMUP_RATIO = 0f;
        const float BASE_CLEAR_MULTIPLIER = 1f;
        const float GAUGE_IDLE_SCALE_RATIO = 0.7f;
        const float GAUGE_PEAK_SCALE_RATIO = 1.35f;
        const float GAUGE_DECAY_DURATION = 0.5f;

        static readonly Color WARMUP_TINT = new Color(1f, 0.18f, 0.16f, 1f);
        static readonly Color GAUGE_PEAK_TINT = new Color(1f, 0.85f, 0.35f, 1f);

        [SerializeField] Lamp[] _lamps = null;
        [SerializeField] SpriteRenderer _background = null;
        [SerializeField] LampMultiplierIndicator _multiplierIndicator = null;
        [SerializeField] Sprite _multiIgniteBackgroundSprite = null;
        [SerializeField] float _multiIgniteBackgroundAlpha = 0.5f;
        [SerializeField] SpriteRenderer _impactGauge = null;
        [SerializeField] float _impactGaugeReference = 30f;
        [SerializeField] int _baseRequiredCount = 3;
        [SerializeField] float _baseLampSpacing = 0.38f;
        [SerializeField] int _clearScoreReward = 0;
        [SerializeField] PoolableFX.EFXType _lightUpFX = PoolableFX.EFXType.LampLightUp;
        [SerializeField] PoolableFX.EFXType _clearFX = PoolableFX.EFXType.NonePooling;

        CancellationTokenSource _cancellationTokenSource = null;
        Tween _gaugeTween = null;
        Sprite _backgroundBaseSprite = null;
        float _backgroundAlpha = 1f;
        Vector3 _lampBasePosition = Vector3.zero;
        Vector3 _lampBaseScale = Vector3.one;
        Vector3 _gaugeBaseScale = Vector3.one;
        Color _backgroundBaseColor = Color.white;
        Color _gaugeBaseColor = Color.white;
        bool _hasCapturedLampBase = false;
        bool _hasCapturedBackgroundColor = false;
        bool _hasCapturedOverlayBase = false;
        int _hitCount = 0;
        int _requiredCount = 0;
        int _litLampCount = NO_LIT_LAMP;
        int _clearCount = 0;
        float _warmupElapsed = 0f;
        bool _isPlayingSiren = false;

        public override EGimmickType GimmickType => EGimmickType.LampBank;

        protected override void OnInit()
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            _hitCount = 0;
            _litLampCount = NO_LIT_LAMP;
            _clearCount = 0;
            _warmupElapsed = 0f;
            _isPlayingSiren = false;

            CaptureBackgroundBaseColor();
            CaptureOverlayBase();
            RefreshPassiveOverlay();
            ApplyWarmupTint(NO_WARMUP_RATIO);

            if (null != _multiplierIndicator)
            {
                _multiplierIndicator.Init();
                RefreshMultiplierIndicator();
            }

            if (null == _lamps)
                return;

            _requiredCount = Mathf.Clamp(StatProvider.GetLampRequiredCount(_baseRequiredCount), MIN_REQUIRED_COUNT, _lamps.Length);
            CaptureLampBaseTransforms();
            LayoutLamps();

            for (int i = 0; i < _lamps.Length; ++i)
                _lamps[i].Init();
        }

        void CaptureBackgroundBaseColor()
        {
            if (_hasCapturedBackgroundColor || null == _background)
                return;

            _hasCapturedBackgroundColor = true;
            _backgroundBaseColor = _background.color;
            _backgroundBaseSprite = _background.sprite;
        }

        void CaptureOverlayBase()
        {
            if (_hasCapturedOverlayBase)
                return;

            _hasCapturedOverlayBase = true;

            if (null == _impactGauge)
                return;

            _gaugeBaseColor = _impactGauge.color;
            _gaugeBaseScale = _impactGauge.transform.localScale;
        }

        void RefreshPassiveOverlay()
        {
            KillGaugeTween();
            RefreshBackgroundSprite();

            if (null == _impactGauge)
                return;

            _impactGauge.gameObject.SetActive(StatProvider.HasEffect(ShopItemData.EEffectType.LampAttackScore));
            _impactGauge.color = _gaugeBaseColor;
            _impactGauge.transform.localScale = _gaugeBaseScale * GAUGE_IDLE_SCALE_RATIO;
        }

        void RefreshBackgroundSprite()
        {
            if (null == _background)
                return;

            bool hasMultiIgnite = null != _multiIgniteBackgroundSprite && StatProvider.HasEffect(ShopItemData.EEffectType.LampMultiIgnite);
            _background.sprite = hasMultiIgnite ? _multiIgniteBackgroundSprite : _backgroundBaseSprite;
            _backgroundAlpha = hasMultiIgnite ? _multiIgniteBackgroundAlpha : _backgroundBaseColor.a;
        }

        void PlayImpactGauge(float impact)
        {
            if (null == _impactGauge || !_impactGauge.gameObject.activeSelf)
                return;

            float impactRatio = _impactGaugeReference <= 0f ? 0f : Mathf.Clamp01(impact / _impactGaugeReference);
            KillGaugeTween();
            _impactGauge.color = Color.Lerp(_gaugeBaseColor, GAUGE_PEAK_TINT, impactRatio);
            _impactGauge.transform.localScale = _gaugeBaseScale * Mathf.Lerp(GAUGE_IDLE_SCALE_RATIO, GAUGE_PEAK_SCALE_RATIO, impactRatio);

            Sequence decaySequence = DOTween.Sequence();
            decaySequence.Join(_impactGauge.transform.DOScale(_gaugeBaseScale * GAUGE_IDLE_SCALE_RATIO, GAUGE_DECAY_DURATION).SetEase(Ease.OutCubic));
            decaySequence.Join(_impactGauge.DOColor(_gaugeBaseColor, GAUGE_DECAY_DURATION));
            _gaugeTween = decaySequence;
        }

        void KillGaugeTween()
        {
            if (null == _gaugeTween)
                return;

            _gaugeTween.Kill(false);
            _gaugeTween = null;
        }

        void ApplyWarmupTint(float warmupRatio)
        {
            if (null == _background)
                return;

            Color color = Color.Lerp(_backgroundBaseColor, WARMUP_TINT, warmupRatio);
            color.a = _backgroundAlpha;
            _background.color = color;
        }

        void RefreshMultiplierIndicator()
        {
            if (null == _multiplierIndicator)
                return;

            _multiplierIndicator.SetMultiplier(GetCompletionMultiplier());
        }

        float GetCompletionMultiplier()
        {
            return BASE_CLEAR_MULTIPLIER + _clearCount * StatProvider.GetLampCompletionGrowth();
        }

        void CaptureLampBaseTransforms()
        {
            if (_hasCapturedLampBase)
                return;

            _hasCapturedLampBase = true;
            _lampBasePosition = _lamps[ROW_REFERENCE_INDEX].transform.localPosition;
            _lampBaseScale = _lamps[ROW_REFERENCE_INDEX].transform.localScale;
        }

        void LayoutLamps()
        {
            float rowWidth = _baseLampSpacing * _baseRequiredCount;
            float spacing = rowWidth / _requiredCount;
            float scaleRatio = Mathf.Min(FULL_LAMP_SCALE_RATIO, spacing / _baseLampSpacing);
            Vector3 rowPosition = _lampBasePosition;
            Vector3 lampScale = _lampBaseScale * scaleRatio;

            for (int i = 0; i < _lamps.Length; ++i)
            {
                _lamps[i].gameObject.SetActive(i < _requiredCount);
                if (i >= _requiredCount)
                    continue;

                rowPosition.x = spacing * (i + ROW_CENTER_RATIO) - rowWidth * ROW_CENTER_RATIO;
                _lamps[i].transform.localPosition = rowPosition;
                _lamps[i].transform.localScale = lampScale;
            }
        }

        protected override void OnRelease()
        {
            if (null != _cancellationTokenSource)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }

            _isPlayingSiren = false;
            _requiredCount = 0;
            KillGaugeTween();
            ApplyWarmupTint(NO_WARMUP_RATIO);

            if (null != _multiplierIndicator)
                _multiplierIndicator.Release();

            if (null == _lamps)
                return;

            for (int i = 0; i < _lamps.Length; ++i)
                _lamps[i].Release();
        }

        public void TickWarmup(float deltaTime)
        {
            if (_isPlayingSiren || IsDeactivated)
                return;

            _warmupElapsed += deltaTime;
            ApplyWarmupTint(StatProvider.GetLampWarmupRatio(_warmupElapsed));
        }

        public void TakeHit(float damage, Vector2 worldPoint)
        {
            ApplyLampHit(damage, worldPoint);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsBallLayer(other.gameObject.layer))
                return;

            Rigidbody2D body = other.attachedRigidbody;
            if (null == body || body.bodyType != RigidbodyType2D.Dynamic)
                return;

            ApplyLampHit(EvaluateAttackPower(other) * body.linearVelocity.magnitude, other.bounds.center);
        }

        void ApplyLampHit(float impact, Vector2 worldPoint)
        {
            if (_isPlayingSiren || IsDeactivated)
                return;

            if (null == _lamps || _lamps.Length == 0)
                return;

            _hitCount += StatProvider.GetLampIgniteCount(impact);
            GainScore(Mathf.RoundToInt(StatProvider.GetLampAttackScore(impact)), worldPoint, EScoreSource.PassiveEffect);
            PlayImpactGauge(impact);
            LightUpProgressLamps();
            PlayGimmickHit();

            if (_hitCount < _requiredCount)
                return;

            Clear();
        }

        void LightUpProgressLamps()
        {
            int targetLampCount = Mathf.Clamp(_hitCount, NO_LIT_LAMP, _requiredCount);
            for (int i = _litLampCount; i < targetLampCount; ++i)
            {
                _lamps[i].PlayLightUp();
                PlayFX(_lightUpFX, _lamps[i].transform.position);
            }

            _litLampCount = Mathf.Max(_litLampCount, targetLampCount);
        }

        void Clear()
        {
            _isPlayingSiren = true;
            float baseScore = _clearScoreReward * StatProvider.GetLampScoreMultiplier();
            float clearMultiplier = StatProvider.GetLampWarmupMultiplier(_warmupElapsed) * GetCompletionMultiplier();
            GainScore(Mathf.RoundToInt(baseScore * clearMultiplier));
            ++_clearCount;
            _warmupElapsed = 0f;
            ApplyWarmupTint(NO_WARMUP_RATIO);
            RefreshMultiplierIndicator();
            PlayFX(_clearFX, transform.position);
            PlayBankClearedSound();

            if (null != _cancellationTokenSource)
                PlaySirenAsync(_cancellationTokenSource.Token).Forget();
        }

        async UniTaskVoid PlaySirenAsync(CancellationToken cancellationToken)
        {
            for (int cycle = 0; cycle < SIREN_CYCLE_COUNT; ++cycle)
            {
                for (int i = 0; i < _requiredCount; ++i)
                {
                    SetAllLit(false);
                    _lamps[i].SetLit(true);
                    await UniTask.Delay(TimeSpan.FromSeconds(SIREN_STEP_INTERVAL), cancellationToken: cancellationToken);
                }

                SetAllLit(false);
                await UniTask.Delay(TimeSpan.FromSeconds(SIREN_CYCLE_INTERVAL), cancellationToken: cancellationToken);
            }

            _hitCount = 0;
            _litLampCount = NO_LIT_LAMP;
            _isPlayingSiren = false;
        }

        void SetAllLit(bool isLit)
        {
            for (int i = 0; i < _requiredCount; ++i)
                _lamps[i].SetLit(isLit);
        }

#if UNITY_EDITOR
        const string BACKGROUND_NODE_NAME = "BG";

        protected override void OnBindSerializedField()
        {
            base.OnBindSerializedField();
            _lamps = GetComponentsInChildren<Lamp>(true);
            _multiplierIndicator = GetComponentInChildren<LampMultiplierIndicator>(true);

            Transform backgroundTransform = transform.Find(BACKGROUND_NODE_NAME);
            _background = null == backgroundTransform ? null : backgroundTransform.GetComponent<SpriteRenderer>();
        }
#endif // UNITY_EDITOR
    }
}
