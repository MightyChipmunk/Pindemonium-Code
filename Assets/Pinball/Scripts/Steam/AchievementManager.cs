using System.Threading;
using Cysharp.Threading.Tasks;
using GameCore.Steam;
using Pinball.Save;

namespace Pinball.Steam
{
    /// <summary>
    /// 게임 조건을 Steam 도전과제 API Name에 매핑하는 파사드. 같은 프레임에 여러 개가 해금돼도 StoreStats는 다음 프레임에 한 번만 호출한다.
    /// GameBootStrap이 세이브 초기화 직후 Init하며, Init 시 세이브 기준으로 이미 달성한 마일스톤을 소급 해금한다.
    /// Steam이 초기화되지 않은 환경(공유 빌드, Ingame 씬 직접 실행)에서는 전부 no-op이다.
    /// </summary>
    public static class AchievementManager
    {
        public enum EAchievement
        {
            UpgradeX5 = 0,
            UpgradeX10 = 1,
            PassiveFull = 2,
            GameClear = 3,
            EndlessClear = 4,
            Money50 = 5,
            VoucherX4 = 6,
            LastBallClear = 7,
            OneBallClear = 8,
        }

        const int UPGRADE_X5_COUNT = 5;
        const int UPGRADE_X10_COUNT = 10;
        const int MONEY_50_AMOUNT = 50;
        const int VOUCHER_X4_COUNT = 4;
        const int LAST_BALL_REMAINING = 1;
        const int ONE_BALL_USED = 0;

        static readonly string[] API_NAME_ARR =
        {
            "ACH_UPGRADE_X5",
            "ACH_UPGRADE_X10",
            "ACH_PASSIVE_FULL",
            "ACH_GAME_CLEAR",
            "ACH_ENDLESS_CLEAR",
            "ACH_MONEY_50",
            "ACH_VOUCHER_X4",
            "ACH_LAST_BALL_CLEAR",
            "ACH_ONE_BALL_CLEAR",
        };

        static CancellationTokenSource _flushCts = null;
        static bool _isFlushScheduled = false;
        static bool _isInitialized = false;

        public static bool IsAvailable => SteamAchievements.IsInitialized;

        public static void Init()
        {
            if (_isInitialized)
                return;

            SteamAchievements.Init();
            _flushCts = new CancellationTokenSource();
            _isInitialized = true;
            UnlockEarnedMilestones();
        }

        public static void Release()
        {
            if (!_isInitialized)
                return;

            _isInitialized = false;
            _flushCts.Cancel();
            _flushCts.Dispose();
            _flushCts = null;
            _isFlushScheduled = false;
            SteamAchievements.Store();
            SteamAchievements.Release();
        }

        public static string GetApiName(EAchievement achievement)
        {
            return API_NAME_ARR[(int)achievement];
        }

        public static bool IsUnlocked(EAchievement achievement)
        {
            return SteamAchievements.IsUnlocked(GetApiName(achievement));
        }

        public static void Unlock(EAchievement achievement)
        {
            if (!_isInitialized)
                return;

            if (!SteamAchievements.TryUnlock(GetApiName(achievement)))
                return;

            ScheduleFlush();
        }

        public static void NotifyUpgradeCount(int maxUpgradeCount)
        {
            if (maxUpgradeCount >= UPGRADE_X5_COUNT)
                Unlock(EAchievement.UpgradeX5);

            if (maxUpgradeCount >= UPGRADE_X10_COUNT)
                Unlock(EAchievement.UpgradeX10);
        }

        public static void NotifyPassiveSlots(int passiveCount, int totalSlotCount)
        {
            if (totalSlotCount > 0 && passiveCount >= totalSlotCount)
                Unlock(EAchievement.PassiveFull);
        }

        public static void NotifyMoney(int money)
        {
            if (money >= MONEY_50_AMOUNT)
                Unlock(EAchievement.Money50);
        }

        public static void NotifyVoucherCount(int ownedCount)
        {
            if (ownedCount >= VOUCHER_X4_COUNT)
                Unlock(EAchievement.VoucherX4);
        }

        public static void NotifyStageCleared(bool isEndless, int remainingBalls, int usedBallsInStage)
        {
            if (isEndless)
                Unlock(EAchievement.EndlessClear);

            if (remainingBalls == LAST_BALL_REMAINING)
                Unlock(EAchievement.LastBallClear);

            if (usedBallsInStage == ONE_BALL_USED)
                Unlock(EAchievement.OneBallClear);
        }

        public static void NotifyGameCleared()
        {
            Unlock(EAchievement.GameClear);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static void Clear(EAchievement achievement)
        {
            if (!_isInitialized)
                return;

            if (!SteamAchievements.TryClear(GetApiName(achievement)))
                return;

            ScheduleFlush();
        }

        public static void ResetAll()
        {
            if (!_isInitialized)
                return;

            SteamAchievements.ResetAll();
        }
#endif

        static void UnlockEarnedMilestones()
        {
            if (SaveManager.ClearCount > 0)
                Unlock(EAchievement.GameClear);

            if (SaveManager.HasEndlessClear())
                Unlock(EAchievement.EndlessClear);
        }

        static void ScheduleFlush()
        {
            if (_isFlushScheduled)
                return;

            _isFlushScheduled = true;
            FlushAsync(_flushCts.Token).Forget();
        }

        static async UniTaskVoid FlushAsync(CancellationToken cancellationToken)
        {
            await UniTask.NextFrame(cancellationToken);
            _isFlushScheduled = false;
            SteamAchievements.Store();
        }
    }
}
