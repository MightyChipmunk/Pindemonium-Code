using UnityEngine;

namespace Pinball.Ingame.Stage
{
    [CreateAssetMenu(fileName = "StageData", menuName = "Pinball/Ingame/StageData")]
    public class StageData : ScriptableObject
    {
        [SerializeField] int _targetScore = 5000;

        public int TargetScore => _targetScore;
    }
}
