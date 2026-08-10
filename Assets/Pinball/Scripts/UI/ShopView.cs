using System.Text;
using Pinball.Ingame;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.UI
{
    public class ShopView : UIViewBase
    {
        public record Args(int Money, int RerollPrice) : UIViewArgs
        {
            public override UIViewCatalogData.EViewType ViewType => UIViewCatalogData.EViewType.Shop;
        }

        public record OfferArgs(int SlotIndex, int ItemIndex) : UIViewArgs
        {
            public override UIViewCatalogData.EViewType ViewType => UIViewCatalogData.EViewType.Shop;
        }

        public record PassivesArgs(int SlotCount, int ItemIndex0, int ItemIndex1, int ItemIndex2) : UIViewArgs
        {
            public override UIViewCatalogData.EViewType ViewType => UIViewCatalogData.EViewType.Shop;
        }

        const string MONEY_FORMAT = "MONEY {0}";
        const string REROLL_FORMAT = "REROLL {0}";
        const string OFFER_FORMAT = "{0}\n{1}";
        const string SOLD_LABEL = "SOLD";
        const string PASSIVES_HEADER_FORMAT = "PASSIVES {0}/{1}";
        const string PASSIVE_NAME_SEPARATOR = "  |  ";

        [SerializeField] ShopCatalogData _catalog = null;
        [SerializeField] TMP_Text _moneyLabel = null;
        [SerializeField] TMP_Text _passivesLabel = null;
        [SerializeField] Button _offerButton0 = null;
        [SerializeField] TMP_Text _offerLabel0 = null;
        [SerializeField] Button _offerButton1 = null;
        [SerializeField] TMP_Text _offerLabel1 = null;
        [SerializeField] Button _offerButton2 = null;
        [SerializeField] TMP_Text _offerLabel2 = null;
        [SerializeField] Button _rerollButton = null;
        [SerializeField] TMP_Text _rerollLabel = null;
        [SerializeField] Button _startButton = null;

        public override void OnGenerate()
        {
            _offerButton0.onClick.AddListener(OnOffer0Clicked);
            _offerButton1.onClick.AddListener(OnOffer1Clicked);
            _offerButton2.onClick.AddListener(OnOffer2Clicked);
            _rerollButton.onClick.AddListener(OnRerollClicked);
            _startButton.onClick.AddListener(OnStartClicked);
        }

        public override void OnTerminate()
        {
            _offerButton0.onClick.RemoveListener(OnOffer0Clicked);
            _offerButton1.onClick.RemoveListener(OnOffer1Clicked);
            _offerButton2.onClick.RemoveListener(OnOffer2Clicked);
            _rerollButton.onClick.RemoveListener(OnRerollClicked);
            _startButton.onClick.RemoveListener(OnStartClicked);
        }

        public override void Bind(UIViewArgs args)
        {
            Args mainArgs = args as Args;
            if (null != mainArgs)
            {
                BindMain(mainArgs);
                return;
            }

            OfferArgs offerArgs = args as OfferArgs;
            if (null != offerArgs)
            {
                BindOffer(offerArgs);
                return;
            }

            PassivesArgs passivesArgs = args as PassivesArgs;
            if (null != passivesArgs)
                BindPassives(passivesArgs);
        }

        void BindMain(Args args)
        {
            _moneyLabel.text = string.Format(MONEY_FORMAT, args.Money);
            _rerollLabel.text = string.Format(REROLL_FORMAT, args.RerollPrice);
        }

        void BindOffer(OfferArgs args)
        {
            Button button = GetOfferButton(args.SlotIndex);
            TMP_Text label = GetOfferLabel(args.SlotIndex);
            if (null == button || null == label)
                return;

            ShopItemData item = null == _catalog ? null : _catalog.GetItem(args.ItemIndex);
            if (null == item)
            {
                button.interactable = false;
                label.text = SOLD_LABEL;
                return;
            }

            button.interactable = true;
            label.text = string.Format(OFFER_FORMAT, item.GetDisplayName(), item.Price);
        }

        void BindPassives(PassivesArgs args)
        {
            StringBuilder builder = new StringBuilder();
            int passiveCount = 0;
            AppendPassiveName(builder, args.ItemIndex0, ref passiveCount);
            AppendPassiveName(builder, args.ItemIndex1, ref passiveCount);
            AppendPassiveName(builder, args.ItemIndex2, ref passiveCount);

            StringBuilder header = new StringBuilder();
            header.AppendFormat(PASSIVES_HEADER_FORMAT, passiveCount, args.SlotCount);
            if (builder.Length > 0)
            {
                header.Append(PASSIVE_NAME_SEPARATOR);
                header.Append(builder);
            }

            _passivesLabel.text = header.ToString();
        }

        void AppendPassiveName(StringBuilder builder, int itemIndex, ref int passiveCount)
        {
            ShopItemData item = null == _catalog ? null : _catalog.GetItem(itemIndex);
            if (null == item)
                return;

            if (builder.Length > 0)
                builder.Append(PASSIVE_NAME_SEPARATOR);

            builder.Append(item.GetDisplayName());
            ++passiveCount;
        }

        Button GetOfferButton(int slotIndex)
        {
            switch (slotIndex)
            {
                case 0:
                    return _offerButton0;
                case 1:
                    return _offerButton1;
                case 2:
                    return _offerButton2;
                default:
                    return null;
            }
        }

        TMP_Text GetOfferLabel(int slotIndex)
        {
            switch (slotIndex)
            {
                case 0:
                    return _offerLabel0;
                case 1:
                    return _offerLabel1;
                case 2:
                    return _offerLabel2;
                default:
                    return null;
            }
        }

        void OnOffer0Clicked()
        {
            UIManager.RaiseEvent(UIManager.EEvent.ShopBuyRequested, 0);
        }

        void OnOffer1Clicked()
        {
            UIManager.RaiseEvent(UIManager.EEvent.ShopBuyRequested, 1);
        }

        void OnOffer2Clicked()
        {
            UIManager.RaiseEvent(UIManager.EEvent.ShopBuyRequested, 2);
        }

        void OnRerollClicked()
        {
            UIManager.RaiseEvent(UIManager.EEvent.ShopRerollRequested);
        }

        void OnStartClicked()
        {
            UIManager.Hide(UIViewCatalogData.EViewType.Shop);
            UIManager.RaiseEvent(UIManager.EEvent.ShopStartRequested);
        }
    }
}
