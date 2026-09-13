using Cysharp.Threading.Tasks;
using GameCore.Scenes;

namespace Pinball.Core
{
    /// <summary>
    /// 게임 씬 이름과 씬 간 전달 파라미터(이어하기, 플리퍼 선택)를 정한다. 실제 로드는 GameCore.SceneLoader가 맡는다.
    /// </summary>
    public static class SceneFlow
    {
        public const string LOBBY_SCENE_NAME = "MainLobby";
        public const string INGAME_SCENE_NAME = "Ingame";
        public const int DEFAULT_FLIPPER_INDEX = 0;

        static bool _isContinueRequested = false;
        static int _requestedFlipperIndex = DEFAULT_FLIPPER_INDEX;

        public static bool ConsumeContinueRequested()
        {
            bool isContinueRequested = _isContinueRequested;
            _isContinueRequested = false;
            return isContinueRequested;
        }

        public static int ConsumeRequestedFlipperIndex()
        {
            int flipperIndex = _requestedFlipperIndex;
            _requestedFlipperIndex = DEFAULT_FLIPPER_INDEX;
            return flipperIndex;
        }

        public static async UniTask LoadLobbyAsync()
        {
            await SceneLoader.LoadAsync(LOBBY_SCENE_NAME);
        }

        public static async UniTask LoadIngameAsync(bool isContinue, int flipperIndex)
        {
            _isContinueRequested = isContinue;
            _requestedFlipperIndex = flipperIndex;
            await SceneLoader.LoadAsync(INGAME_SCENE_NAME);
        }
    }
}
