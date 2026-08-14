using Pinball.Util;
using UnityEngine;

namespace Pinball.Ingame
{
    public class Ball : MonoBehaviour, IPoolObject
    {
        [SerializeField] Rigidbody2D _rigidbody = null;
        [SerializeField] Collider2D _collider = null;
        [SerializeField] BallTrail _trail = null;
        [SerializeField] BallAuraController _auraController = null;

        public IPoolBase OwnPool { get; set; }
        public int ColliderInstanceID => _collider.GetInstanceID();

        public void SetMass(float mass)
        {
            _rigidbody.mass = mass;
        }

        public void SetAura(BallAura.EAuraType auraType, bool isActive)
        {
            _auraController.SetAuraActive(auraType, isActive);
            RefreshTrailColor();
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
            _rigidbody.position = position;
            transform.position = position;
            _trail.Clear();
            _auraController.ClearParticles();
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
            _auraController.Init();
        }

        public void OnReturn()
        {
            SetKinematic(true);
            _auraController.Release();
            _trail.Release();
        }

        public void OnTerminate() { }

        void RefreshTrailColor()
        {
            if (_auraController.TryGetBlendedColor(out Color blendedColor))
            {
                _trail.SetColor(blendedColor);
                return;
            }

            _trail.ResetColor();
        }
    }
}
