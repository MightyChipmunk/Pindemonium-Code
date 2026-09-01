using UnityEngine;

namespace Pinball.Util
{
    public static class ScoreMath
    {
        const float MAX_HIT_SCORE = 2000000000f;

        public static int RoundToScore(float value)
        {
            return Mathf.RoundToInt(Mathf.Min(value, MAX_HIT_SCORE));
        }
    }
}
