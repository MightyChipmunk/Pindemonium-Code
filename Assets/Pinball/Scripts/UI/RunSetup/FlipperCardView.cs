using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.UI
{
    public class FlipperCardView : MonoBehaviour
    {
        public record BindParam(string DisplayName, Sprite Icon, bool IsUnlocked, string LockedHint);

        const string LOCKED_TITLE = "???";
        const float LOCKED_ICON_ALPHA = 0.25f;

        [SerializeField] TMP_Text _titleLabel = null;
        [SerializeField] Image _iconImage = null;
        [SerializeField] GameObject _lockedRoot = null;
        [SerializeField] TMP_Text _lockedHintLabel = null;

        public void Bind(BindParam param)
        {
            _titleLabel.text = param.IsUnlocked ? param.DisplayName : LOCKED_TITLE;
            _iconImage.sprite = param.Icon;
            _iconImage.enabled = null != param.Icon;
            Color iconColor = _iconImage.color;
            iconColor.a = param.IsUnlocked ? 1f : LOCKED_ICON_ALPHA;
            _iconImage.color = iconColor;
            _lockedRoot.SetActive(!param.IsUnlocked);
            _lockedHintLabel.text = param.LockedHint;
        }
    }
}
