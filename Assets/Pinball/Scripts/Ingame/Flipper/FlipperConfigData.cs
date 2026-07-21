using UnityEngine;

namespace Pinball.Ingame
{
    [CreateAssetMenu(fileName = "FlipperConfigData", menuName = "Pinball/Ingame/FlipperConfigData")]
    public class FlipperConfigData : ScriptableObject
    {
        [SerializeField] float _restAngle = -25f;
        [SerializeField] float _pressedAngle = 35f;
        [SerializeField] float _rotateSpeed = 600f;
        [SerializeField] float _flipperLength = 1.3f;
        [SerializeField] float _sweetSpotMinRatio = 0.6f;
        [SerializeField] float _sweetSpotMaxRatio = 0.9f;
        [SerializeField] float _sweetSpotBonusMultiplier = 1.5f;
        [SerializeField] float _perfectBonusMultiplier = 2f;
        [SerializeField] float _perfectTimingMin = 0.3f;
        [SerializeField] float _perfectTimingMax = 0.6f;

        public float RestAngle => _restAngle;
        public float PressedAngle => _pressedAngle;
        public float RotateSpeed => _rotateSpeed;
        public float FlipperLength => _flipperLength;
        public float SweetSpotMinRatio => _sweetSpotMinRatio;
        public float SweetSpotMaxRatio => _sweetSpotMaxRatio;
        public float SweetSpotBonusMultiplier => _sweetSpotBonusMultiplier;
        public float PerfectBonusMultiplier => _perfectBonusMultiplier;
        public float PerfectTimingMin => _perfectTimingMin;
        public float PerfectTimingMax => _perfectTimingMax;
    }
}
