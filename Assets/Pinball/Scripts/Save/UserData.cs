using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Pinball.Save
{
    [Serializable]
    public class UserData : SaveDataBase
    {
        public const int NO_PENDING_REWARD_TIER = -1;
        public const int NO_BEST_STAGE = 0;

        [Serializable]
        public struct FlipperBestEntry
        {
            [SerializeField] string _flipperId;
            [SerializeField] int _stageNumber;

            public string FlipperId => _flipperId;
            public int StageNumber => _stageNumber;

            public FlipperBestEntry(string flipperId, int stageNumber)
            {
                _flipperId = flipperId;
                _stageNumber = stageNumber;
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

        public int GetBestStageNumber(string flipperId)
        {
            int index = IndexOfFlipperBest(flipperId);
            return index == INVALID_INDEX ? NO_BEST_STAGE : _flipperBestStages[index].StageNumber;
        }

        public bool TryUpdateBestStage(string flipperId, int stageNumber)
        {
            if (string.IsNullOrEmpty(flipperId))
                return false;

            if (null == _flipperBestStages)
                _flipperBestStages = new List<FlipperBestEntry>();

            int index = IndexOfFlipperBest(flipperId);
            if (index == INVALID_INDEX)
            {
                _flipperBestStages.Add(new FlipperBestEntry(flipperId, stageNumber));
                return true;
            }

            if (stageNumber <= _flipperBestStages[index].StageNumber)
                return false;

            _flipperBestStages[index] = new FlipperBestEntry(flipperId, stageNumber);
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
