using System;
using Pinball.Audio;
using Pinball.FX;
using Pinball.StaticPoolModule;
using Pinball.Util;
using UnityEngine;

namespace Pinball.Ingame.Presentation
{
    public class IngameDirector : IDisposable
    {
        const float NUDGE_SHAKE_DELAY = 0f;

        PresentationConfigData _config = null;
        HitStopPlayer _hitStopPlayer = null;
        Action<CameraController.ECameraShakeType, float> _cameraShaker = null;

        public IngameDirector(PresentationConfigData config, FXPoolData fxPoolData, Action<CameraController.ECameraShakeType, float> cameraShaker)
        {
            _config = config;
            _cameraShaker = cameraShaker;
            StaticPool<PoolableFX.EFXType, PoolableFX>.Init(fxPoolData);
            _hitStopPlayer = new HitStopPlayer();
        }

        public void Dispose()
        {
            if (null != _hitStopPlayer)
            {
                _hitStopPlayer.Dispose();
                _hitStopPlayer = null;
            }

            StaticPool<PoolableFX.EFXType, PoolableFX>.Release();
        }

        public void SetPaused(bool isPaused)
        {
            if (null == _hitStopPlayer)
                return;

            _hitStopPlayer.SetPaused(isPaused);
        }

        public void PlayFlipperHit(Flipper.EHitGrade grade, Vector2 worldPoint)
        {
            if (null == _config)
                return;

            HitPresentationData presentation = _config.GetFlipperHit(grade);
            PlayFX(presentation.FXType, worldPoint);
            PlayCameraShake(presentation);
            _hitStopPlayer.Play(presentation);
            AudioManager.Play(GetFlipperHitClipType(grade));
        }

        static AudioStorage.EAudioClipType GetFlipperHitClipType(Flipper.EHitGrade grade)
        {
            switch (grade)
            {
                case Flipper.EHitGrade.Perfect:
                    return AudioStorage.EAudioClipType.FlipperHitPerfect;
                case Flipper.EHitGrade.SweetSpot:
                    return AudioStorage.EAudioClipType.FlipperHitSweetSpot;
                default:
                    return AudioStorage.EAudioClipType.FlipperHitNormal;
            }
        }

        public void PlaySpringCharge(Spring.EChargeEvent chargeEvent, Vector2 worldPoint)
        {
            if (null == _config)
                return;

            PlayFX(_config.GetSpringChargeFX(chargeEvent), worldPoint);
            AudioManager.Play(GetSpringChargeClipType(chargeEvent));
        }

        static AudioStorage.EAudioClipType GetSpringChargeClipType(Spring.EChargeEvent chargeEvent)
        {
            if (chargeEvent == Spring.EChargeEvent.Peaked)
                return AudioStorage.EAudioClipType.SpringChargePeak;

            return AudioStorage.EAudioClipType.SpringChargeCancel;
        }

        public void PlayNudge()
        {
            _cameraShaker?.Invoke(CameraController.ECameraShakeType.Nudge, NUDGE_SHAKE_DELAY);
            AudioManager.Play(AudioStorage.EAudioClipType.EffectNudge);
        }

        public void PlayComboUp()
        {
            AudioManager.Play(AudioStorage.EAudioClipType.ComboUp);
        }

        public void PlaySurgeCharged()
        {
            AudioManager.Play(AudioStorage.EAudioClipType.EffectPowerSurgeCharged);
        }

        public void PlayStageClear()
        {
            AudioManager.Play(AudioStorage.EAudioClipType.StageClear);
        }

        public void PlayGameResult(bool isClear)
        {
            AudioManager.Play(isClear ? AudioStorage.EAudioClipType.GameClear : AudioStorage.EAudioClipType.GameOver);
        }

        void PlayCameraShake(HitPresentationData presentation)
        {
            if (!presentation.IsCameraShakeEnabled)
                return;

            _cameraShaker?.Invoke(presentation.CameraShakeType, presentation.CameraShakeDelay);
        }

        public void PlayLineFX(PoolableFX.EFXType fxType, Vector2 startPoint, Vector2 endPoint)
        {
            if (fxType == PoolableFX.EFXType.NonePooling)
                return;

            PoolableLineFX lineFX = StaticPool<PoolableFX.EFXType, PoolableFX>.GetObject(fxType) as PoolableLineFX;
            if (null == lineFX)
                return;

            lineFX.transform.position = startPoint;
            lineFX.SetPoints(startPoint, endPoint);
            lineFX.PlayFX();
        }

        public void PlayFX(PoolableFX.EFXType fxType, Vector2 worldPoint)
        {
            if (fxType == PoolableFX.EFXType.NonePooling)
                return;

            PoolableFX fx = StaticPool<PoolableFX.EFXType, PoolableFX>.GetObject(fxType);
            if (null == fx)
                return;

            fx.transform.position = worldPoint;
            fx.PlayFX();
        }
    }
}
