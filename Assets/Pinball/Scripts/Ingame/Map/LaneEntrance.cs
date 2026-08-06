using System;
using UnityEngine;

namespace Pinball.Ingame.Map
{
    public class LaneEntrance : MonoBehaviour
    {
        Action<Collider2D> _onEntered = null;

        public void Init(Action<Collider2D> onEntered)
        {
            _onEntered = onEntered;
        }

        public void Release()
        {
            _onEntered = null;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (null == other)
                return;

            _onEntered?.Invoke(other);
        }
    }
}
