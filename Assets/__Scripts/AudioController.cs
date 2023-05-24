using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace __Scripts
{
    public class AudioController : SingletonMonobehaviour<AudioController>
    {
        [SerializeField] private List<AudioClip> ouchAudioClips;
        [SerializeField] private AudioClip heartbeat;
        [SerializeField] private AudioSource audioSource;
        public AudioSource AudioSource => audioSource;

        private void Start()
        {
            audioSource = GetComponent<AudioSource>();
        }

        public AudioClip GetRandomOuch()
        {
            int randomIndex = Random.Range(0, ouchAudioClips.Count);
            AudioClip randomClip = ouchAudioClips[randomIndex];
            return randomClip;
        }

        public AudioClip GetHeartbeat()
        {
            return heartbeat;
        }
        
    }
}
