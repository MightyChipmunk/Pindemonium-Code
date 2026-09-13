using System;
using Pinball.FX;
using UnityEngine;
using Alchemy.Inspector;
using GameCore.Util;

namespace Pinball.Ingame.Presentation
{
    [Serializable]
    public struct HitPresentationData
    {
        [SerializeField] EFXType _fxType;
        [SerializeField] bool _isHitStopEnabled;
        [SerializeField, ShowIf("_isHitStopEnabled")] float _hitStopTimeScale;
        [SerializeField, ShowIf("_isHitStopEnabled")] float _hitStopDuration;
        [SerializeField, ShowIf("_isHitStopEnabled")] float _recoverDuration;
        [SerializeField, ShowIf("_isHitStopEnabled")] EaseType _recoverEase;
        [SerializeField] bool _isCameraShakeEnabled;
        [SerializeField, ShowIf("_isCameraShakeEnabled")] ECameraShakeType _cameraShakeType;
        [SerializeField, ShowIf("_isCameraShakeEnabled")] float _cameraShakeDelay;

        public EFXType FXType => _fxType;
        public bool IsHitStopEnabled => _isHitStopEnabled;
        public float HitStopTimeScale => _hitStopTimeScale;
        public float HitStopDuration => _hitStopDuration;
        public float RecoverDuration => _recoverDuration;
        public EaseType RecoverEase => _recoverEase;
        public bool IsCameraShakeEnabled => _isCameraShakeEnabled;
        public ECameraShakeType CameraShakeType => _cameraShakeType;
        public float CameraShakeDelay => _cameraShakeDelay;
    }
}
