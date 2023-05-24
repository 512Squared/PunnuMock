using System;
using System.Collections;
using Cinemachine;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace __Scripts
{
    public class HealthController : MonoBehaviour, IDamageable
    {
        [PropertySpace(SpaceBefore = 10, SpaceAfter = 10)] [SerializeField]
        private bool debugOn;
        [Title("Stats")]
        [SerializeField] private float health = 100;
        [SerializeField] private float maxHealth = 100;
        
        [Title("Settings")]
        [SerializeField] private bool canRegenerate;
        [ShowIf("canRegenerate")] 
        public bool regenInHealingCircleOnly;

        
        [ShowIf("canRegenerate")]
        [SerializeField][PropertySpace(SpaceBefore = 0, SpaceAfter = 10)] private float regenSpeed = 2;
        [SerializeField] private Image bloodSplatter;
        [SerializeField] private float splatterAlphaMax = 0.5f;
        public CinemachineImpulseSource cameraImpulse;
        
        [Title("Info only")]
        [ShowIf("canRegenerate")]
        [SerializeField]
        private bool isRegenerating;
        
        private Coroutine regenCoroutine;


        private void Start()
        {
            bloodSplatter = GameObject.FindGameObjectWithTag("DamageEffect").GetComponent<Image>();
            if (cameraImpulse) Debug.Log($"Camera impulse found");
        }

        private void Update()
        {
            if (!canRegenerate || isRegenerating || health >= maxHealth || regenInHealingCircleOnly) return;

            foreach (Turret turret in TurretsManager.Instance.GetTurretList())
                if (turret.CanAttackTarget())
                    return; // stops regen when being attacked

            StartCoroutine(HealthRegeneration());
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("RegenCircle"))
            {
                if (regenInHealingCircleOnly && regenCoroutine == null)
                {
                    regenCoroutine = StartCoroutine(HealthRegeneration());
                    StartCoroutine(AudioController.FadeInClip(AudioController.Instance.GetHealing(), 0.3f, AudioController.Instance.healingAudioSource, 0.162f));
                    Debug.Log($"Regen has begun");
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("RegenCircle")) 
            {
                if(regenInHealingCircleOnly && regenCoroutine != null)
                {
                    StopCoroutine(regenCoroutine);
                    StartCoroutine(AudioController.FadeOutClip(7f, AudioController.Instance.healingAudioSource));
                    regenCoroutine = null;
                    Debug.Log($"Regen has ended");
                }
                
            }
        }
        

        /// <summary>
        /// Implements the IDamageable interface, but also invokes a system event to update the UI and health bars. Sends an 'hit' impulse to the camera (some juice)
        /// </summary>
        /// <param name="damage"></param>
        public void TakeDamage(int damage)
        {
            Debug.Log($"Take DAMAGE called {damage}");
            if (health >= 0) UpdateDamageEffect();
            health -= damage;
            if (health < 0) health = 0;
            AudioClip ouch = AudioController.Instance.GetRandomOuch();
            AudioController.Instance.AudioSource.PlayOneShot(ouch);
            cameraImpulse.GenerateImpulse();
            Actions.OnDamageReceived?.Invoke(damage);
        }

        /// <summary>
        /// Health regeneration - implements if 'canRegenerate' is true. 10% every 3 seconds. TODO: add values to set perc and time via Inspector
        /// </summary>
        /// <returns></returns>
        private IEnumerator HealthRegeneration()
        {
            isRegenerating = true;
            while (health < maxHealth)
            {
                yield return new WaitForSeconds(regenSpeed);
                health += maxHealth * 0.1f;
                health = Mathf.Min(health, maxHealth);
                Actions.OnRegeneration?.Invoke(health);
                UpdateRegenEffect();
            }

            isRegenerating = false;
        }


        /// <summary>
        /// A bloodSplatter effect on the camera helps the player to register hits and to give an additional visual feedback on health (might be a bit outdated :D)
        /// </summary>
        private void UpdateDamageEffect()
        {
            bloodSplatter.DOKill();
            Color initialColor = bloodSplatter.color;
            initialColor.a = splatterAlphaMax;
            bloodSplatter.color = initialColor;
            float finalAlpha = splatterAlphaMax - splatterAlphaMax * health / maxHealth;
            bloodSplatter.DOColor(new Color(bloodSplatter.color.r, bloodSplatter.color.g, bloodSplatter.color.b, finalAlpha), 0.5f);
        }

        /// <summary>
        /// When canRegenerate is true, an reducing bloodSplatter effect is needed to sync with the regen
        /// </summary>
        private void UpdateRegenEffect()
        {
            Color splatterAlpha = bloodSplatter.color;
            splatterAlpha.a = splatterAlphaMax - health / maxHealth;
            if (DOTween.IsTweening(bloodSplatter)) DOTween.Kill(bloodSplatter);
            bloodSplatter.DOFade(splatterAlpha.a, 0.5f);
        }

    }
}
