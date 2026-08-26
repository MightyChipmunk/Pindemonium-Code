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
        }

        const string PLAY_INTRO_ID = "play_intro";
        const string SHOP_ID = "shop";
        const string STAGE_GIMMICK_ID = "stage_gimmick";

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
                default:
                    return STAGE_GIMMICK_ID;
            }
        }
    }
}
