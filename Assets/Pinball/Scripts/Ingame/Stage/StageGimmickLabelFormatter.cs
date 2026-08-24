using Pinball.Ingame.Map;
using Pinball.Localization;
using UnityEngine;

namespace Pinball.Ingame.Stage
{
    public static class StageGimmickLabelFormatter
    {
        const string TARGET_SCORE_FORMAT = "TARGET SCORE x{0:0.#}";
        const string TARGET_SCORE_KEY = "gimmick.target_score";
        const string TIME_LIMIT_FORMAT = "TIME LIMIT {0}:{1:00}";
        const string TIME_LIMIT_KEY = "gimmick.time_limit";
        const string ABSENT_FORMAT = "NO {0}";
        const string ABSENT_KEY = "gimmick.absent";
        const string SCORE_MULTIPLIER_FORMAT = "{0} SCORE x{1:0.#}";
        const string SCORE_MULTIPLIER_KEY = "gimmick.score_multiplier";
        const string GAIN_LIMIT_FORMAT = "{0} LIMIT {1} HITS";
        const string GAIN_LIMIT_KEY = "gimmick.gain_limit";
        const string BUMPER_NAME = "BUMPER";
        const string BUMPER_NAME_KEY = "gimmick.name.bumper";
        const string SLINGSHOT_NAME = "SLINGSHOT";
        const string SLINGSHOT_NAME_KEY = "gimmick.name.slingshot";
        const string SPINNER_NAME = "SPINNER";
        const string SPINNER_NAME_KEY = "gimmick.name.spinner";
        const string DROP_TARGET_NAME = "DROP TARGET";
        const string DROP_TARGET_NAME_KEY = "gimmick.name.drop_target";
        const string STANDUP_NAME = "STANDUP";
        const string STANDUP_NAME_KEY = "gimmick.name.standup";
        const string LAMP_BANK_NAME = "LAMP BANK";
        const string LAMP_BANK_NAME_KEY = "gimmick.name.lamp_bank";
        const string LANE_NAME = "LANE";
        const string LANE_NAME_KEY = "gimmick.name.lane";
        const string SAUCER_NAME = "SAUCER";
        const string SAUCER_NAME_KEY = "gimmick.name.saucer";
        const int SECONDS_PER_MINUTE = 60;

        public static string Format(StageGimmickData gimmick)
        {
            if (null == gimmick)
                return string.Empty;

            switch (gimmick.Type)
            {
                case StageGimmickData.EType.TargetScoreMultiplier:
                    return LocalizationTable.Format(TARGET_SCORE_KEY, TARGET_SCORE_FORMAT, gimmick.Value);
                case StageGimmickData.EType.TimeLimit:
                    return FormatTimeLimit(gimmick.Value);
                case StageGimmickData.EType.GimmickAbsent:
                    return LocalizationTable.Format(ABSENT_KEY, ABSENT_FORMAT, GetGimmickName(gimmick.TargetGimmickType));
                case StageGimmickData.EType.GimmickScoreMultiplier:
                    return LocalizationTable.Format(SCORE_MULTIPLIER_KEY, SCORE_MULTIPLIER_FORMAT, GetGimmickName(gimmick.TargetGimmickType), gimmick.Value);
                case StageGimmickData.EType.GimmickScoreGainLimit:
                    return LocalizationTable.Format(GAIN_LIMIT_KEY, GAIN_LIMIT_FORMAT, GetGimmickName(gimmick.TargetGimmickType), Mathf.RoundToInt(gimmick.Value));
                default:
                    return gimmick.Type.ToString().ToUpperInvariant();
            }
        }

        static string FormatTimeLimit(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return LocalizationTable.Format(TIME_LIMIT_KEY, TIME_LIMIT_FORMAT, totalSeconds / SECONDS_PER_MINUTE, totalSeconds % SECONDS_PER_MINUTE);
        }

        static string GetGimmickName(MapInteractable.EGimmickType gimmickType)
        {
            switch (gimmickType)
            {
                case MapInteractable.EGimmickType.Bumper:
                    return LocalizationTable.Get(BUMPER_NAME_KEY, BUMPER_NAME);
                case MapInteractable.EGimmickType.Slingshot:
                    return LocalizationTable.Get(SLINGSHOT_NAME_KEY, SLINGSHOT_NAME);
                case MapInteractable.EGimmickType.Spinner:
                    return LocalizationTable.Get(SPINNER_NAME_KEY, SPINNER_NAME);
                case MapInteractable.EGimmickType.DropTarget:
                    return LocalizationTable.Get(DROP_TARGET_NAME_KEY, DROP_TARGET_NAME);
                case MapInteractable.EGimmickType.StandupTarget:
                    return LocalizationTable.Get(STANDUP_NAME_KEY, STANDUP_NAME);
                case MapInteractable.EGimmickType.LampBank:
                    return LocalizationTable.Get(LAMP_BANK_NAME_KEY, LAMP_BANK_NAME);
                case MapInteractable.EGimmickType.Lane:
                    return LocalizationTable.Get(LANE_NAME_KEY, LANE_NAME);
                case MapInteractable.EGimmickType.Saucer:
                    return LocalizationTable.Get(SAUCER_NAME_KEY, SAUCER_NAME);
                default:
                    return gimmickType.ToString().ToUpperInvariant();
            }
        }
    }
}
