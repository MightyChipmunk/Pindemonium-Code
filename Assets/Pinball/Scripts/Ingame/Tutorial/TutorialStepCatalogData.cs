using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pinball.Ingame.Tutorial
{
    [CreateAssetMenu(fileName = "TutorialStepCatalogData", menuName = "Pinball/Ingame/TutorialStepCatalogData")]
    public class TutorialStepCatalogData : ScriptableObject
    {
        public enum EStep
        {
            PlayWelcome = 0,
            PlayStageSelectCard = 1,
            PlayStageSelectStart = 2,
            PlayScore = 3,
            PlayGimmickBumper = 4,
            PlayGimmickSlingshot = 5,
            PlayGimmickSpinner = 6,
            PlayGimmickDropTarget = 7,
            PlayGimmickStandupTarget = 8,
            PlayGimmickLampBank = 9,
            PlayGimmickLane = 10,
            PlayGimmickSaucer = 11,
            PlaySpring = 12,
            PlayLaunchGuide = 13,
            PlayFlipper = 14,
            PlayParry = 15,
            PlayNudge = 16,
            PlayNudgeTry = 17,
            PlayBallLost = 18,
            PlayBallLostNotice = 19,
            PlayCombo = 20,
            PlayFreePlay = 21,
            ShopOffers = 22,
            ShopPassiveLimit = 23,
            ShopPacks = 24,
            ShopVoucher = 25,
            ShopReroll = 26,
            ShopInterest = 27,
            ShopStart = 28,
            StageSelectGimmick = 29,
            StageSelectReroll = 30,
        }

        public enum EHighlight
        {
            None = 0,
            FullScreen = 1,
            IngameScore = 2,
            IngameCombo = 3,
            WorldSpring = 4,
            WorldFlipper = 5,
            WorldBumper = 6,
            WorldSlingshot = 7,
            WorldSpinner = 8,
            WorldDropTarget = 9,
            WorldStandupTarget = 10,
            WorldLampBank = 11,
            WorldLane = 12,
            WorldSaucer = 13,
            StageSelectCard = 14,
            StageSelectStart = 15,
            StageSelectGimmick = 16,
            StageSelectReroll = 17,
            ShopOffers = 18,
            ShopPacks = 19,
            ShopVoucher = 20,
            ShopReroll = 21,
            ShopMoney = 22,
            ShopStart = 23,
        }

        [Serializable]
        public struct Step
        {
            public EStep Id;
            public string Key;
            public Sprite Mascot;
            public EHighlight Highlight;
            public TutorialDialogView.EPlacement Placement;
        }

        static readonly int STEP_COUNT = Enum.GetValues(typeof(EStep)).Length;

        [SerializeField]
        List<Step> _steps = new List<Step>();

        public static int StepCount => STEP_COUNT;

        public Step GetStep(EStep id)
        {
            for (int i = 0, count = null == _steps ? 0 : _steps.Count; i < count; ++i)
            {
                if (_steps[i].Id != id)
                    continue;

                return _steps[i];
            }

            Step fallback = new Step();
            fallback.Id = id;
            fallback.Placement = TutorialDialogView.EPlacement.Bottom;
            return fallback;
        }
    }
}
