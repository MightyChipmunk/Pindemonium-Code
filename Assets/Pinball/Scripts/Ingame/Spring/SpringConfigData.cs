using UnityEngine;

namespace Pinball.Ingame
{
    [CreateAssetMenu(fileName = "SpringConfigData", menuName = "Pinball/Ingame/SpringConfigData")]
    public class SpringConfigData : ScriptableObject
    {
        [SerializeField] float _launchImpulse = 30f;
        [SerializeField] float _peakChargeDuration = 2f;
        [SerializeField] float _resetChargeDuration = 3f;
        [SerializeField] float _minPowerRatio = 0.225f;
        [SerializeField, Range(0f, 1f)] float _launchInitialImpulseRatio = 0.25f;
        [SerializeField] float _launchAccelDuration = 0.12f;

        public float LaunchImpulse => _launchImpulse;
        public float PeakChargeDuration => _peakChargeDuration;
        public float ResetChargeDuration => _resetChargeDuration;
        public float MinPowerRatio => _minPowerRatio;
        public float LaunchInitialImpulseRatio => _launchInitialImpulseRatio;
        public float LaunchAccelDuration => _launchAccelDuration;
    }
}
