using System;
using System.Collections.Generic;
using ARSurvival.Core;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ARSurvival.Audio
{
    public enum SoundId
    {
        PlayerShoot,
        PlayerHurt,
        PlayerDeath,
        EnemySpawn,
        EnemyShoot,
        MeleeAttack,
        EnemyHit,
        EnemyDeath,
        RoundStart,
        RoundWin,
        RoundLose,
        UIClick,
    }

    /// <summary>
    /// Single owner of all game audio. Uses one looping music source plus a small fixed pool of
    /// SFX "voices" (round-robin), instead of an AudioSource on every enemy and bullet.
    /// Gameplay sounds are triggered by EventBus events, so gameplay code never references audio.
    /// </summary>
    public class AudioManager : Singleton<AudioManager>
    {
        [Serializable]
        class Sound
        {
            public SoundId id;
            public AudioClip[] clips;
            [Range(0f, 1f)] public float volume = 1f;
            public Vector2 pitchRange = new(0.95f, 1.05f);
        }

        [SerializeField] Sound[] sounds;
        [SerializeField] AudioClip music;
        [SerializeField, Range(0f, 1f)] float musicVolume = 0.35f;
        [SerializeField, Range(1, 16)] int voiceCount = 8;

        readonly Dictionary<SoundId, Sound> lookup = new();
        AudioSource musicSource;
        AudioSource[] voices;
        int nextVoice;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this)
                return;

            foreach (var sound in sounds)
                lookup[sound.id] = sound;

            musicSource = CreateSource("Music");
            musicSource.loop = true;
            musicSource.volume = musicVolume;

            voices = new AudioSource[voiceCount];
            for (int i = 0; i < voiceCount; i++)
                voices[i] = CreateSource($"Voice_{i}");
        }

        void Start()
        {
            if (music != null)
            {
                musicSource.clip = music;
                musicSource.Play();
            }
        }

        AudioSource CreateSource(string sourceName)
        {
            var child = new GameObject(sourceName);
            child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f; // AR arena is a few metres wide; 2D keeps every cue audible.
            return source;
        }

        public void Play(SoundId id)
        {
            if (!lookup.TryGetValue(id, out var sound) || sound.clips == null || sound.clips.Length == 0)
                return;

            var clip = sound.clips[Random.Range(0, sound.clips.Length)];
            if (clip == null)
                return;

            var voice = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            voice.pitch = Random.Range(sound.pitchRange.x, sound.pitchRange.y);
            voice.PlayOneShot(clip, sound.volume);
        }

        /// <summary>For UI Button OnClick events in the inspector.</summary>
        public void PlayClick() => Play(SoundId.UIClick);

        // --- Event wiring (Observer) ---

        void OnEnable()
        {
            EventBus<PlayerShotEvent>.Subscribe(OnPlayerShot);
            EventBus<PlayerDamagedEvent>.Subscribe(OnPlayerDamaged);
            EventBus<PlayerDiedEvent>.Subscribe(OnPlayerDied);
            EventBus<EnemySpawnedEvent>.Subscribe(OnEnemySpawned);
            EventBus<EnemyShotEvent>.Subscribe(OnEnemyShot);
            EventBus<MeleeAttackEvent>.Subscribe(OnMeleeAttack);
            EventBus<EnemyDamagedEvent>.Subscribe(OnEnemyDamaged);
            EventBus<EnemyKilledEvent>.Subscribe(OnEnemyKilled);
            EventBus<RoundStartedEvent>.Subscribe(OnRoundStarted);
            EventBus<RoundEndedEvent>.Subscribe(OnRoundEnded);
        }

        void OnDisable()
        {
            EventBus<PlayerShotEvent>.Unsubscribe(OnPlayerShot);
            EventBus<PlayerDamagedEvent>.Unsubscribe(OnPlayerDamaged);
            EventBus<PlayerDiedEvent>.Unsubscribe(OnPlayerDied);
            EventBus<EnemySpawnedEvent>.Unsubscribe(OnEnemySpawned);
            EventBus<EnemyShotEvent>.Unsubscribe(OnEnemyShot);
            EventBus<MeleeAttackEvent>.Unsubscribe(OnMeleeAttack);
            EventBus<EnemyDamagedEvent>.Unsubscribe(OnEnemyDamaged);
            EventBus<EnemyKilledEvent>.Unsubscribe(OnEnemyKilled);
            EventBus<RoundStartedEvent>.Unsubscribe(OnRoundStarted);
            EventBus<RoundEndedEvent>.Unsubscribe(OnRoundEnded);
        }

        void OnPlayerShot(PlayerShotEvent e) => Play(SoundId.PlayerShoot);
        void OnPlayerDamaged(PlayerDamagedEvent e) => Play(SoundId.PlayerHurt);
        void OnPlayerDied(PlayerDiedEvent e) => Play(SoundId.PlayerDeath);
        void OnEnemySpawned(EnemySpawnedEvent e) => Play(SoundId.EnemySpawn);
        void OnEnemyShot(EnemyShotEvent e) => Play(SoundId.EnemyShoot);
        void OnMeleeAttack(MeleeAttackEvent e) => Play(SoundId.MeleeAttack);
        void OnEnemyDamaged(EnemyDamagedEvent e) => Play(SoundId.EnemyHit);
        void OnEnemyKilled(EnemyKilledEvent e) => Play(SoundId.EnemyDeath);
        void OnRoundStarted(RoundStartedEvent e) => Play(SoundId.RoundStart);
        void OnRoundEnded(RoundEndedEvent e) => Play(e.Result.survived ? SoundId.RoundWin : SoundId.RoundLose);
    }
}
