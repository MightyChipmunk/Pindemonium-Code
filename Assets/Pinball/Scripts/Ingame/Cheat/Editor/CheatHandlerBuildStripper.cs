using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pinball.Ingame.Cheat.Editor
{
    public class CheatHandlerBuildStripper : IProcessSceneWithReport
    {
        const string STRIPPED_FORMAT = "[Build] 릴리스 빌드라 씬 '{0}'에서 {1}를 제거했습니다.";

        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (null == report)
                return;

            if ((report.summary.options & BuildOptions.Development) != 0)
                return;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                IngameCheatHandler[] handlers = root.GetComponentsInChildren<IngameCheatHandler>(true);
                foreach (IngameCheatHandler handler in handlers)
                {
                    Debug.Log(string.Format(STRIPPED_FORMAT, scene.name, handler.gameObject.name));
                    Object.DestroyImmediate(handler.gameObject);
                }
            }
        }
    }
}
