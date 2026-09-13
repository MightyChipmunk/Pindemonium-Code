using GameCore.Util;

namespace Pinball.Ingame.Presentation
{
    /// <summary>
    /// 게임의 카메라 흔들림 종류별 지속 시간·진폭 프리셋.
    /// </summary>
    public static class CameraShakePresets
    {
        const float NORMAL_DURATION = 0.3f;
        const float SOFT_DURATION = 0.18f;
        const float SWEET_SPOT_DURATION = 0.18f;
        const float HARD_DURATION = 0.3f;
        const float NUDGE_DURATION = 0.18f;
        const float NORMAL_OFFSET = 0.03f;
        const float SOFT_OFFSET = 0.018f;
        const float SWEET_SPOT_OFFSET = 0.045f;
        const float HARD_OFFSET = 0.045f;
        const float NUDGE_OFFSET = 0.09f;

        public static CameraShakeSpec Get(ECameraShakeType shakeType)
        {
            switch (shakeType)
            {
                case ECameraShakeType.Soft:
                    return new CameraShakeSpec(SOFT_DURATION, SOFT_OFFSET);
                case ECameraShakeType.SweetSpot:
                    return new CameraShakeSpec(SWEET_SPOT_DURATION, SWEET_SPOT_OFFSET);
                case ECameraShakeType.Hard:
                    return new CameraShakeSpec(HARD_DURATION, HARD_OFFSET);
                case ECameraShakeType.Nudge:
                    return new CameraShakeSpec(NUDGE_DURATION, NUDGE_OFFSET);
                default:
                    return new CameraShakeSpec(NORMAL_DURATION, NORMAL_OFFSET);
            }
        }
    }
}
