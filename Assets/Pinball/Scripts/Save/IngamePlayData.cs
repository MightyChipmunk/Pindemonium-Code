using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Pinball.Save
{
    [Serializable]
    public class IngamePlayData : SaveDataBase
    {
        [Serializable]
        public struct UpgradeEntry
        {
            [SerializeField, FormerlySerializedAs("Id")] string _id;
            [SerializeField, FormerlySerializedAs("Count")] int _count;

            public string Id => _id;
            public int Count => _count;

            public UpgradeEntry(string id, int count)
            {
                _id = id;
                _count = count;
            }
        }

        [SerializeField, FormerlySerializedAs("StageNumber")] int _stageNumber = 1;
        [SerializeField] bool _isEndless = false;
        [SerializeField, FormerlySerializedAs("IsShopPending")] bool _isShopPending = false;
        [SerializeField] bool _isRoundRewardPending = false;
        [SerializeField, FormerlySerializedAs("Money")] int _money = 0;
        [SerializeField, FormerlySerializedAs("TotalScore")] long _totalScore = 0;
        [SerializeField, FormerlySerializedAs("GimmickIndices")] int[] _gimmickIndices = null;
        [SerializeField, FormerlySerializedAs("FlipperId")] string _flipperId = string.Empty;
        [SerializeField, FormerlySerializedAs("PassiveIds")] string[] _passiveIds = null;
        [SerializeField, FormerlySerializedAs("Upgrades")] UpgradeEntry[] _upgrades = null;
        [SerializeField] string[] _voucherIds = null;
        [SerializeField] string[] _voucherOfferedIds = null;
        [SerializeField] string _voucherOfferId = string.Empty;
        [SerializeField] int _voucherOfferRound = -1;
        [SerializeField] int _voucherPurchasedRound = -1;
        [SerializeField] string[] _shopOfferIds = null;
        [SerializeField] string[] _shopPackIds = null;
        [SerializeField] int _shopRerollCount = 0;
        [SerializeField] int _slingshotHitCount = 0;
        [SerializeField] long[] _gimmickScores = null;

        public int StageNumber => _stageNumber;
        public bool IsEndless => _isEndless;
        public bool IsShopPending => _isShopPending;
        public bool IsRoundRewardPending => _isRoundRewardPending;
        public string VoucherOfferId => _voucherOfferId;
        public int VoucherOfferRound => _voucherOfferRound;
        public int VoucherPurchasedRound => _voucherPurchasedRound;
        public int Money => _money;
        public long TotalScore => _totalScore;
        public int ShopRerollCount => _shopRerollCount;
        public int SlingshotHitCount => _slingshotHitCount;
        public string FlipperId => _flipperId;

        public IngamePlayData() { }

        public IngamePlayData(int stageNumber, bool isEndless, bool isShopPending, bool isRoundRewardPending, int money, long totalScore, int[] gimmickIndices, string flipperId, string[] passiveIds, UpgradeEntry[] upgrades, string[] voucherIds, string[] voucherOfferedIds, string voucherOfferId, int voucherOfferRound, int voucherPurchasedRound, string[] shopOfferIds, string[] shopPackIds, int shopRerollCount, int slingshotHitCount, long[] gimmickScores)
        {
            _stageNumber = stageNumber;
            _isEndless = isEndless;
            _isShopPending = isShopPending;
            _isRoundRewardPending = isRoundRewardPending;
            _money = money;
            _totalScore = totalScore;
            _gimmickIndices = gimmickIndices;
            _flipperId = flipperId;
            _passiveIds = passiveIds;
            _upgrades = upgrades;
            _voucherIds = voucherIds;
            _voucherOfferedIds = voucherOfferedIds;
            _voucherOfferId = voucherOfferId;
            _voucherOfferRound = voucherOfferRound;
            _voucherPurchasedRound = voucherPurchasedRound;
            _shopOfferIds = shopOfferIds;
            _shopPackIds = shopPackIds;
            _shopRerollCount = shopRerollCount;
            _slingshotHitCount = slingshotHitCount;
            _gimmickScores = gimmickScores;
        }

        public int[] GetGimmickIndices()
        {
            return _gimmickIndices;
        }

        public long[] GetGimmickScores()
        {
            return _gimmickScores;
        }

        public string[] GetPassiveIds()
        {
            return _passiveIds;
        }

        public UpgradeEntry[] GetUpgrades()
        {
            return _upgrades;
        }

        public string[] GetVoucherIds()
        {
            return _voucherIds;
        }

        public string[] GetVoucherOfferedIds()
        {
            return _voucherOfferedIds;
        }

        public string[] GetShopOfferIds()
        {
            return _shopOfferIds;
        }

        public string[] GetShopPackIds()
        {
            return _shopPackIds;
        }
    }
}
