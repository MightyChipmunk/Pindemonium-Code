using System;
using Pinball.Util;
using UnityEngine;

namespace Pinball.Ingame
{
    public class Ball : MonoBehaviour, IPoolObject
    {
        [SerializeField] Rigidbody2D _rigidbody = null;
        [SerializeField] Collider2D _collider = null;
        [SerializeField] BallTrail _trail = null;

        Action _onCollided = null;

        public IPoolBase OwnPool { get; set; }
        public int ColliderInstanceID => _collider.GetInstanceID();

        public void SetCollisionCallback(Action onCollided)
        {
            _onCollided = onCollided;
        }

        public void SetMass(float mass)
        {
            _rigidbody.mass = mass;
        }

        public void SetTrailColor(Color color)
        {
            _trail.SetColor(color);
        }

        public void SetKinematic(bool isKinematic)
        {
            if (isKinematic)
            {
                _rigidbody.linearVelocity = Vector2.zero;
                _rigidbody.angularVelocity = 0f;
                _rigidbody.bodyType = RigidbodyType2D.Kinematic;
            }
            else
            {
                _rigidbody.bodyType = RigidbodyType2D.Dynamic;
            }
        }

        public void MoveTo(Vector3 position)
        {
            transform.position = position;
            _trail.Clear();
        }

        public void AddImpulse(Vector2 impulse)
        {
            _rigidbody.AddForce(impulse, ForceMode2D.Impulse);
        }

        public void OnGenerate() { }

        public void OnGet()
        {
            gameObject.SetActive(true);
            _trail.Init();
        }

        public void OnReturn()
        {
            SetKinematic(true);
            _trail.Release();
            _onCollided = null;
        }

        public void OnTerminate()
        {
            _onCollided = null;
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            _onCollided?.Invoke();
        }
    }
}
