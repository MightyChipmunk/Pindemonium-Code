using System;
using Pinball.Ingame.Map;
using Pinball.Ingame.Stage;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.UI
{
    public class StageGimmickView : MonoBehaviour
    {
        [Serializable]
        public struct GimmickIconEntry
        {
            public MapInteractable.EGimmickType GimmickType;
            public Sprite Icon;
        }

        const string COLLAPSED_ARROW = "▼";
        const string EXPANDED_ARROW = "▲";

        [SerializeField] Button _toggleButton = null;
        [SerializeField] TMP_Text _arrowLabel = null;
        [SerializeField] RectTransform _listRoot = null;
        [SerializeField] GimmickRowView[] _rows = null;
        [SerializeField] Sprite _targetScoreIcon = null;
        [SerializeField] Sprite _timeLimitIcon = null;
        [SerializeField] GimmickIconEntry[] _gimmickIconArr = null;
        [SerializeField] Color _timeRingColor = new Color(1f, 0.839f, 0.302f, 1f);
        [SerializeField] Color _penaltyRingColor = new Color(0.69f, 0.259f, 0.165f, 1f);

        void Awake()
        {
            _toggleButton.onClick.AddListener(OnToggleClicked);
            SetExpanded(false);
        }

        void OnDestroy()
        {
            _toggleButton.onClick.RemoveListener(OnToggleClicked);
        }

        public void SetGimmicks(StageGimmickData[] gimmicks)
        {
            int gimmickCount = null == gimmicks ? 0 : gimmicks.Length;
            gameObject.SetActive(gimmickCount > 0);

            for (int i = 0; i < _rows.Length; ++i)
            {
                bool isVisible = i < gimmickCount && null != gimmicks[i];
                _rows[i].SetVisible(isVisible);
                if (!isVisible)
                    continue;

                StageGimmickData gimmick = gimmicks[i];
                _rows[i].Bind(GetIcon(gimmick), GetRingColor(gimmick), StageGimmickLabelFormatter.Format(gimmick));
            }

            SetExpanded(false);
        }

        void OnToggleClicked()
        {
            SetExpanded(!_listRoot.gameObject.activeSelf);
        }

        void SetExpanded(bool isExpanded)
        {
            _listRoot.gameObject.SetActive(isExpanded);
            _arrowLabel.text = isExpanded ? EXPANDED_ARROW : COLLAPSED_ARROW;
            RebuildLayout();
        }

        void RebuildLayout()
        {
            if (!gameObject.activeInHierarchy)
                return;

            if (_listRoot.gameObject.activeSelf)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_listRoot);

            LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
        }

        Sprite GetIcon(StageGimmickData gimmick)
        {
            switch (gimmick.Type)
            {
                case StageGimmickData.EType.TargetScoreMultiplier:
                    return _targetScoreIcon;
                case StageGimmickData.EType.TimeLimit:
                    return _timeLimitIcon;
                default:
                    return FindGimmickIcon(gimmick.TargetGimmickType);
            }
        }

        Sprite FindGimmickIcon(MapInteractable.EGimmickType gimmickType)
        {
            if (null == _gimmickIconArr)
                return null;

            for (int i = 0; i < _gimmickIconArr.Length; ++i)
            {
                if (_gimmickIconArr[i].GimmickType == gimmickType)
                    return _gimmickIconArr[i].Icon;
            }

            return null;
        }

        Color GetRingColor(StageGimmickData gimmick)
        {
            return gimmick.Type == StageGimmickData.EType.TimeLimit ? _timeRingColor : _penaltyRingColor;
        }
    }
}
