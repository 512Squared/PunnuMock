using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;


namespace __Scripts
{
    public class AudioController : SingletonMonobehaviour<AudioController>
    {
        [SerializeField] private List<AudioClip> ouchAudioClips;
        [SerializeField] private AudioClip heartbeat;
        [SerializeField] private AudioClip healing;

        public AudioSource generalAudioSource;
        public AudioSource healingAudioSource;

        public AudioSource AudioSource => generalAudioSource;
        public AudioSource HealingAudioSource => healingAudioSource;

        private void Start()
        {
            generalAudioSource = GetComponent<AudioSource>();
            // assuming you've attached a second AudioSource for the healing sound
            healingAudioSource = GetComponents<AudioSource>()[1];
        }

        public AudioClip GetRandomOuch()
        {
            int randomIndex = Random.Range(0, ouchAudioClips.Count);
            return ouchAudioClips[randomIndex];
        }

        public AudioClip GetHeartbeat()
        {
            return heartbeat;
        }

        public AudioClip GetHealing()
        {
            return healing;
        }

        public static IEnumerator FadeInClip(AudioClip clip, float time, AudioSource source, float volume)
        {
            source.clip = clip;
            source.volume = 0;
            source.Play();

            while (source.volume < volume)
            {
                source.volume += Time.deltaTime / time;
                yield return null;
            }
        }

        public static IEnumerator FadeOutClip(float time, AudioSource source)
        {
            while (source.volume > 0)
            {
                source.volume -= Time.deltaTime / time;
                yield return null;
            }

            source.Stop();
        }
    }
}
