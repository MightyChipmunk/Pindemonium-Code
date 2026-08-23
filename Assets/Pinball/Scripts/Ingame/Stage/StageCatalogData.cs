using System;
using UnityEngine;

namespace Pinball.Ingame.Stage
{
    [CreateAssetMenu(fileName = "StageCatalogData", menuName = "Pinball/Ingame/StageCatalogData")]
    public class StageCatalogData : ScriptableObject
    {
        public const int STAGES_PER_ROUND = 3;

        [SerializeField] StageData[] _stages = null;

        public int Count => null == _stages ? 0 : _stages.Length;
        public int RoundCount => Count / STAGES_PER_ROUND;

        public static int GetRoundIndex(int stageIndex)
        {
            if (stageIndex < 0)
                return 0;

            return stageIndex / STAGES_PER_ROUND;
        }

        public static int GetRoundStartStageIndex(int roundIndex)
        {
            if (roundIndex < 0)
                return 0;

            return roundIndex * STAGES_PER_ROUND;
        }

        public static bool IsRoundLastStage(int stageIndex)
        {
            if (stageIndex < 0)
                return false;

            return (stageIndex + 1) % STAGES_PER_ROUND == 0;
        }

        public StageData GetStage(int stageIndex)
        {
            if (Count <= 0)
                return null;

            if (stageIndex < 0)
                return null;

            return _stages[stageIndex % Count];
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            int remainder = Count % STAGES_PER_ROUND;
            if (remainder == 0)
                return;

            int paddedCount = Count + STAGES_PER_ROUND - remainder;
            Array.Resize(ref _stages, paddedCount);
            Debug.LogError($"{name}: 스테이지 개수는 라운드 단위({STAGES_PER_ROUND}개)의 배수여야 합니다. {paddedCount}개로 늘렸으니 빈 슬롯을 채우세요.", this);
        }
#endif
    }
}
