using UnityEngine;

namespace Pinball.Ingame.Stat
{
    [CreateAssetMenu(fileName = "PinballStatData", menuName = "Pinball/Ingame/PinballStatData")]
    public class PinballStatData : ScriptableObject
    {
        [SerializeField] int _initialBallCount = 3;
        [SerializeField] float _ballMass = 1f;
        [SerializeField] float _flipPower = 5f;
        [SerializeField] float _ballAttackPower = 1f;

        public int InitialBallCount => _initialBallCount;
        public float BallMass => _ballMass;
        public float FlipPower => _flipPower;
        public float BallAttackPower => _ballAttackPower;
    }
}
