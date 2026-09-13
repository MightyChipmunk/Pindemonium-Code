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
        [SerializeField] EFXType _springChargePeakFX = EFXType.NonePooling;
        [SerializeField] EFXType _springChargeCancelFX = EFXType.NonePooling;

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

        public EFXType GetSpringChargeFX(Spring.EChargeEvent chargeEvent)
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
