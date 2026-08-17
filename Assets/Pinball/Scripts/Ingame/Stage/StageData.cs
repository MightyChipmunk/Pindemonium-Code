using UnityEngine;

namespace Pinball.Ingame.Stage
{
    [CreateAssetMenu(fileName = "StageData", menuName = "Pinball/Ingame/StageData")]
    public class StageData : ScriptableObject, IStageGimmickSource
    {
        [SerializeField] int _targetScore = 5000;
        [SerializeField] StageGimmickData[] _gimmicks = null;

        public int TargetScore => _targetScore;
        public int GimmickCount => null == _gimmicks ? 0 : _gimmicks.Length;

        public StageGimmickData GetGimmick(int index)
        {
            if (index < 0 || index >= GimmickCount)
                return null;

            return _gimmicks[index];
        }
    }
}
