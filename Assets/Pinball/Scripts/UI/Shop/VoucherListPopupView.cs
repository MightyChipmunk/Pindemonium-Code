using System.Collections.Generic;
using Pinball.Localization;
using Pinball.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.UI
{
    public class VoucherListPopupView : UIViewBase
    {
        public record Entry(LocalizedTextRef Title, Sprite Icon, LocalizedTextRef Description);

        public record Args(Entry[] Vouchers) : UIViewArgs
        {
            public override UIViewCatalogData.EViewType ViewType => UIViewCatalogData.EViewType.VoucherListPopup;
        }

        const string TITLE_TEXT = "OWNED VOUCHERS";
        const string TITLE_KEY = "ui.voucher_list.title";
        const string EMPTY_TEXT = "NO VOUCHERS OWNED";
        const string EMPTY_KEY = "ui.voucher_list.empty";
        const int CARD_CAPACITY = 12;

        [SerializeField] TMP_Text _titleLabel = null;
        [SerializeField] TMP_Text _emptyLabel = null;
        [SerializeField] GameObject _emptyRoot = null;
        [SerializeField] Button _closeButton = null;
        [SerializeField] Button _backdropButton = null;
        [SerializeField] RectTransform _cardContent = null;
        [SerializeField] VoucherCardView _cardOrigin = null;

        ObjectPool<VoucherCardView> _cardPool = null;
        List<VoucherCardView> _cards = null;

        public override void OnGenerate()
        {
            _cards = new List<VoucherCardView>();
            _cardPool = new ObjectPool<VoucherCardView>(_cardOrigin, CreateCard, CARD_CAPACITY);
            _closeButton.onClick.AddListener(OnCloseClicked);
            _backdropButton.onClick.AddListener(OnCloseClicked);
        }

        public override void OnTerminate()
        {
            _closeButton.onClick.RemoveListener(OnCloseClicked);
            _backdropButton.onClick.RemoveListener(OnCloseClicked);
            ReturnCards();
            _cardPool.Clear();
            _cardPool = null;
            _cards = null;
        }

        public override void OnReturn()
        {
            ReturnCards();
        }

        public override void Bind(UIViewArgs args)
        {
            Args viewArgs = args as Args;
            if (null == viewArgs)
                return;

            _titleLabel.text = LocalizationTable.Get(TITLE_KEY, TITLE_TEXT);
            _emptyLabel.text = LocalizationTable.Get(EMPTY_KEY, EMPTY_TEXT);
            BindCards(viewArgs.Vouchers);
        }

        void BindCards(Entry[] vouchers)
        {
            ReturnCards();

            int voucherCount = null == vouchers ? 0 : vouchers.Length;
            for (int i = 0; i < voucherCount; ++i)
            {
                Entry entry = vouchers[i];
                if (null == entry)
                    continue;

                VoucherCardView card = _cardPool.Get();
                card.transform.SetParent(_cardContent, false);
                card.transform.SetAsLastSibling();
                card.Bind(new VoucherCardView.BindParam(
                    Icon: entry.Icon,
                    Name: entry.Title.Resolve(),
                    Description: entry.Description.Resolve()));
                _cards.Add(card);
            }

            _emptyRoot.SetActive(_cards.Count <= 0);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_cardContent);
        }

        void ReturnCards()
        {
            for (int i = 0; i < _cards.Count; ++i)
                _cardPool.Return(_cards[i]);

            _cards.Clear();
        }

        VoucherCardView CreateCard(VoucherCardView origin)
        {
            return Instantiate(origin, _cardContent);
        }

        void OnCloseClicked()
        {
            UIManager.Hide(UIViewCatalogData.EViewType.VoucherListPopup);
        }
    }
}
