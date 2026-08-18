using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.UI
{
    public class RewardTileView : MonoBehaviour
    {
        public record BindParam(
            int TileIndex,
            Sprite Icon,
            string DisplayName,
            string KindText,
            Action<int, Vector3, bool> HoverCallback);

        [SerializeField] Image _iconImage = null;
        [SerializeField] TMP_Text _nameLabel = null;
        [SerializeField] TMP_Text _kindLabel = null;
        [SerializeField] UITooltipTrigger _tooltipTrigger = null;

        public void Bind(BindParam param)
        {
            gameObject.SetActive(true);
            _iconImage.sprite = param.Icon;
            _iconImage.enabled = null != param.Icon;
            _nameLabel.text = param.DisplayName;
            _kindLabel.text = param.KindText;
            _tooltipTrigger.SetPayload(param.TileIndex);
            _tooltipTrigger.SetCallback(param.HoverCallback);
        }

        public void Hide()
        {
            _tooltipTrigger.SetCallback(null);
            gameObject.SetActive(false);
        }
    }
}
