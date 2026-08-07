using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.UI
{
    public class ShopView : UIViewBase
    {
        public record Args(int Money) : UIViewArgs
        {
            public override UIViewCatalogData.EViewType ViewType => UIViewCatalogData.EViewType.Shop;
        }

        const string MONEY_FORMAT = "MONEY {0}";

        [SerializeField] TMP_Text _moneyLabel = null;
        [SerializeField] Button _startButton = null;

        public override void OnGenerate()
        {
            _startButton.onClick.AddListener(OnStartClicked);
        }

        public override void OnTerminate()
        {
            _startButton.onClick.RemoveListener(OnStartClicked);
        }

        public override void Bind(UIViewArgs args)
        {
            Args viewArgs = args as Args;
            if (null == viewArgs)
                return;

            _moneyLabel.text = string.Format(MONEY_FORMAT, viewArgs.Money);
        }

        void OnStartClicked()
        {
            UIManager.Hide(UIViewCatalogData.EViewType.Shop);
            UIManager.RaiseEvent(UIManager.EEvent.ShopStartRequested);
        }
    }
}
