using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.UI
{
    public class GimmickRowView : MonoBehaviour
    {
        [SerializeField] Image _iconImage = null;
        [SerializeField] Image _ringImage = null;
        [SerializeField] TMP_Text _label = null;

        public void Bind(Sprite icon, Color ringColor, string label)
        {
            _iconImage.sprite = icon;
            _iconImage.enabled = null != icon;
            _ringImage.color = ringColor;
            _label.text = label;
        }

        public void SetVisible(bool isVisible)
        {
            gameObject.SetActive(isVisible);
        }
    }
}
