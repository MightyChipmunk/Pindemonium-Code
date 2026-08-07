using System;
using UnityEngine;

namespace Pinball.Ingame.Stage
{
    public class StageProgressTracker
    {
        StageCatalogData _catalog = null;
        int _stageIndex = 0;
        int _stageStartScore = 0;
        int _totalScore = 0;
        bool _isCleared = false;

        public int StageNumber => _stageIndex + 1;
        public int StageScore => _totalScore - _stageStartScore;

        public event Action<int, StageData> OnStageStarted;
        public event Action<int, StageData> OnStageCleared;
        public event Action<int> OnStageScoreChanged;

        public void Init(StageCatalogData catalog, int startStageNumber, int startTotalScore)
        {
            _catalog = catalog;
            _stageIndex = Mathf.Max(0, startStageNumber - 1);
            _stageStartScore = startTotalScore;
            _totalScore = startTotalScore;
            _isCleared = false;
        }

        public void Release()
        {
            OnStageStarted = null;
            OnStageCleared = null;
            OnStageScoreChanged = null;
            _catalog = null;
        }

        public StageData GetCurrentStage()
        {
            if (null == _catalog)
                return null;

            return _catalog.GetStage(_stageIndex);
        }

        public void StartFirstStage()
        {
            OnStageStarted?.Invoke(StageNumber, GetCurrentStage());
        }

        public void SetTotalScore(int totalScore)
        {
            _totalScore = totalScore;

            if (_isCleared)
                return;

            OnStageScoreChanged?.Invoke(StageScore);

            if (CanClearByScore())
                ClearCurrentStage();
        }

        public void AdvanceToNextStage()
        {
            if (!_isCleared)
                return;

            _isCleared = false;
            ++_stageIndex;
            _stageStartScore = _totalScore;
            OnStageStarted?.Invoke(StageNumber, GetCurrentStage());
        }

        bool CanClearByScore()
        {
            StageData stage = GetCurrentStage();
            if (null == stage)
                return false;

            if (stage.TargetScore <= 0)
                return false;

            return StageScore >= stage.TargetScore;
        }

        void ClearCurrentStage()
        {
            _isCleared = true;
            OnStageCleared?.Invoke(StageNumber, GetCurrentStage());
        }
    }
}
