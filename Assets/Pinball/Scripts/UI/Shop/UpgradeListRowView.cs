using Pinball.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.UI
{
    public class UpgradeListRowView : MonoBehaviour, IPoolObject
    {
        public record BindParam(Sprite Icon, string Name, string CountText);

        [SerializeField] Image _iconImage = null;
        [SerializeField] TMP_Text _nameLabel = null;
        [SerializeField] TMP_Text _countLabel = null;

        public IPoolBase OwnPool { get; set; }

        public void OnGenerate()
        {
        }

        public void OnGet()
        {
            gameObject.SetActive(true);
        }

        public void OnReturn()
        {
        }

        public void OnTerminate()
        {
        }

        public void Bind(BindParam param)
        {
            _iconImage.sprite = param.Icon;
            _iconImage.enabled = null != param.Icon;
            _nameLabel.text = param.Name;
            _countLabel.text = param.CountText;
        }
    }
}
