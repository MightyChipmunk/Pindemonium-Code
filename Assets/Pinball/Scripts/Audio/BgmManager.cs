using GameCore.Audio;

namespace Pinball.Audio
{
    /// <summary>
    /// 게임 BGM 키(EBgmType)를 GameCore.BgmManager 클립 재생으로 매핑하는 파사드.
    /// </summary>
    public static class BgmManager
    {
        public static void Init(GameCore.Audio.BgmPlayer playerPrefab)
        {
            BgmPlayback.Init(playerPrefab);
        }

        public static void Release()
        {
            BgmPlayback.Release();
        }

        public static void Play(BgmStorage.EBgmType type)
        {
            BgmPlayback.Play(BgmStorage.GetClip(type), BgmStorage.GetVolume(type));
        }

        public static void Stop()
        {
            BgmPlayback.Stop();
        }
    }
}
