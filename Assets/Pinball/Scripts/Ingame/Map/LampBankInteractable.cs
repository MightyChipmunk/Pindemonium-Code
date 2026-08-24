using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Pinball.FX;
using Pinball.Ingame.Stat;
using UnityEngine;

namespace Pinball.Ingame.Map
{
    public class LampBankInteractable : MapInteractable
    {
        const int SIREN_CYCLE_COUNT = 3;
        const float SIREN_STEP_INTERVAL = 0.08f;
        const float SIREN_CYCLE_INTERVAL = 0.12f;
        const int NO_LIT_LAMP = 0;

        [SerializeField] Lamp[] _lamps = null;
        [SerializeField] int _clearScoreReward = 0;
        [SerializeField] PoolableFX.EFXType _lightUpFX = PoolableFX.EFXType.LampLightUp;
        [SerializeField] PoolableFX.EFXType _clearFX = PoolableFX.EFXType.NonePooling;

        CancellationTokenSource _cancellationTokenSource = null;
        int _hitCount = 0;
        int _requiredCount = 0;
        int _litLampCount = NO_LIT_LAMP;
        bool _isPlayingSiren = false;

        public override EGimmickType GimmickType => EGimmickType.LampBank;

        protected override void OnInit()
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            _hitCount = 0;
            _litLampCount = NO_LIT_LAMP;
            _isPlayingSiren = false;

            if (null == _lamps)
                return;

            _requiredCount = StatProvider.GetLampRequiredCount(_lamps.Length);
            for (int i = 0; i < _lamps.Length; ++i)
                _lamps[i].Init();
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

            if (null == _lamps)
                return;

            for (int i = 0; i < _lamps.Length; ++i)
                _lamps[i].Release();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_isPlayingSiren)
                return;

            if (null == _lamps || _lamps.Length == 0)
                return;

            if (!IsBallLayer(other.gameObject.layer))
                return;

            ++_hitCount;
            LightUpProgressLamps();
            PlayGimmickHit();

            if (_hitCount < _requiredCount)
                return;

            Clear();
        }

        void LightUpProgressLamps()
        {
            int targetLampCount = Mathf.Clamp(Mathf.RoundToInt((float)_hitCount * _lamps.Length / _requiredCount), NO_LIT_LAMP, _lamps.Length);
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
            GainScore(Mathf.RoundToInt(_clearScoreReward * StatProvider.GetLampScoreMultiplier()));
            PlayFX(_clearFX, transform.position);
            PlayBankClearedSound();

            if (null != _cancellationTokenSource)
                PlaySirenAsync(_cancellationTokenSource.Token).Forget();
        }

        async UniTaskVoid PlaySirenAsync(CancellationToken cancellationToken)
        {
            for (int cycle = 0; cycle < SIREN_CYCLE_COUNT; ++cycle)
            {
                for (int i = 0; i < _lamps.Length; ++i)
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
            for (int i = 0; i < _lamps.Length; ++i)
                _lamps[i].SetLit(isLit);
        }

#if UNITY_EDITOR
        protected override void OnBindSerializedField()
        {
            _lamps = GetComponentsInChildren<Lamp>(true);
        }
#endif // UNITY_EDITOR
    }
}
