using Pinball.Ingame.Map;

namespace Pinball.Ingame.Stage
{
    public static class StageGimmickBlacklist
    {
        public static bool IsConflict(StageGimmickData lhs, StageGimmickData rhs)
        {
            if (null == lhs || null == rhs)
                return false;

            if (lhs == rhs)
                return true;

            if (IsSameEffectSlot(lhs, rhs))
                return true;

            if (IsAbsentConflict(lhs, rhs))
                return true;

            if (IsAbsentConflict(rhs, lhs))
                return true;

            return false;
        }

        static bool IsSameEffectSlot(StageGimmickData lhs, StageGimmickData rhs)
        {
            if (lhs.Type != rhs.Type)
                return false;

            if (!UsesTargetGimmickType(lhs.Type))
                return true;

            return lhs.TargetGimmickType == rhs.TargetGimmickType;
        }

        static bool IsAbsentConflict(StageGimmickData absent, StageGimmickData other)
        {
            if (absent.Type != StageGimmickData.EType.GimmickAbsent)
                return false;

            if (other.Type == StageGimmickData.EType.BumperUnbreakable)
                return absent.TargetGimmickType == MapInteractable.EGimmickType.Bumper;

            if (!UsesTargetGimmickType(other.Type))
                return false;

            return absent.TargetGimmickType == other.TargetGimmickType;
        }

        static bool UsesTargetGimmickType(StageGimmickData.EType type)
        {
            return type == StageGimmickData.EType.GimmickAbsent
                || type == StageGimmickData.EType.GimmickScoreMultiplier
                || type == StageGimmickData.EType.GimmickScoreGainLimit;
        }
    }
}
