using System;
using System.Text;
using Pinball.Audio;
using Pinball.Ingame;
using Pinball.Ingame.Reward;
using Pinball.Ingame.Stage;
using Pinball.Localization;
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

        const string FIRST_CLEAR_HINT = "CLEAR THE RUN TO UNLOCK";
        const string FIRST_CLEAR_HINT_KEY = "ui.lobby.unlock_hint_first";
        const string CLEAR_COUNT_HINT_FORMAT = "CLEAR THE RUN x{0} TO UNLOCK";
        const string CLEAR_COUNT_HINT_KEY = "ui.lobby.unlock_hint_count";
        const string GIMMICK_SEPARATOR = " / ";

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

            return new FlipperSelectView.FlipperEntry(
                DisplayName: flipper.GetDisplayName(),
                Icon: flipper.GetIcon(),
                AbilityDescription: flipper.GetAbilityDescription(),
                DrawbackDescription: flipper.GetDrawbackDescription(),
                IsUnlocked: _unlockProgress.IsFlipperUnlocked(flipper),
                UnlockHint: BuildUnlockHint(flipper));
        }

        public void OpenContinue()
        {
            IngamePlayData saved = SaveManager.GetIngameData();
            if (null == saved)
                return;

            FlipperData flipper = ResolveSavedFlipper(saved.FlipperId);
            UIManager.Show(new ContinueRunView.Args(
                FlipperName: null == flipper ? string.Empty : flipper.GetDisplayName(),
                FlipperIcon: null == flipper ? null : flipper.GetIcon(),
                StageNumber: saved.StageNumber,
                TotalStageCount: null == _stageCatalog ? 0 : _stageCatalog.Count,
                Money: saved.Money,
                Score: saved.TotalScore,
                GimmickText: BuildGimmickText(saved)));
        }

        FlipperData ResolveSavedFlipper(string flipperId)
        {
            if (null == _flipperCatalog)
                return null;

            return _flipperCatalog.GetFlipper(_flipperCatalog.IndexOfIdOrDefault(flipperId));
        }

        string BuildUnlockHint(FlipperData flipper)
        {
            int tier = _unlockProgress.GetFlipperUnlockTier(flipper);
            if (tier == ClearRewardCatalogData.INVALID_TIER)
                return string.Empty;

            if (tier <= 0)
                return LocalizationTable.Get(FIRST_CLEAR_HINT_KEY, FIRST_CLEAR_HINT);

            return LocalizationTable.Format(CLEAR_COUNT_HINT_KEY, CLEAR_COUNT_HINT_FORMAT, tier + 1);
        }

        string BuildGimmickText(IngamePlayData saved)
        {
            StringBuilder builder = new StringBuilder();
            int[] gimmickIndices = saved.GetGimmickIndices();
            if (null != gimmickIndices && gimmickIndices.Length > 0)
            {
                for (int i = 0; i < gimmickIndices.Length; ++i)
                    AppendGimmickLabel(builder, null == _gimmickCatalog ? null : _gimmickCatalog.GetGimmick(gimmickIndices[i]));

                return builder.ToString();
            }

            StageData stage = null == _stageCatalog ? null : _stageCatalog.GetStage(saved.StageNumber - 1);
            if (null == stage)
                return string.Empty;

            for (int i = 0; i < stage.GimmickCount; ++i)
                AppendGimmickLabel(builder, stage.GetGimmick(i));

            return builder.ToString();
        }

        static void AppendGimmickLabel(StringBuilder builder, StageGimmickData gimmick)
        {
            if (null == gimmick)
                return;

            if (builder.Length > 0)
                builder.Append(GIMMICK_SEPARATOR);

            builder.Append(StageGimmickLabelFormatter.Format(gimmick));
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
