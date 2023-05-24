using UnityEngine;

namespace __Scripts
{
    public class Projectile : MonoBehaviour
    {
        private new ParticleSystem particleSystem;
        [SerializeField] private int damageAmount;


        private void Start()
        {
            particleSystem = GetComponent<ParticleSystem>();
        }

        private void OnParticleCollision(GameObject other)
        {
            IDamageable damageableComponent = other.GetComponent<IDamageable>();
            damageableComponent?.TakeDamage(damageAmount);
        }

        public void SetDamageAmount(int newDamage)
        {
            damageAmount = newDamage;
        }
    }
}

