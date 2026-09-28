using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using GameCore.Save;

namespace Pinball.Save
{
    [Serializable]
    public class UserData : SaveDataBase
    {
        public const int NO_PENDING_REWARD_TIER = -1;

        const int NO_BEST_STAGE = 0;

        [Serializable]
        public struct FlipperBestEntry
        {
            [SerializeField] string _flipperId;
            [SerializeField] int _stageNumber;
            [SerializeField] bool _isCleared;
            [SerializeField] int _endlessStageNumber;

            public string FlipperId => _flipperId;
            public int StageNumber => _stageNumber;
            public bool IsCleared => _isCleared;
            public int EndlessStageNumber => _endlessStageNumber;

            public FlipperBestEntry(string flipperId, int stageNumber, bool isCleared, int endlessStageNumber)
            {
                _flipperId = flipperId;
                _stageNumber = stageNumber;
                _isCleared = isCleared;
                _endlessStageNumber = endlessStageNumber;
            }
        }

        const int INVALID_INDEX = -1;
        const int PENDING_REWARD_EMPTY = 0;
        const int PENDING_REWARD_TIER_OFFSET = 1;

        [SerializeField] List<FlipperBestEntry> _flipperBestStages = new List<FlipperBestEntry>();
        [SerializeField, FormerlySerializedAs("ClearCount")] int _clearCount = 0;
        [SerializeField, FormerlySerializedAs("UnlockedFlipperIds")] List<string> _unlockedFlipperIds = new List<string>();
        [SerializeField, FormerlySerializedAs("UnlockedItemIds")] List<string> _unlockedItemIds = new List<string>();
        [SerializeField] int _pendingRewardTierNumber = PENDING_REWARD_EMPTY;
        [SerializeField] List<string> _completedTutorialIds = new List<string>();

        public int ClearCount => _clearCount;
        public bool HasPendingReward => _pendingRewardTierNumber > PENDING_REWARD_EMPTY;
        public int PendingRewardTier => _pendingRewardTierNumber - PENDING_REWARD_TIER_OFFSET;

        public bool IsFlipperUnlocked(string flipperId)
        {
            return Contains(_unlockedFlipperIds, flipperId);
        }

        public bool IsItemUnlocked(string itemId)
        {
            return Contains(_unlockedItemIds, itemId);
        }

        public bool IsTutorialCompleted(string tutorialId)
        {
            return Contains(_completedTutorialIds, tutorialId);
        }

        public bool TryCompleteTutorial(string tutorialId)
        {
            if (string.IsNullOrEmpty(tutorialId))
                return false;

            if (null == _completedTutorialIds)
                _completedTutorialIds = new List<string>();

            if (_completedTutorialIds.Contains(tutorialId))
                return false;

            _completedTutorialIds.Add(tutorialId);
            return true;
        }

        public bool TryResetTutorials()
        {
            if (null == _completedTutorialIds || _completedTutorialIds.Count <= 0)
                return false;

            _completedTutorialIds.Clear();
            return true;
        }

        public FlipperBestEntry GetFlipperBest(string flipperId)
        {
            int index = IndexOfFlipperBest(flipperId);
            return index == INVALID_INDEX ? default : _flipperBestStages[index];
        }

        public bool HasAnyEndlessClear()
        {
            if (null == _flipperBestStages)
                return false;

            for (int i = 0; i < _flipperBestStages.Count; ++i)
                if (_flipperBestStages[i].EndlessStageNumber > NO_BEST_STAGE)
                    return true;

            return false;
        }

        public bool TryUpdateBestStage(string flipperId, int stageNumber)
        {
            return TryMergeFlipperBest(flipperId, stageNumber, false, NO_BEST_STAGE);
        }

        public bool TryUpdateBestClear(string flipperId)
        {
            return TryMergeFlipperBest(flipperId, NO_BEST_STAGE, true, NO_BEST_STAGE);
        }

        public bool TryUpdateBestEndlessStage(string flipperId, int endlessStageNumber)
        {
            if (endlessStageNumber <= NO_BEST_STAGE)
                return false;

            return TryMergeFlipperBest(flipperId, NO_BEST_STAGE, true, endlessStageNumber);
        }

        bool TryMergeFlipperBest(string flipperId, int stageNumber, bool isCleared, int endlessStageNumber)
        {
            if (string.IsNullOrEmpty(flipperId))
                return false;

            if (null == _flipperBestStages)
                _flipperBestStages = new List<FlipperBestEntry>();

            int index = IndexOfFlipperBest(flipperId);
            if (index == INVALID_INDEX)
            {
                _flipperBestStages.Add(new FlipperBestEntry(flipperId, stageNumber, isCleared, endlessStageNumber));
                return true;
            }

            FlipperBestEntry current = _flipperBestStages[index];
            FlipperBestEntry merged = new FlipperBestEntry(
                flipperId,
                Math.Max(current.StageNumber, stageNumber),
                current.IsCleared || isCleared,
                Math.Max(current.EndlessStageNumber, endlessStageNumber));
            if (merged.StageNumber == current.StageNumber && merged.IsCleared == current.IsCleared && merged.EndlessStageNumber == current.EndlessStageNumber)
                return false;

            _flipperBestStages[index] = merged;
            return true;
        }

        int IndexOfFlipperBest(string flipperId)
        {
            if (null == _flipperBestStages || string.IsNullOrEmpty(flipperId))
                return INVALID_INDEX;

            for (int i = 0; i < _flipperBestStages.Count; ++i)
                if (_flipperBestStages[i].FlipperId == flipperId)
                    return i;

            return INVALID_INDEX;
        }

        public void RegisterClear(IReadOnlyList<string> flipperIds, IReadOnlyList<string> itemIds, int pendingRewardTier)
        {
            ++_clearCount;
            AddIds(_unlockedFlipperIds, flipperIds);
            AddIds(_unlockedItemIds, itemIds);

            if (pendingRewardTier > NO_PENDING_REWARD_TIER)
                _pendingRewardTierNumber = pendingRewardTier + PENDING_REWARD_TIER_OFFSET;
        }

        public void ClearPendingReward()
        {
            _pendingRewardTierNumber = PENDING_REWARD_EMPTY;
        }

        static bool Contains(List<string> ids, string id)
        {
            if (null == ids || string.IsNullOrEmpty(id))
                return false;

            return ids.Contains(id);
        }

        static void AddIds(List<string> ids, IReadOnlyList<string> newIds)
        {
            if (null == ids || null == newIds)
                return;

            for (int i = 0; i < newIds.Count; ++i)
            {
                string id = newIds[i];
                if (string.IsNullOrEmpty(id) || ids.Contains(id))
                    continue;

                ids.Add(id);
            }
        }
    }
}
