using GameCore.Audio;
using UnityEngine;

namespace Pinball.Audio
{
    [CreateAssetMenu(fileName = "AudioStorage", menuName = "Pinball/Playable/Sound/AudioStorage")]
    public class AudioStorage : AudioStorageBase<AudioStorage.EAudioClipType>
    {
        public enum EAudioClipType
        {
            None = -1,

            UIButtonClick = 0,
            UIButtonHover = 1,

            FlipperHitNormal = 10,
            FlipperHitSweetSpot = 11,
            FlipperHitPerfect = 12,
            FlipperSwing = 13,

            GimmickHitBumper = 20,
            GimmickHitSlingshot = 21,
            GimmickHitSpinner = 22,
            GimmickHitDropTarget = 23,
            GimmickHitStandupTarget = 24,
            GimmickHitLampBank = 25,
            GimmickHitLane = 26,
            GimmickSaucerCapture = 27,
            GimmickBankCleared = 28,
            BumperBreak = 29,
            GimmickSaucerSpinTick = 30,
            GimmickSaucerEject = 31,

            SpringChargeStart = 40,
            SpringChargePeak = 41,
            SpringChargeCancel = 42,
            SpringLaunch = 43,
            SpringChargeLoop = 44,

            BallHitNormal = 50,
            BallCloneSpawn = 56,
            BallLost = 57,
            BallReady = 58,

            EffectChainLightning = 70,
            EffectBurn = 71,
            EffectFreeze = 72,
            EffectFrostShatter = 73,
            EffectPowerSurgeCharged = 74,
            EffectNudge = 75,
            EffectSniperMark = 76,
            EffectSniperHit = 77,
            EffectSniperExpire = 78,
            EffectBlast = 79,
            EffectDropOrderStep = 80,
            EffectVoidRift = 81,

            MoneyGain = 90,
            ComboUp = 91,

            ShopBuy = 100,
            ShopReroll = 101,
            ShopDenied = 102,

            StageClear = 110,
            GameClear = 111,
            GameOver = 112,
            RewardUnlock = 113,
        }

        protected override EAudioClipType GetNoneKey()
        {
            return EAudioClipType.None;
        }
    }
}
