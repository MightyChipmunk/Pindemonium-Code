using UnityEngine;

namespace Pinball.Ingame.Stage
{
    [CreateAssetMenu(fileName = "EndlessConfigData", menuName = "Pinball/Ingame/EndlessConfigData")]
    public class EndlessConfigData : ScriptableObject
    {
        const int MAX_TARGET_SCORE = 1000000000;
        const int FIBONACCI_SUM_OFFSET = 2;
        const int FIRST_FIBONACCI_INDEX = 1;

        [SerializeField, Tooltip("무한 스테이지마다 목표 점수에 더해지는 증가량. 기준 점수 대비 비율이며 피보나치 수열 배로 누적된다")]
        float _stepRatio = 0.2f;
        [SerializeField, Tooltip("무한 라운드의 마지막 스테이지에 확정 배치할 기믹. 목록을 넘어선 라운드는 마지막 항목을 계속 사용한다")]
        StageGimmickData[] _roundLastGimmicks = null;

        public int GetTargetScore(int baseTargetScore, int endlessStageNumber)
        {
            if (baseTargetScore <= 0 || endlessStageNumber <= 0)
                return baseTargetScore;

            double stepCount = GetFibonacci(endlessStageNumber + FIBONACCI_SUM_OFFSET) - 1.0;
            double target = baseTargetScore * (1.0 + _stepRatio * stepCount);
            if (target >= MAX_TARGET_SCORE)
                return MAX_TARGET_SCORE;

            return (int)System.Math.Round(target);
        }

        public StageGimmickData GetRoundLastGimmick(int endlessStageNumber)
        {
            int gimmickCount = null == _roundLastGimmicks ? 0 : _roundLastGimmicks.Length;
            if (endlessStageNumber <= 0 || gimmickCount <= 0)
                return null;

            if (endlessStageNumber % StageCatalogData.STAGES_PER_ROUND != 0)
                return null;

            int roundIndex = (endlessStageNumber - 1) / StageCatalogData.STAGES_PER_ROUND;
            return _roundLastGimmicks[Mathf.Min(roundIndex, gimmickCount - 1)];
        }

        static double GetFibonacci(int index)
        {
            double previous = 0.0;
            double current = 1.0;
            for (int i = FIRST_FIBONACCI_INDEX; i < index; ++i)
            {
                double next = previous + current;
                previous = current;
                current = next;
                if (current >= MAX_TARGET_SCORE)
                    break;
            }

            return current;
        }
    }
}
