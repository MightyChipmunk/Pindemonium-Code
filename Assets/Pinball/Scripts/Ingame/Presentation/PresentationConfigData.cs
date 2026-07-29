using Pinball.FX;
using UnityEngine;

namespace Pinball.Ingame.Presentation
{
    [CreateAssetMenu(fileName = "PresentationConfigData", menuName = "Pinball/Ingame/PresentationConfigData")]
    public class PresentationConfigData : ScriptableObject
    {
        [SerializeField] HitPresentationData _normalHit = default;
        [SerializeField] HitPresentationData _sweetSpotHit = default;
        [SerializeField] HitPresentationData _perfectHit = default;
        [SerializeField] PoolableFX.EFXType _springChargePeakFX = PoolableFX.EFXType.NonePooling;
        [SerializeField] PoolableFX.EFXType _springChargeCancelFX = PoolableFX.EFXType.NonePooling;

        public HitPresentationData GetFlipperHit(Flipper.EHitGrade grade)
        {
            switch (grade)
            {
                case Flipper.EHitGrade.SweetSpot:
                    return _sweetSpotHit;
                case Flipper.EHitGrade.Perfect:
                    return _perfectHit;
                default:
                    return _normalHit;
            }
        }

        public PoolableFX.EFXType GetSpringChargeFX(Spring.EChargeEvent chargeEvent)
        {
            switch (chargeEvent)
            {
                case Spring.EChargeEvent.Canceled:
                    return _springChargeCancelFX;
                default:
                    return _springChargePeakFX;
            }
        }
    }
}
