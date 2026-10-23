using System;
using UnityEngine;

namespace GameJam2026
{
    public enum GameSound { LightSequenceKey, PasswordKey, PuzzleCompleted, Victory, Warning, Explosion }

    [DisallowMultipleComponent]
    public sealed class SoundManager : MonoBehaviour
    {
        [Serializable]
        private sealed class Sound
        {
            public AudioClip clip = null;
            [Range(0f, 1f)] public float volume = 1f;
        }

        [Header("Teclas da sequencia de luzes")]
        [SerializeField] private Sound lightSequenceKey = new Sound();
        [Header("Teclas da senha (numeros, limpar e confirmar)")]
        [SerializeField] private Sound passwordKey = new Sound();
        [Header("Acerto - conclusao de qualquer minijogo")]
        [SerializeField] private Sound puzzleCompleted = new Sound();
        [Header("Vitoria - conclusao do jogo")]
        [SerializeField] private Sound victory = new Sound();
        [Header("Aviso - ultimo minuto")]
        [SerializeField] private Sound warning = new Sound();
        [Header("Explosao - derrota")]
        [SerializeField] private Sound explosion = new Sound();
        [Header("Musica de fundo (loop)")]
        [SerializeField] private Sound music = new Sound();
        [SerializeField] private bool playMusicOnStart = true;
        [Header("Volumes gerais")]
        [SerializeField, Range(0f, 1f)] private float effectsVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float musicVolume = .5f;

        private static SoundManager instance;
        private AudioSource effectsSource;
        private AudioSource musicSource;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogError("Only one active Sound Manager is allowed. Disabling the duplicate.", this);
                enabled = false;
                return;
            }
            instance = this;
            effectsSource = CreateSource(false);
            musicSource = CreateSource(true);
            ApplyVolumes();
        }

        private AudioSource CreateSource(bool loop)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            return source;
        }

        private void Start()
        {
            if (playMusicOnStart) PlayMusic();
        }

        public static void Play(GameSound sound)
        {
            if (instance == null || !instance.isActiveAndEnabled) return;
            Sound entry;
            switch (sound)
            {
                case GameSound.LightSequenceKey: entry = instance.lightSequenceKey; break;
                case GameSound.PasswordKey: entry = instance.passwordKey; break;
                case GameSound.PuzzleCompleted: entry = instance.puzzleCompleted; break;
                case GameSound.Victory: entry = instance.victory; break;
                case GameSound.Warning: entry = instance.warning; break;
                case GameSound.Explosion: entry = instance.explosion; break;
                default: return;
            }
            if (entry.clip != null)
                instance.effectsSource.PlayOneShot(entry.clip, entry.volume);
        }

        public void PlayMusic()
        {
            if (!isActiveAndEnabled || musicSource == null || music.clip == null) return;
            if (musicSource.isPlaying && musicSource.clip == music.clip) return;
            musicSource.clip = music.clip;
            ApplyVolumes();
            musicSource.Play();
        }

        public void StopMusic()
        {
            if (musicSource != null) musicSource.Stop();
        }

        public static void StopBackgroundMusic()
        {
            if (instance != null) instance.StopMusic();
        }

        private void ApplyVolumes()
        {
            if (effectsSource != null) effectsSource.volume = effectsVolume;
            if (musicSource != null) musicSource.volume = musicVolume * music.volume;
        }

        private void OnValidate() => ApplyVolumes();

        private void OnDisable()
        {
            if (effectsSource != null) effectsSource.Stop();
            StopMusic();
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
