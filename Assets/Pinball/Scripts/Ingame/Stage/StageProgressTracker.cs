using System;
using UnityEngine;

namespace Pinball.Ingame.Stage
{
    public class StageProgressTracker
    {
        const float NO_TIME_LIMIT = 0f;

        StageCatalogData _catalog = null;
        StageGimmickResolver _gimmickResolver = null;
        EndlessConfigData _endlessConfig = null;
        int _stageIndex = 0;
        int _stageStartScore = 0;
        int _totalScore = 0;
        float _timeLimit = NO_TIME_LIMIT;
        float _remainingTime = NO_TIME_LIMIT;
        bool _isCleared = false;
        bool _isTimeExpired = false;
        bool _isEndless = false;

        public int StageNumber => _stageIndex + 1;
        public int StageScore => _totalScore - _stageStartScore;
        public bool HasTimeLimit => _timeLimit > NO_TIME_LIMIT;
        public int RemainingSeconds => Mathf.CeilToInt(_remainingTime);
        public bool IsEndless => _isEndless;
        public int EndlessStageNumber => null == _catalog ? 0 : _catalog.GetEndlessStageNumber(_stageIndex);

        public int TargetScore
        {
            get
            {
                StageData stage = GetCurrentStage();
                if (null == stage)
                    return 0;

                return _gimmickResolver.GetTargetScore(GetBaseTargetScore(stage));
            }
        }

        public event Action<int, StageData> OnStageStarted;
        public event Action<int, StageData> OnStageCleared;
        public event Action OnAllStagesCleared;
        public event Action<int> OnStageScoreChanged;
        public event Action<int> OnRemainingSecondsChanged;
        public event Action OnTimeExpired;

        public StageProgressTracker(StageCatalogData catalog, int startStageNumber, int startTotalScore, StageGimmickResolver gimmickResolver, EndlessConfigData endlessConfig, bool isEndless)
        {
            _catalog = catalog;
            _gimmickResolver = gimmickResolver;
            _endlessConfig = endlessConfig;
            _stageIndex = Mathf.Max(0, startStageNumber - 1);
            _stageStartScore = startTotalScore;
            _totalScore = startTotalScore;
            _isEndless = isEndless;
            ResetStageState();
        }

        public void StartEndless()
        {
            _isEndless = true;
        }

        public StageData GetCurrentStage()
        {
            if (null == _catalog)
                return null;

            return _catalog.GetStage(_stageIndex);
        }

        public void StartCurrentStage()
        {
            _timeLimit = _gimmickResolver.GetTimeLimit();
            _remainingTime = _timeLimit;
            _isTimeExpired = false;
            OnStageScoreChanged?.Invoke(StageScore);
            OnStageStarted?.Invoke(StageNumber, GetCurrentStage());
        }

        public void SetTotalScore(int totalScore)
        {
            _totalScore = totalScore;

            if (_isCleared || _isTimeExpired)
                return;

            OnStageScoreChanged?.Invoke(StageScore);

            if (CanClearByScore())
                ClearCurrentStage();
        }

        public void Tick(float deltaTime)
        {
            if (!HasTimeLimit)
                return;

            if (_isCleared || _isTimeExpired)
                return;

            int previousSeconds = RemainingSeconds;
            _remainingTime = Mathf.Max(NO_TIME_LIMIT, _remainingTime - deltaTime);

            if (RemainingSeconds != previousSeconds)
                OnRemainingSecondsChanged?.Invoke(RemainingSeconds);

            if (_remainingTime > NO_TIME_LIMIT)
                return;

            _isTimeExpired = true;
            OnTimeExpired?.Invoke();
        }

        public void AdvanceToNextStage()
        {
            if (!_isCleared)
                return;

            ++_stageIndex;
            _stageStartScore = _totalScore;
            ResetStageState();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void SetCheatCleared()
        {
            _isCleared = true;
        }
#endif

        void ResetStageState()
        {
            _isCleared = false;
            _isTimeExpired = false;
            _timeLimit = NO_TIME_LIMIT;
            _remainingTime = NO_TIME_LIMIT;
        }

        int GetBaseTargetScore(StageData stage)
        {
            if (!_isEndless || null == _endlessConfig)
                return stage.TargetScore;

            return _endlessConfig.GetTargetScore(stage.TargetScore, EndlessStageNumber);
        }

        bool CanClearByScore()
        {
            int targetScore = TargetScore;
            if (targetScore <= 0)
                return false;

            return StageScore >= targetScore;
        }

        void ClearCurrentStage()
        {
            _isCleared = true;

            if (IsFinalStage())
            {
                OnAllStagesCleared?.Invoke();
                return;
            }

            OnStageCleared?.Invoke(StageNumber, GetCurrentStage());
        }

        bool IsFinalStage()
        {
            if (_isEndless)
                return false;

            if (null == _catalog)
                return false;

            return StageNumber >= _catalog.Count;
        }
    }
}
