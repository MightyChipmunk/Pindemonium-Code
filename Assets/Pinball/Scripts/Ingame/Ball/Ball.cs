using Pinball.Util;
using UnityEngine;

namespace Pinball.Ingame
{
    public class Ball : MonoBehaviour, IPoolObject
    {
        [SerializeField] Rigidbody2D _rigidbody = null;
        [SerializeField] Collider2D _collider = null;

        public IPoolBase OwnPool { get; set; }
        public int ColliderInstanceID => _collider.GetInstanceID();

        public void SetMass(float mass)
        {
            _rigidbody.mass = mass;
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
        }

        public void AddImpulse(Vector2 impulse)
        {
            _rigidbody.AddForce(impulse, ForceMode2D.Impulse);
        }

        public void OnGenerate() { }

        public void OnGet()
        {
            gameObject.SetActive(true);
        }

        public void OnReturn()
        {
            SetKinematic(true);
        }

        public void OnTerminate() { }
    }
}
