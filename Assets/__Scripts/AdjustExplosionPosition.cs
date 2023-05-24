using UnityEngine;

namespace __Scripts
{
    public class AdjustExplosionPosition : MonoBehaviour
    {
        public float penetrationDistance = 0.1f;

        private Vector3 direction;
        private bool hasCollided;

        private void OnCollisionEnter(Collision collision)
        {
            direction = collision.relativeVelocity.normalized;
            hasCollided = true;
        }

        private void LateUpdate()
        {
            if (hasCollided)
            {
                transform.position += direction * penetrationDistance;
                hasCollided = false;
            }
        }
    }
}
