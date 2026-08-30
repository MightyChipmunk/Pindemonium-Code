using Pinball.Save;

namespace Pinball.Ingame.Tutorial
{
    public class TutorialProgress
    {
        public enum EBlock
        {
            PlayIntro = 0,
            Shop = 1,
            StageGimmick = 2,
            BallLost = 3,
        }

        const string PLAY_INTRO_ID = "play_intro";
        const string SHOP_ID = "shop";
        const string STAGE_GIMMICK_ID = "stage_gimmick";
        const string BALL_LOST_ID = "ball_lost";

        public bool IsCompleted(EBlock block)
        {
            return SaveManager.IsTutorialCompleted(ToId(block));
        }

        public void Complete(EBlock block)
        {
            SaveManager.CompleteTutorial(ToId(block));
        }

        public void CompleteAll()
        {
            SaveManager.CompleteTutorial(PLAY_INTRO_ID);
            SaveManager.CompleteTutorial(SHOP_ID);
            SaveManager.CompleteTutorial(STAGE_GIMMICK_ID);
            SaveManager.CompleteTutorial(BALL_LOST_ID);
        }

        public void ResetAll()
        {
            SaveManager.ResetTutorials();
        }

        static string ToId(EBlock block)
        {
            switch (block)
            {
                case EBlock.PlayIntro:
                    return PLAY_INTRO_ID;
                case EBlock.Shop:
                    return SHOP_ID;
                case EBlock.StageGimmick:
                    return STAGE_GIMMICK_ID;
                default:
                    return BALL_LOST_ID;
            }
        }
    }
}
