using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using DG.Tweening;
using UnityEngine.Serialization;

namespace __Scripts
{
    public class HealthBar : MonoBehaviour
    {
        [PropertySpace(SpaceBefore = 10, SpaceAfter = 10)] 
        [SerializeField] private bool debugOn;
        
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Gradient gradient;
        [SerializeField] private Image fill;
        [SerializeField] private RectTransform heartScale;
        [SerializeField] private Image heartColor;
        [SerializeField] private float currentHealth;
        [SerializeField] private float damageTweenDuration;
        [SerializeField] private Ease damageEaseType;
        [SerializeField] private Ease regenEaseType;
        [SerializeField] private Ease heartbeatEaseType;

        private const int MaxHealth = 100;
        private const int MinHealth = 0;

        private void OnEnable()
        {
            Actions.OnDamageReceived += ReceiveDamage;
            Actions.OnRegeneration += Regeneration;
        }

        private void OnDisable()
        {
            Actions.OnDamageReceived -= ReceiveDamage;
            Actions.OnRegeneration -= Regeneration;
        }

        private void Start()
        {
            SetMaxMinHealth();
            currentHealth = (int) healthSlider.value;
        }

        [Button(ButtonSizes.Large), GUIColor(0.5f,0.5f, 0.9f)]
        private void OnReceiveDamage(int damage)
        {
            Actions.OnDamageReceived?.Invoke(4);
        }
        
        [Button(ButtonSizes.Large), GUIColor(1,0.5f, 0.5f)]
        public void SetMaxMinHealth()
        {
            healthSlider.maxValue = MaxHealth;
            healthSlider.minValue = MinHealth;
            healthSlider.value = MaxHealth;
            currentHealth = MaxHealth;
            fill.color =  gradient.Evaluate(1f);
        }

        public void ReceiveDamage(float damage)
        {
            currentHealth -= damage;
            healthSlider.DOValue(currentHealth, damageTweenDuration).SetEase(damageEaseType);
            fill.color = gradient.Evaluate(healthSlider.normalizedValue);
            Debug.Log($"Damage received: {damage}");
        }

        /// <summary>
        /// This method handles the UI update for regen, while regen logic is handled in the HealthController. This class listens for the OnRegeneration event 
        /// </summary>
        /// <param name="health"></param>
        public void Regeneration(float health)
        {
            currentHealth = health;
            Vector3 finalScale = new Vector3(1.1f, 1.1f, 1f);
            Color initialColor = heartColor.color;
            Color glowColor = new Color(1f, 0.0078f, 0.0078f); // equivalent to #FF0202
            PlayHeartbeat();
            
            DOTween.Sequence()
                .Append(heartColor.DOColor(glowColor, 0.3f).SetEase(Ease.InOutSine))
                .Join(heartScale.DOScale(finalScale, 0.3f).SetEase(heartbeatEaseType))
                .Join(healthSlider.DOValue(health, 0.3f).SetEase(regenEaseType))
                .Append(heartScale.DOScale(new Vector3(0.8f, 0.8f, 0.8f), 0.15f).SetEase(Ease.InExpo)) 
                .Join(heartColor.DOColor(initialColor, 0.15f).SetEase(Ease.InOutSine))
                .OnComplete(() =>
                {
                    fill.color = gradient.Evaluate(healthSlider.normalizedValue);
                    heartScale.localScale = new Vector3 (0.8f, 0.8f, 0.8f);
                });
        }

        private void PlayHeartbeat()
        {
            float healthPercentage = currentHealth / MaxHealth;
            float volume = 1 - healthPercentage + 0.1f;
            volume = Mathf.Clamp(volume, 0f, 1f); // Normalize
            AudioController.Instance.AudioSource.PlayOneShot(AudioController.Instance.GetHeartbeat(), volume);
            Debug.Log($"Current health: {currentHealth}  | MaxHealth: {MaxHealth} | Volume: {volume}");
        }

        
    }
}