using System;
using Pinball.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.UI
{
    public class SettingsKeyRowView : MonoBehaviour
    {
        [SerializeField] EKeyAction _keyAction = EKeyAction.FlipperLeft;
        [SerializeField] Button _valueButton = null;
        [SerializeField] TMP_Text _valueLabel = null;

        Action<EKeyAction> _onClicked = null;

        public EKeyAction KeyAction => _keyAction;

        public void Init(Action<EKeyAction> onClicked)
        {
            _onClicked = onClicked;
            _valueButton.onClick.AddListener(OnClicked);
        }

        public void Release()
        {
            _valueButton.onClick.RemoveListener(OnClicked);
            _onClicked = null;
        }

        public void SetValue(string value)
        {
            _valueLabel.text = value;
        }

        void OnClicked()
        {
            _onClicked?.Invoke(_keyAction);
        }
    }
}
