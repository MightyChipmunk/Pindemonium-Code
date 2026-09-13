using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameCore.Util;

namespace Pinball.UI
{
    public class VoucherCardView : MonoBehaviour, IPoolObject
    {
        public record BindParam(Sprite Icon, string Name, string Description);

        [SerializeField] Image _iconImage = null;
        [SerializeField] TMP_Text _nameLabel = null;
        [SerializeField] TMP_Text _descriptionLabel = null;

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
            _descriptionLabel.text = param.Description;
        }
    }
}
