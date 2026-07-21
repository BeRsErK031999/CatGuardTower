using UnityEngine;

namespace CatGuard.Core.Audio
{
    public static class ProceduralAudioService
    {
        private static AudioSource sfxSource;
        private static AudioSource musicSource;
        private static AudioClip musicLoop;
        private static bool muted;

        public static bool IsMuted => muted;

        public static void Initialize(bool startMuted)
        {
            muted = startMuted;
            EnsureSources();
            ApplyMute();
            EnsureMusic();
        }

        public static void SetMuted(bool isMuted)
        {
            muted = isMuted;
            EnsureSources();
            ApplyMute();
        }

        public static void EnsureMusic()
        {
            EnsureSources();
            if (musicLoop == null)
            {
                musicLoop = CreateMusicLoop();
            }

            musicSource.clip = musicLoop;
            musicSource.loop = true;
            if (!musicSource.isPlaying)
            {
                musicSource.Play();
            }
        }

        public static void Play(ProceduralSoundId soundId)
        {
            if (muted)
            {
                return;
            }

            EnsureSources();
            var clip = soundId switch
            {
                ProceduralSoundId.MenuClick => CreateTone("MenuClick", 520f, 0.07f, 0.18f),
                ProceduralSoundId.TowerPlaced => CreateTone("TowerPlaced", 660f, 0.10f, 0.22f),
                ProceduralSoundId.TowerShot => CreateTone("TowerShot", 880f, 0.05f, 0.16f),
                ProceduralSoundId.EnemyDefeated => CreateTone("EnemyDefeated", 360f, 0.12f, 0.22f),
                ProceduralSoundId.BaseHit => CreateTone("BaseHit", 150f, 0.16f, 0.28f),
                ProceduralSoundId.UltimateCast => CreateArpeggio("UltimateCast", new[] { 392f, 523.25f, 783.99f }, 0.28f, 0.2f),
                ProceduralSoundId.Victory => CreateArpeggio("Victory", new[] { 523.25f, 659.25f, 783.99f }, 0.36f, 0.22f),
                ProceduralSoundId.Defeat => CreateArpeggio("Defeat", new[] { 349.23f, 293.66f, 220f }, 0.42f, 0.22f),
                _ => null
            };

            if (clip != null)
            {
                sfxSource.PlayOneShot(clip);
            }
        }

        private static void EnsureSources()
        {
            if (sfxSource != null && musicSource != null)
            {
                return;
            }

            var audioObject = GameObject.Find("ProceduralAudioService");
            if (audioObject == null)
            {
                audioObject = new GameObject("ProceduralAudioService");
                if (Application.isPlaying)
                {
                    UnityEngine.Object.DontDestroyOnLoad(audioObject);
                }
            }

            sfxSource = audioObject.GetComponent<AudioSource>();
            if (sfxSource == null)
            {
                sfxSource = audioObject.AddComponent<AudioSource>();
            }

            var musicObject = GameObject.Find("ProceduralMusicSource");
            if (musicObject == null)
            {
                musicObject = new GameObject("ProceduralMusicSource");
                musicObject.transform.SetParent(audioObject.transform, false);
            }

            musicSource = musicObject.GetComponent<AudioSource>();
            if (musicSource == null)
            {
                musicSource = musicObject.AddComponent<AudioSource>();
            }

            sfxSource.playOnAwake = false;
            sfxSource.volume = 0.55f;
            musicSource.playOnAwake = false;
            musicSource.volume = 0.13f;
        }

        private static void ApplyMute()
        {
            if (sfxSource != null)
            {
                sfxSource.mute = muted;
            }

            if (musicSource != null)
            {
                musicSource.mute = muted;
            }
        }

        private static AudioClip CreateTone(string clipName, float frequency, float duration, float volume)
        {
            var sampleRate = GetSampleRate();
            var samples = Mathf.CeilToInt(sampleRate * duration);
            var data = new float[samples];

            for (var index = 0; index < samples; index++)
            {
                var t = (float)index / sampleRate;
                var envelope = 1f - (index / (float)samples);
                data[index] = Mathf.Sin(2f * Mathf.PI * frequency * t) * volume * envelope;
            }

            var clip = AudioClip.Create(clipName, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateArpeggio(string clipName, float[] frequencies, float duration, float volume)
        {
            var sampleRate = GetSampleRate();
            var samples = Mathf.CeilToInt(sampleRate * duration);
            var data = new float[samples];
            var segmentSamples = Mathf.Max(1, samples / frequencies.Length);

            for (var index = 0; index < samples; index++)
            {
                var segment = Mathf.Min(frequencies.Length - 1, index / segmentSamples);
                var t = (float)index / sampleRate;
                var envelope = 1f - (index / (float)samples);
                data[index] = Mathf.Sin(2f * Mathf.PI * frequencies[segment] * t) * volume * envelope;
            }

            var clip = AudioClip.Create(clipName, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateMusicLoop()
        {
            var sampleRate = GetSampleRate();
            var duration = 4f;
            var samples = Mathf.CeilToInt(sampleRate * duration);
            var data = new float[samples];

            for (var index = 0; index < samples; index++)
            {
                var t = (float)index / sampleRate;
                var pulse = Mathf.Sin(2f * Mathf.PI * 130.81f * t) * 0.08f;
                var shimmer = Mathf.Sin(2f * Mathf.PI * 261.63f * t) * 0.04f;
                var breath = 0.65f + (Mathf.Sin(2f * Mathf.PI * 0.5f * t) * 0.15f);
                data[index] = (pulse + shimmer) * breath;
            }

            var clip = AudioClip.Create("ProceduralSoftLoop", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static int GetSampleRate()
        {
            return AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 44100;
        }
    }
}
