using System.Collections.Generic;
using GameCore.Save;
using GameCore.Steam;

namespace Pinball.Save
{
    /// <summary>
    /// 게임 세이브(유저 진행·런 이어하기)의 파일 이름과 타입을 정하고 GameCore.SaveStore로 읽고 쓴다.
    /// </summary>
    public static class SaveManager
    {
        const int CURRENT_SAVE_VERSION = 1;
        const string USER_FILE_NAME = "pinball_user.json";
        const string INGAME_FILE_NAME = "pinball_ingame.json";

        static UserData _userData = null;
        static IngamePlayData _ingameData = null;

        public static bool IsInitialized => SaveStore.IsInitialized;
        public static bool HasIngameSave => null != _ingameData;

        public static void EnsureInit()
        {
            if (IsInitialized)
                return;

            Init();
        }

        public static void Init()
        {
            SaveStore.Init(CURRENT_SAVE_VERSION, CreateStorages());

            _userData = SaveStore.ReadNewest<UserData>(USER_FILE_NAME);
            if (null == _userData)
                _userData = new UserData();

            _ingameData = SaveStore.ReadNewest<IngamePlayData>(INGAME_FILE_NAME);
        }

        static List<ISaveStorage> CreateStorages()
        {
            List<ISaveStorage> storages = new List<ISaveStorage>();
            if (SteamCloudStorage.IsAvailable())
                storages.Add(new SteamCloudStorage());

            storages.Add(new LocalFileStorage());
            return storages;
        }

        public static void Release()
        {
            SaveStore.Release();
            _userData = null;
            _ingameData = null;
        }

        public static int ClearCount => null == _userData ? 0 : _userData.ClearCount;
        public static int PendingRewardTier => null == _userData ? UserData.NO_PENDING_REWARD_TIER : _userData.PendingRewardTier;

        public static bool IsFlipperUnlocked(string flipperId)
        {
            return null != _userData && _userData.IsFlipperUnlocked(flipperId);
        }

        public static bool IsItemUnlocked(string itemId)
        {
            return null != _userData && _userData.IsItemUnlocked(itemId);
        }

        public static UserData.FlipperBestEntry GetFlipperBest(string flipperId)
        {
            return null == _userData ? default : _userData.GetFlipperBest(flipperId);
        }

        public static void UpdateBestStage(string flipperId, int stageNumber)
        {
            if (null == _userData)
                return;

            if (!_userData.TryUpdateBestStage(flipperId, stageNumber))
                return;

            SaveUser();
        }

        public static void UpdateBestClear(string flipperId)
        {
            if (null == _userData)
                return;

            if (!_userData.TryUpdateBestClear(flipperId))
                return;

            SaveUser();
        }

        public static void UpdateBestEndlessStage(string flipperId, int endlessStageNumber)
        {
            if (null == _userData)
                return;

            if (!_userData.TryUpdateBestEndlessStage(flipperId, endlessStageNumber))
                return;

            SaveUser();
        }

        public static bool IsTutorialCompleted(string tutorialId)
        {
            return null != _userData && _userData.IsTutorialCompleted(tutorialId);
        }

        public static void CompleteTutorial(string tutorialId)
        {
            if (null == _userData)
                return;

            if (!_userData.TryCompleteTutorial(tutorialId))
                return;

            SaveUser();
        }

        public static void ResetTutorials()
        {
            if (null == _userData)
                return;

            if (!_userData.TryResetTutorials())
                return;

            SaveUser();
        }

        public static void RegisterRunClear(IReadOnlyList<string> unlockedFlipperIds, IReadOnlyList<string> unlockedItemIds, int pendingRewardTier)
        {
            if (null == _userData)
                return;

            _userData.RegisterClear(unlockedFlipperIds, unlockedItemIds, pendingRewardTier);
            SaveUser();
        }

        public static void ConsumePendingReward()
        {
            if (null == _userData)
                return;

            if (!_userData.HasPendingReward)
                return;

            _userData.ClearPendingReward();
            SaveUser();
        }

        public static IngamePlayData GetIngameData()
        {
            return _ingameData;
        }

        static void SaveUser()
        {
            if (null == _userData)
                return;

            SaveStore.WriteAll(USER_FILE_NAME, _userData);
        }

        public static void SaveIngame(IngamePlayData data)
        {
            if (null == data)
                return;

            _ingameData = data;
            SaveStore.WriteAll(INGAME_FILE_NAME, data);
        }

        public static void ClearIngame()
        {
            _ingameData = null;
            SaveStore.DeleteAll(INGAME_FILE_NAME);
        }
    }
}
