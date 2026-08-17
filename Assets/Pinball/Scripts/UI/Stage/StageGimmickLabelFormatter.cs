using Pinball.Ingame.Map;
using Pinball.Ingame.Stage;
using UnityEngine;

namespace Pinball.UI
{
    public static class StageGimmickLabelFormatter
    {
        const string BUMPER_UNBREAKABLE_LABEL = "BUMPER UNBREAKABLE";
        const string TARGET_SCORE_FORMAT = "TARGET SCORE x{0:0.#}";
        const string TIME_LIMIT_FORMAT = "TIME LIMIT {0}:{1:00}";
        const string ABSENT_FORMAT = "NO {0}";
        const string SCORE_MULTIPLIER_FORMAT = "{0} SCORE x{1:0.#}";
        const string GAIN_LIMIT_FORMAT = "{0} LIMIT {1} HITS";
        const int SECONDS_PER_MINUTE = 60;

        public static string Format(StageGimmickData gimmick)
        {
            if (null == gimmick)
                return string.Empty;

            switch (gimmick.Type)
            {
                case StageGimmickData.EType.BumperUnbreakable:
                    return BUMPER_UNBREAKABLE_LABEL;
                case StageGimmickData.EType.TargetScoreMultiplier:
                    return string.Format(TARGET_SCORE_FORMAT, gimmick.Value);
                case StageGimmickData.EType.TimeLimit:
                    return FormatTimeLimit(gimmick.Value);
                case StageGimmickData.EType.GimmickAbsent:
                    return string.Format(ABSENT_FORMAT, GetGimmickName(gimmick.TargetGimmickType));
                case StageGimmickData.EType.GimmickScoreMultiplier:
                    return string.Format(SCORE_MULTIPLIER_FORMAT, GetGimmickName(gimmick.TargetGimmickType), gimmick.Value);
                case StageGimmickData.EType.GimmickScoreGainLimit:
                    return string.Format(GAIN_LIMIT_FORMAT, GetGimmickName(gimmick.TargetGimmickType), Mathf.RoundToInt(gimmick.Value));
                default:
                    return gimmick.Type.ToString().ToUpperInvariant();
            }
        }

        static string FormatTimeLimit(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return string.Format(TIME_LIMIT_FORMAT, totalSeconds / SECONDS_PER_MINUTE, totalSeconds % SECONDS_PER_MINUTE);
        }

        static string GetGimmickName(MapInteractable.EGimmickType gimmickType)
        {
            switch (gimmickType)
            {
                case MapInteractable.EGimmickType.Bumper:
                    return "BUMPER";
                case MapInteractable.EGimmickType.Slingshot:
                    return "SLINGSHOT";
                case MapInteractable.EGimmickType.Spinner:
                    return "SPINNER";
                case MapInteractable.EGimmickType.DropTarget:
                    return "DROP TARGET";
                case MapInteractable.EGimmickType.StandupTarget:
                    return "STANDUP";
                case MapInteractable.EGimmickType.LampBank:
                    return "LAMP BANK";
                case MapInteractable.EGimmickType.Lane:
                    return "LANE";
                default:
                    return gimmickType.ToString().ToUpperInvariant();
            }
        }
    }
}
