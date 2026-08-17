using System.Collections.Generic;
using UnityEngine;

namespace Pinball.Ingame.Stage
{
    public class StageGimmickRoller : IStageGimmickSource
    {
        StageGimmickCatalogData _catalog = null;
        List<StageGimmickData> _gimmicks = null;
        int _rerollCount = 0;

        public int GimmickCount => null == _gimmicks ? 0 : _gimmicks.Count;
        public int CurrentRerollPrice => null == _catalog ? 0 : _catalog.RerollBasePrice + _catalog.RerollPriceIncrement * _rerollCount;
        public bool CanReroll => GimmickCount > 0 && null != _catalog && _catalog.Count > 0;

        public void Init(StageGimmickCatalogData catalog)
        {
            _catalog = catalog;
            _gimmicks = new List<StageGimmickData>();
            _rerollCount = 0;
        }

        public void Release()
        {
            if (null != _gimmicks)
            {
                _gimmicks.Clear();
                _gimmicks = null;
            }

            _catalog = null;
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

            for (int i = 0; i < stage.GimmickCount; ++i)
            {
                StageGimmickData gimmick = stage.GetGimmick(i);
                if (null == gimmick)
                    continue;

                _gimmicks.Add(gimmick);
            }
        }

        public void RestoreGimmicks(int[] catalogIndices)
        {
            _gimmicks.Clear();
            _rerollCount = 0;

            if (null == catalogIndices || null == _catalog)
                return;

            for (int i = 0; i < catalogIndices.Length; ++i)
            {
                StageGimmickData gimmick = _catalog.GetGimmick(catalogIndices[i]);
                if (null == gimmick)
                    continue;

                _gimmicks.Add(gimmick);
            }
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

                int pickIndex = Random.Range(0, candidates.Count);
                _gimmicks[i] = candidates[pickIndex];
                candidates.RemoveAt(pickIndex);
            }
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
