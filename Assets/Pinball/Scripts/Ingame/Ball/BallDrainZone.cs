using System;
using Pinball.Util;
using UnityEngine;

namespace Pinball.Ingame
{
    public class BallDrainZone : MonoBehaviour
    {
        [SerializeField] LayerMask _ballLayerMask = default;

        public event Action<Collider2D> OnColliderEntered;

        public void Init()
        {
            OnColliderEntered = null;
        }

        public void Release()
        {
            OnColliderEntered = null;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (null == other)
                return;

            if (!_ballLayerMask.HasLayer(other.gameObject.layer))
                return;

            OnColliderEntered?.Invoke(other);
        }
    }
}
