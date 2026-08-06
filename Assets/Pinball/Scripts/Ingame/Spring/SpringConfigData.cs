using UnityEngine;

namespace Pinball.Ingame
{
    [CreateAssetMenu(fileName = "SpringConfigData", menuName = "Pinball/Ingame/SpringConfigData")]
    public class SpringConfigData : ScriptableObject
    {
        [SerializeField] float _launchImpulse = 15f;
        [SerializeField] float _peakChargeDuration = 2f;
        [SerializeField] float _resetChargeDuration = 3f;
        [SerializeField] float _minPowerRatio = 0.45f;

        public float LaunchImpulse => _launchImpulse;
        public float PeakChargeDuration => _peakChargeDuration;
        public float ResetChargeDuration => _resetChargeDuration;
        public float MinPowerRatio => _minPowerRatio;
    }
}
