using System;
using Pinball.Ingame.Map;
using UnityEngine;

namespace Pinball.Ingame.Score
{
    public class GimmickScoreTracker
    {
        readonly int[] _scores = null;

        public GimmickScoreTracker(int[] restoredScores)
        {
            _scores = new int[Enum.GetValues(typeof(MapInteractable.EGimmickType)).Length];
            if (null == restoredScores)
                return;

            int restoredCount = Mathf.Min(restoredScores.Length, _scores.Length);
            for (int i = 0; i < restoredCount; ++i)
                _scores[i] = restoredScores[i];
        }

        public void Add(MapInteractable.EGimmickType gimmickType, int score)
        {
            int index = (int)gimmickType;
            if (index < 0 || index >= _scores.Length)
                return;

            _scores[index] += score;
        }

        public int[] CollectScores()
        {
            int[] scores = new int[_scores.Length];
            Array.Copy(_scores, scores, _scores.Length);
            return scores;
        }
    }
}
