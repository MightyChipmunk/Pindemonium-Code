using Pinball.FX;
using Pinball.StaticPoolModule;
using UnityEngine;

namespace Pinball.Ingame.Presentation
{
    public class IngameDirector : MonoBehaviour
    {
        [SerializeField] PresentationConfigData _config = null;
        [SerializeField] FXPoolData _fxPoolData = null;

        HitStopPlayer _hitStopPlayer = null;

        public void Init()
        {
            StaticPool<PoolableFX.EFXType, PoolableFX>.Init(_fxPoolData);
            _hitStopPlayer = new HitStopPlayer();
            _hitStopPlayer.Init();
        }

        public void Release()
        {
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

            if (!presentation.IsHitStopEnabled)
                return;

            _hitStopPlayer.Play(presentation.HitStopTimeScale, presentation.HitStopDuration, presentation.RecoverDuration, presentation.RecoverEase);
        }

        void PlayFX(PoolableFX.EFXType fxType, Vector2 worldPoint)
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
