using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using GameCore.Util;

namespace Pinball.Core
{
    public class LoadingSceneManager : BehaviourBase
    {
        const float MIN_DISPLAY_SECONDS = 0.5f;

        [SerializeField] GameBootStrapConfig _bootStrapConfig = null;

        void Start()
        {
            BootAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        async UniTaskVoid BootAsync(CancellationToken cancellationToken)
        {
            GameBootStrap.EnsureInit(_bootStrapConfig);
            await UniTask.Delay(TimeSpan.FromSeconds(MIN_DISPLAY_SECONDS), cancellationToken: cancellationToken);
            await SceneFlow.LoadLobbyAsync();
        }
    }
}
