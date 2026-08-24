using System.Collections.Generic;
using UnityEngine;

namespace Pinball.Ingame.Stage
{
    public class StageGimmickRoller : IStageGimmickSource
    {
        const int INVALID_INDEX = -1;

        StageGimmickCatalogData _catalog = null;
        List<StageGimmickData> _gimmicks = null;
        int _rerollCount = 0;
        bool _isFirstRerollFree = false;

        public int GimmickCount => null == _gimmicks ? 0 : _gimmicks.Count;

        public int CurrentRerollPrice
        {
            get
            {
                if (null == _catalog)
                    return 0;

                if (_isFirstRerollFree && _rerollCount <= 0)
                    return 0;

                return _catalog.RerollBasePrice + _catalog.RerollPriceIncrement * _rerollCount;
            }
        }
        public bool CanReroll => GimmickCount > 0 && null != _catalog && _catalog.Count > 0;

        public StageGimmickRoller(StageGimmickCatalogData catalog)
        {
            _catalog = catalog;
            _gimmicks = new List<StageGimmickData>();
            _rerollCount = 0;
        }

        public void SetFirstRerollFree(bool isFree)
        {
            _isFirstRerollFree = isFree;
        }

        public StageGimmickData GetGimmick(int index)
        {
            if (index < 0 || index >= GimmickCount)
                return null;

            return _gimmicks[index];
        }

        public void SetBaseGimmicks(StageData stage)
        {
            _gimmicks.Clear();
            _rerollCount = 0;

            if (null == stage)
                return;

            List<StageGimmickData> candidates = CollectStageCandidates(stage);
            int rollCount = Mathf.Min(stage.GimmickRollCount, candidates.Count);
            for (int i = 0; i < rollCount; ++i)
            {
                int pickIndex = PickCandidateIndex(candidates, INVALID_INDEX);
                if (pickIndex == INVALID_INDEX)
                    return;

                _gimmicks.Add(candidates[pickIndex]);
                candidates.RemoveAt(pickIndex);
            }
        }

        public void RestoreOrSetBase(int[] catalogIndices, StageData baseStage)
        {
            if (null == catalogIndices || catalogIndices.Length <= 0)
            {
                SetBaseGimmicks(baseStage);
                return;
            }

            RestoreGimmicks(catalogIndices);
        }

        void RestoreGimmicks(int[] catalogIndices)
        {
            _gimmicks.Clear();
            _rerollCount = 0;

            if (null == catalogIndices || null == _catalog)
                return;

            for (int i = 0; i < catalogIndices.Length; ++i)
                AddGimmick(_catalog.GetGimmick(catalogIndices[i]));
        }

        public int[] GetCatalogIndices()
        {
            int[] indices = new int[GimmickCount];
            for (int i = 0; i < indices.Length; ++i)
                indices[i] = null == _catalog ? StageGimmickCatalogData.INVALID_INDEX : _catalog.IndexOf(_gimmicks[i]);

            return indices;
        }

        public void Reroll()
        {
            if (!CanReroll)
                return;

            ++_rerollCount;

            List<StageGimmickData> candidates = CollectCandidates();
            for (int i = 0; i < _gimmicks.Count; ++i)
            {
                if (candidates.Count <= 0)
                    return;

                int pickIndex = PickCandidateIndex(candidates, i);
                if (pickIndex == INVALID_INDEX)
                    continue;

                _gimmicks[i] = candidates[pickIndex];
                candidates.RemoveAt(pickIndex);
            }
        }

        int PickCandidateIndex(List<StageGimmickData> candidates, int slotIndex)
        {
            int acceptableCount = 0;
            for (int i = 0; i < candidates.Count; ++i)
            {
                if (IsAcceptable(candidates[i], slotIndex))
                    ++acceptableCount;
            }

            if (acceptableCount <= 0)
                return INVALID_INDEX;

            int pickOrder = Random.Range(0, acceptableCount);
            for (int i = 0; i < candidates.Count; ++i)
            {
                if (!IsAcceptable(candidates[i], slotIndex))
                    continue;

                if (pickOrder <= 0)
                    return i;

                --pickOrder;
            }

            return INVALID_INDEX;
        }

        void AddGimmick(StageGimmickData gimmick)
        {
            if (null == gimmick)
                return;

            if (!IsAcceptable(gimmick, INVALID_INDEX))
            {
                Debug.LogWarning($"[StageGimmickRoller] 블랙리스트에 걸린 기믹을 제외합니다: {gimmick.name}");
                return;
            }

            _gimmicks.Add(gimmick);
        }

        bool IsAcceptable(StageGimmickData candidate, int slotIndex)
        {
            for (int i = 0; i < _gimmicks.Count; ++i)
            {
                if (i == slotIndex)
                    continue;

                if (StageGimmickBlacklist.IsConflict(candidate, _gimmicks[i]))
                    return false;
            }

            return true;
        }

        List<StageGimmickData> CollectStageCandidates(StageData stage)
        {
            List<StageGimmickData> candidates = new List<StageGimmickData>();
            for (int i = 0; i < stage.GimmickPoolCount; ++i)
            {
                StageGimmickData gimmick = stage.GetPoolGimmick(i);
                if (null == gimmick)
                    continue;

                if (candidates.Contains(gimmick))
                    continue;

                candidates.Add(gimmick);
            }

            return candidates;
        }

        List<StageGimmickData> CollectCandidates()
        {
            List<StageGimmickData> candidates = new List<StageGimmickData>();
            for (int i = 0; i < _catalog.Count; ++i)
            {
                StageGimmickData gimmick = _catalog.GetGimmick(i);
                if (null == gimmick)
                    continue;

                if (_gimmicks.Contains(gimmick))
                    continue;

                candidates.Add(gimmick);
            }

            return candidates;
        }
    }
}
