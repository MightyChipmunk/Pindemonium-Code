using System;
using Pinball.Audio;
using Pinball.Ingame;
using Pinball.Ingame.Reward;
using Pinball.Ingame.Stage;
using Pinball.Save;
using Pinball.UI;

namespace Pinball.Lobby
{
    public class LobbyRunSetupController : IDisposable
    {
        public record InitParam(
            FlipperCatalogData FlipperCatalog,
            ClearRewardCatalogData RewardCatalog,
            StageCatalogData StageCatalog,
            StageGimmickCatalogData GimmickCatalog);

        FlipperCatalogData _flipperCatalog = null;
        StageCatalogData _stageCatalog = null;
        StageGimmickCatalogData _gimmickCatalog = null;
        UnlockProgress _unlockProgress = null;

        public event Action<int> OnPlayRequested;
        public event Action OnContinueRequested;

        public LobbyRunSetupController(InitParam param)
        {
            _flipperCatalog = param.FlipperCatalog;
            _stageCatalog = param.StageCatalog;
            _gimmickCatalog = param.GimmickCatalog;
            _unlockProgress = new UnlockProgress(param.RewardCatalog);
            UIManager.OnEvent += HandleUIEvent;
        }

        public void Dispose()
        {
            UIManager.OnEvent -= HandleUIEvent;
            UIManager.Hide(UIViewCatalogData.EViewType.FlipperSelect);
            UIManager.Hide(UIViewCatalogData.EViewType.ContinueRun);
            UIManager.Hide(UIViewCatalogData.EViewType.RewardUnlock);
            OnPlayRequested = null;
            OnContinueRequested = null;
        }

        public void ShowPendingClearReward()
        {
            if (!ClearRewardUnlockPresenter.TryShow(_unlockProgress.ConsumePendingReward()))
                return;

            AudioManager.Play(AudioStorage.EAudioClipType.RewardUnlock);
        }

        public void OpenFlipperSelect()
        {
            int flipperCount = null == _flipperCatalog ? 0 : _flipperCatalog.Count;
            FlipperSelectView.FlipperEntry[] entries = new FlipperSelectView.FlipperEntry[flipperCount];
            for (int i = 0; i < flipperCount; ++i)
                entries[i] = BuildFlipperEntry(_flipperCatalog.GetFlipper(i));

            UIManager.Show(new FlipperSelectView.Args(entries));
        }

        FlipperSelectView.FlipperEntry BuildFlipperEntry(FlipperData flipper)
        {
            if (null == flipper)
                return null;

            UserData.FlipperBestEntry best = SaveManager.GetFlipperBest(flipper.GetId());
            return new FlipperSelectView.FlipperEntry(
                NameKey: flipper.GetNameKey(),
                Icon: flipper.GetIcon(),
                AbilityKey: flipper.GetAbilityKey(),
                DrawbackKey: flipper.GetDrawbackKey(),
                IsUnlocked: _unlockProgress.IsFlipperUnlocked(flipper),
                UnlockTier: _unlockProgress.GetFlipperUnlockTier(flipper),
                BestStageNumber: best.StageNumber,
                IsClearRecorded: best.IsCleared,
                BestEndlessStageNumber: best.EndlessStageNumber);
        }

        public void OpenContinue()
        {
            IngamePlayData saved = SaveManager.GetIngameData();
            if (null == saved)
                return;

            FlipperData flipper = ResolveSavedFlipper(saved.FlipperId);
            int stageIndex = saved.StageNumber - 1;
            bool isEndless = saved.IsEndless && null != _stageCatalog && _stageCatalog.IsEndlessStageIndex(stageIndex);
            UIManager.Show(new ContinueRunView.Args(
                FlipperNameKey: null == flipper ? string.Empty : flipper.GetNameKey(),
                FlipperIcon: null == flipper ? null : flipper.GetIcon(),
                StageNumber: isEndless ? _stageCatalog.GetEndlessStageNumber(stageIndex) : saved.StageNumber,
                IsEndless: isEndless,
                TotalStageCount: null == _stageCatalog ? 0 : _stageCatalog.Count,
                Money: saved.Money,
                Score: saved.TotalScore,
                Gimmicks: BuildGimmicks(saved)));
        }

        FlipperData ResolveSavedFlipper(string flipperId)
        {
            if (null == _flipperCatalog)
                return null;

            return _flipperCatalog.GetFlipper(_flipperCatalog.IndexOfIdOrDefault(flipperId));
        }

        StageGimmickData[] BuildGimmicks(IngamePlayData saved)
        {
            int[] gimmickIndices = saved.GetGimmickIndices();
            if (null == gimmickIndices || gimmickIndices.Length <= 0)
                return null;

            StageGimmickData[] rolledGimmicks = new StageGimmickData[gimmickIndices.Length];
            for (int i = 0; i < rolledGimmicks.Length; ++i)
                rolledGimmicks[i] = null == _gimmickCatalog ? null : _gimmickCatalog.GetGimmick(gimmickIndices[i]);

            return rolledGimmicks;
        }

        void HandleUIEvent(UIManager.EEvent uiEvent, int payload)
        {
            switch (uiEvent)
            {
                case UIManager.EEvent.FlipperSelectPlayRequested:
                    OnPlayRequested?.Invoke(payload);
                    break;
                case UIManager.EEvent.ContinueRunRequested:
                    OnContinueRequested?.Invoke();
                    break;
            }
        }
    }
}
