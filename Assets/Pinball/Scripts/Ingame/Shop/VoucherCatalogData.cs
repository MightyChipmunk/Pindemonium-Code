using UnityEngine;

namespace Pinball.Ingame.Shop
{
    [CreateAssetMenu(fileName = "VoucherCatalogData", menuName = "Pinball/Ingame/VoucherCatalogData")]
    public class VoucherCatalogData : ScriptableObject
    {
        [SerializeField] VoucherData[] _vouchers = null;

        public int Count => null == _vouchers ? 0 : _vouchers.Length;

        public VoucherData GetVoucher(int index)
        {
            if (index < 0 || index >= Count)
                return null;

            return _vouchers[index];
        }

        public VoucherData FindById(string voucherId)
        {
            if (string.IsNullOrEmpty(voucherId))
                return null;

            for (int i = 0; i < Count; ++i)
                if (null != _vouchers[i] && _vouchers[i].GetId() == voucherId)
                    return _vouchers[i];

            return null;
        }
    }
}
