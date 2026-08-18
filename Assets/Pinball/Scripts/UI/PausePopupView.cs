using UnityEngine;
using UnityEngine.UI;

namespace Pinball.UI
{
    public class PausePopupView : UIViewBase
    {
        public record Args : UIViewArgs
        {
            public override UIViewCatalogData.EViewType ViewType => UIViewCatalogData.EViewType.PausePopup;
        }

        [SerializeField] Button _settingsButton = null;
        [SerializeField] Button _exitButton = null;
        [SerializeField] Button _resumeButton = null;

        public override void OnGenerate()
        {
            _settingsButton.onClick.AddListener(OnSettingsClicked);
            _exitButton.onClick.AddListener(OnExitClicked);
            _resumeButton.onClick.AddListener(OnResumeClicked);
        }

        public override void OnTerminate()
        {
            _settingsButton.onClick.RemoveListener(OnSettingsClicked);
            _exitButton.onClick.RemoveListener(OnExitClicked);
            _resumeButton.onClick.RemoveListener(OnResumeClicked);
        }

        public override void Bind(UIViewArgs args) { }

        void OnSettingsClicked()
        {
            UIManager.Show(new SettingsView.Args());
        }

        void OnExitClicked()
        {
            UIManager.Hide(UIViewCatalogData.EViewType.PausePopup);
            UIManager.RaiseEvent(UIManager.EEvent.ExitToLobbyRequested);
        }

        void OnResumeClicked()
        {
            UIManager.RaiseEvent(UIManager.EEvent.PauseResumeRequested);
        }
    }
}
