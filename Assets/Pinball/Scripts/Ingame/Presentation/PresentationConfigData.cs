using UnityEngine;

namespace Pinball.Ingame.Presentation
{
    [CreateAssetMenu(fileName = "PresentationConfigData", menuName = "Pinball/Ingame/PresentationConfigData")]
    public class PresentationConfigData : ScriptableObject
    {
        [SerializeField] HitPresentationData _normalHit = default;
        [SerializeField] HitPresentationData _sweetSpotHit = default;
        [SerializeField] HitPresentationData _perfectHit = default;

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
    }
}
