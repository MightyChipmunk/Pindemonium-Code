using Pinball.Ingame.Map;
using UnityEngine;

namespace Pinball.Ingame.Stage
{
    public class StageGimmickResolver
    {
        public const float DEFAULT_TARGET_SCORE_MULTIPLIER = 1f;

        const float DEFAULT_SCORE_MULTIPLIER = 1f;
        const float NO_TIME_LIMIT = 0f;

        IStageGimmickSource _source = null;

        public StageGimmickResolver(IStageGimmickSource source)
        {
            _source = source;
        }

        public bool IsBumperUnbreakable()
        {
            return null != FindGimmick(StageGimmickData.EType.BumperUnbreakable);
        }

        public int GetTargetScore(int baseTargetScore)
        {
            return Mathf.RoundToInt(baseTargetScore * GetTargetScoreMultiplier());
        }

        public float GetTargetScoreMultiplier()
        {
            StageGimmickData gimmick = FindGimmick(StageGimmickData.EType.TargetScoreMultiplier);
            if (null == gimmick)
                return DEFAULT_TARGET_SCORE_MULTIPLIER;

            return gimmick.Value;
        }

        public float GetTimeLimit()
        {
            StageGimmickData gimmick = FindGimmick(StageGimmickData.EType.TimeLimit);
            if (null == gimmick)
                return NO_TIME_LIMIT;

            return Mathf.Max(NO_TIME_LIMIT, gimmick.Value);
        }

        public bool IsGimmickAbsent(MapInteractable.EGimmickType gimmickType)
        {
            return null != FindGimmick(StageGimmickData.EType.GimmickAbsent, gimmickType);
        }

        public float GetGimmickScoreMultiplier(MapInteractable.EGimmickType gimmickType)
        {
            StageGimmickData gimmick = FindGimmick(StageGimmickData.EType.GimmickScoreMultiplier, gimmickType);
            if (null == gimmick)
                return DEFAULT_SCORE_MULTIPLIER;

            return Mathf.Max(0f, gimmick.Value);
        }

        public int GetGimmickScoreGainLimit(MapInteractable.EGimmickType gimmickType)
        {
            StageGimmickData gimmick = FindGimmick(StageGimmickData.EType.GimmickScoreGainLimit, gimmickType);
            if (null == gimmick)
                return MapInteractable.NO_SCORE_GAIN_LIMIT;

            return Mathf.Max(MapInteractable.NO_SCORE_GAIN_LIMIT, Mathf.RoundToInt(gimmick.Value));
        }

        StageGimmickData FindGimmick(StageGimmickData.EType type)
        {
            if (null == _source)
                return null;

            for (int i = 0; i < _source.GimmickCount; ++i)
            {
                StageGimmickData gimmick = _source.GetGimmick(i);
                if (null == gimmick)
                    continue;

                if (gimmick.Type == type)
                    return gimmick;
            }

            return null;
        }

        StageGimmickData FindGimmick(StageGimmickData.EType type, MapInteractable.EGimmickType targetGimmickType)
        {
            if (null == _source)
                return null;

            for (int i = 0; i < _source.GimmickCount; ++i)
            {
                StageGimmickData gimmick = _source.GetGimmick(i);
                if (null == gimmick)
                    continue;

                if (gimmick.Type != type)
                    continue;

                if (gimmick.TargetGimmickType == targetGimmickType)
                    return gimmick;
            }

            return null;
        }
    }
}
