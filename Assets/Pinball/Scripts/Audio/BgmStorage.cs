using GameCore.Audio;
using UnityEngine;

namespace Pinball.Audio
{
    [CreateAssetMenu(fileName = "BgmStorage", menuName = "Pinball/Playable/Sound/BgmStorage")]
    public class BgmStorage : BgmStorageBase<BgmStorage.EBgmType>
    {
        public enum EBgmType
        {
            None = -1,

            Lobby = 0,

            StageSelect = 10,
            Shop = 11,

            PlayEarly = 20,
            PlayLate = 21,
        }

        protected override EBgmType GetNoneKey()
        {
            return EBgmType.None;
        }
    }
}
