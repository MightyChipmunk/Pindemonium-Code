using UnityEngine;

namespace Pinball.Ingame.Map
{
    public interface IDamageable
    {
        void TakeHit(float damage, Vector2 worldPoint);
    }
}
