using UnityEngine;

namespace Pinball.Ingame.Stage
{
    [CreateAssetMenu(fileName = "EndlessConfigData", menuName = "Pinball/Ingame/EndlessConfigData")]
    public class EndlessConfigData : ScriptableObject
    {
        const int MAX_TARGET_SCORE = 1000000000;
        const int SIGNIFICANT_DIGITS = 2;

        [SerializeField, Tooltip("무한 1스테이지 목표 점수 배율")] float _growthBase = 1.6f;
        [SerializeField, Tooltip("무한 스테이지가 오를 때마다 배율에 더해지는 증가량")] float _growthRamp = 0.25f;

        public int GetTargetScore(int baseTargetScore, int endlessStageNumber)
        {
            if (baseTargetScore <= 0 || endlessStageNumber <= 0)
                return baseTargetScore;

            double rate = _growthBase + _growthRamp * (endlessStageNumber - 1);
            double target = baseTargetScore * System.Math.Pow(rate, endlessStageNumber);
            if (target >= MAX_TARGET_SCORE)
                return MAX_TARGET_SCORE;

            return FloorToSignificantDigits(target);
        }

        static int FloorToSignificantDigits(double value)
        {
            double magnitude = System.Math.Pow(10.0, System.Math.Floor(System.Math.Log10(value)) - (SIGNIFICANT_DIGITS - 1));
            if (magnitude < 1.0)
                return (int)value;

            return (int)(System.Math.Floor(value / magnitude) * magnitude);
        }
    }
}
