using System;
using Pinball.FX;
using Pinball.Util;
using UnityEngine;

namespace Pinball.Ingame.Presentation
{
    [Serializable]
    public struct HitPresentationData
    {
        [SerializeField] PoolableFX.EFXType _fxType;
        [SerializeField] bool _isHitStopEnabled;
        [SerializeField] float _hitStopTimeScale;
        [SerializeField] float _hitStopDuration;
        [SerializeField] float _recoverDuration;
        [SerializeField] EaseType _recoverEase;

        public PoolableFX.EFXType FXType => _fxType;
        public bool IsHitStopEnabled => _isHitStopEnabled;
        public float HitStopTimeScale => _hitStopTimeScale;
        public float HitStopDuration => _hitStopDuration;
        public float RecoverDuration => _recoverDuration;
        public EaseType RecoverEase => _recoverEase;
    }
}
