using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Pinball.Save
{
    [Serializable]
    public class UserData : SaveDataBase
    {
        [SerializeField, FormerlySerializedAs("BestStageNumber")] int _bestStageNumber = 0;
        [SerializeField, FormerlySerializedAs("ClearCount")] int _clearCount = 0;
        [SerializeField, FormerlySerializedAs("UnlockedFlipperIds")] List<string> _unlockedFlipperIds = new List<string>();
        [SerializeField, FormerlySerializedAs("UnlockedItemIds")] List<string> _unlockedItemIds = new List<string>();

        public int BestStageNumber => _bestStageNumber;
        public int ClearCount => _clearCount;

        public bool IsFlipperUnlocked(string flipperId)
        {
            return Contains(_unlockedFlipperIds, flipperId);
        }

        public bool IsItemUnlocked(string itemId)
        {
            return Contains(_unlockedItemIds, itemId);
        }

        public bool TryUpdateBestStage(int stageNumber)
        {
            if (stageNumber <= _bestStageNumber)
                return false;

            _bestStageNumber = stageNumber;
            return true;
        }

        public void RegisterClear(IReadOnlyList<string> flipperIds, IReadOnlyList<string> itemIds)
        {
            ++_clearCount;
            AddIds(_unlockedFlipperIds, flipperIds);
            AddIds(_unlockedItemIds, itemIds);
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
