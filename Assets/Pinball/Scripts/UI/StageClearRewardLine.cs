using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Pinball.UI
{
    public class StageClearRewardLine : MonoBehaviour
    {
        const float HIDDEN_SCALE = 0f;
        const float SHOWN_SCALE = 1f;

        [SerializeField] RectTransform _root = null;
        [SerializeField] TMP_Text _label = null;

        public void SetText(string text)
        {
            if (null == _label)
                return;

            _label.text = text;
        }

        public void SetHidden()
        {
            if (null == _root)
                return;

            _root.localScale = Vector3.one * HIDDEN_SCALE;
        }

        public Tween CreateShowTween(float duration)
        {
            if (null == _root)
                return null;

            return _root.DOScale(SHOWN_SCALE, duration).SetEase(Ease.OutBack);
        }
    }
}
