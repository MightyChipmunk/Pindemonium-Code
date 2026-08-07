using UnityEngine;

namespace Pinball.Ingame.Stage
{
    [CreateAssetMenu(fileName = "StageCatalogData", menuName = "Pinball/Ingame/StageCatalogData")]
    public class StageCatalogData : ScriptableObject
    {
        [SerializeField] StageData[] _stages = null;

        public int Count => null == _stages ? 0 : _stages.Length;

        public StageData GetStage(int stageIndex)
        {
            if (Count <= 0)
                return null;

            if (stageIndex < 0)
                return null;

            return _stages[stageIndex % Count];
        }
    }
}
