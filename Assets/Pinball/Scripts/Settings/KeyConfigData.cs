using UnityEngine;
using UnityEngine.InputSystem;

namespace Pinball.Settings
{
    [CreateAssetMenu(fileName = "KeyConfigData", menuName = "Pinball/Settings/KeyConfigData")]
    public class KeyConfigData : ScriptableObject
    {
        [SerializeField] Key _flipperLeftKey = Key.LeftArrow;
        [SerializeField] Key _flipperRightKey = Key.RightArrow;
        [SerializeField] Key _launchKey = Key.Space;
        [SerializeField] Key _nudgeKey = Key.LeftShift;

        public Key GetKey(EKeyAction action)
        {
            switch (action)
            {
                case EKeyAction.FlipperLeft:
                    return _flipperLeftKey;
                case EKeyAction.FlipperRight:
                    return _flipperRightKey;
                case EKeyAction.Launch:
                    return _launchKey;
                case EKeyAction.Nudge:
                    return _nudgeKey;
                default:
                    return Key.None;
            }
        }
    }
}
