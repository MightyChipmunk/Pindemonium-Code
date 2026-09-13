using UnityEngine;
using GameCore.Util;

namespace Pinball.Ingame
{
    public class BallSpawner : MonoBehaviour
    {
        const int DEFAULT_POOL_CAPACITY = 3;

        [SerializeField] Ball _ballPrefab = null;
        [SerializeField] int _poolCapacity = DEFAULT_POOL_CAPACITY;

        ObjectPool<Ball> _pool = null;
        ObjectRegistry<int, Ball> _colliderRegistry = null;

        public void Init()
        {
            _colliderRegistry = new ObjectRegistry<int, Ball>();
            _pool = new ObjectPool<Ball>(_ballPrefab, CreateBall, _poolCapacity);
        }

        public void Release()
        {
            if (null != _pool)
            {
                _pool.Clear();
                _pool = null;
            }

            if (null != _colliderRegistry)
            {
                _colliderRegistry.Clear();
                _colliderRegistry = null;
            }
        }

        Ball CreateBall(Ball origin)
        {
            return Instantiate(origin, transform);
        }

        public Ball Spawn()
        {
            Ball ball = _pool.Get();
            if (null == ball)
                return null;

            ball.MoveTo(transform.position);
            _colliderRegistry.Register(ball.ColliderInstanceID, ball);
            return ball;
        }

        public void Despawn(Ball ball)
        {
            if (null == ball)
                return;

            _colliderRegistry.Unregister(ball.ColliderInstanceID);
            _pool.Return(ball);
        }

        public bool TryResolve(Collider2D collider, out Ball ball)
        {
            ball = null;
            if (null == collider)
                return false;

            return _colliderRegistry.Find(collider.GetInstanceID(), out ball);
        }
    }
}
