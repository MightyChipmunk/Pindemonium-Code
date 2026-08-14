using System;
using Pinball.FX;
using Pinball.StaticPoolModule;
using Pinball.Util;
using UnityEngine;

namespace Pinball.Ingame.Presentation
{
    public class IngameDirector
    {
        PresentationConfigData _config = null;
        HitStopPlayer _hitStopPlayer = null;
        Action<CameraController.ECameraShakeType, float> _cameraShaker = null;

        public void Init(PresentationConfigData config, FXPoolData fxPoolData, Action<CameraController.ECameraShakeType, float> cameraShaker)
        {
            _config = config;
            _cameraShaker = cameraShaker;
            StaticPool<PoolableFX.EFXType, PoolableFX>.Init(fxPoolData);
            _hitStopPlayer = new HitStopPlayer();
            _hitStopPlayer.Init();
        }

        public void Release()
        {
            _cameraShaker = null;
            _config = null;

            if (null != _hitStopPlayer)
            {
                _hitStopPlayer.Release();
                _hitStopPlayer = null;
            }

            StaticPool<PoolableFX.EFXType, PoolableFX>.Release();
        }

        public void PlayFlipperHit(Flipper.EHitGrade grade, Vector2 worldPoint)
        {
            if (null == _config)
                return;

            HitPresentationData presentation = _config.GetFlipperHit(grade);
            PlayFX(presentation.FXType, worldPoint);
            PlayCameraShake(presentation);
            _hitStopPlayer.Play(presentation);
        }

        public void PlaySpringCharge(Spring.EChargeEvent chargeEvent, Vector2 worldPoint)
        {
            if (null == _config)
                return;

            PlayFX(_config.GetSpringChargeFX(chargeEvent), worldPoint);
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
