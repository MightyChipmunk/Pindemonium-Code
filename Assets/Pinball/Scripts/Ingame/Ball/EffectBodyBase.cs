using UnityEngine;
using GameCore.Util;

namespace Pinball.Ingame
{
    public abstract class EffectBodyBase : MonoBehaviour, IPoolObject
    {
        public IPoolBase OwnPool { get; set; }

        public void MoveTo(Vector2 position)
        {
            transform.position = position;
        }

        protected Vector2 GetPosition()
        {
            return transform.position;
        }

        protected void SetRotation(float angleDegree)
        {
            transform.rotation = Quaternion.Euler(0f, 0f, angleDegree);
        }

        public virtual void OnGenerate() { }

        public virtual void OnGet()
        {
            gameObject.SetActive(true);
        }

        public virtual void OnReturn() { }

        public virtual void OnTerminate() { }
    }
}
