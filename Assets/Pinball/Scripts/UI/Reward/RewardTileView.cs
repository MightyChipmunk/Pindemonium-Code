using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.UI
{
    public class RewardTileView : MonoBehaviour
    {
        public record BindParam(Sprite Icon, string DisplayName, string KindText);

        [SerializeField] Image _iconImage = null;
        [SerializeField] TMP_Text _nameLabel = null;
        [SerializeField] TMP_Text _kindLabel = null;

        public void Bind(BindParam param)
        {
            gameObject.SetActive(true);
            _iconImage.sprite = param.Icon;
            _iconImage.enabled = null != param.Icon;
            _nameLabel.text = param.DisplayName;
            _kindLabel.text = param.KindText;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
