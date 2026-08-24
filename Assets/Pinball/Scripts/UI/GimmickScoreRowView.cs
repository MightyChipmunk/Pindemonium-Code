using System.Globalization;
using Pinball.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.UI
{
    public class GimmickScoreRowView : MonoBehaviour
    {
        const string NUMBER_FORMAT = "N0";
        const float MIN_FILL_RATIO = 0.075f;

        [SerializeField] TMP_Text _nameLabel = null;
        [SerializeField] TMP_Text _scoreLabel = null;
        [SerializeField] Image _fillImage = null;
        [SerializeField] Sprite _fillSprite = null;
        [SerializeField] Sprite _topFillSprite = null;

        public void Bind(LocalizedTextRef nameRef, int score, float ratio, bool isTop)
        {
            _nameLabel.text = nameRef.Resolve();
            _scoreLabel.text = score.ToString(NUMBER_FORMAT, CultureInfo.InvariantCulture);
            _fillImage.sprite = isTop ? _topFillSprite : _fillSprite;
            _fillImage.enabled = score > 0;
            _fillImage.fillAmount = Mathf.Clamp(ratio, MIN_FILL_RATIO, 1f);
        }

        public void SetVisible(bool isVisible)
        {
            gameObject.SetActive(isVisible);
        }
    }
}
