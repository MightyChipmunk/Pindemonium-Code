using System;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.Lobby
{
    public class LobbyUIRoot : MonoBehaviour
    {
        [SerializeField] Button _continueButton = null;
        [SerializeField] Button _newGameButton = null;
        [SerializeField] Button _settingsButton = null;

        Action _onContinueClicked = null;
        Action _onNewGameClicked = null;
        Action _onSettingsClicked = null;

        public void Init(Action onContinueClicked, Action onNewGameClicked, Action onSettingsClicked)
        {
            _onContinueClicked = onContinueClicked;
            _onNewGameClicked = onNewGameClicked;
            _onSettingsClicked = onSettingsClicked;

            _continueButton.onClick.AddListener(OnContinueClicked);
            _newGameButton.onClick.AddListener(OnNewGameClicked);
            _settingsButton.onClick.AddListener(OnSettingsClicked);
        }

        public void Release()
        {
            _continueButton.onClick.RemoveListener(OnContinueClicked);
            _newGameButton.onClick.RemoveListener(OnNewGameClicked);
            _settingsButton.onClick.RemoveListener(OnSettingsClicked);

            _onContinueClicked = null;
            _onNewGameClicked = null;
            _onSettingsClicked = null;
        }

        void OnContinueClicked()
        {
            _onContinueClicked?.Invoke();
        }

        void OnNewGameClicked()
        {
            _onNewGameClicked?.Invoke();
        }

        void OnSettingsClicked()
        {
            _onSettingsClicked?.Invoke();
        }
    }
}
