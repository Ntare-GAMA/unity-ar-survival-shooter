using System;
using System.IO;
using ARSurvival.Audio;
using UnityEditor;
using UnityEngine;

namespace ARSurvival.EditorTools
{
    /// <summary>
    /// Procedurally synthesises every sound effect as 16-bit mono WAVs in Assets/Audio/SFX and
    /// assigns them, plus the background music, to the scene's AudioManager. All sound effects are
    /// original; the music is "Underwater" by Moodmode from Pixabay (Pixabay Content License).
    /// </summary>
    public static class SoundGenerator
    {
        const int SampleRate = 44100;
        const string SfxFolder = "Assets/Audio/SFX";
        const string MusicPath = "Assets/Underwater music/moodmode-underwater-309016.mp3";

        static System.Random s_Random;

        [MenuItem("Tools/AR Survival/Generate Sounds")]
        public static void GenerateAndAssign()
        {
            s_Random = new System.Random(1234); // deterministic output
            Directory.CreateDirectory(SfxFolder);

            Write(SoundId.PlayerShoot, Laser(1400f, 380f, 0.11f, square: true));
            Write(SoundId.EnemyShoot, Laser(620f, 140f, 0.2f, square: false));
            Write(SoundId.PlayerHurt, Thud(200f, 70f, 0.18f, noise: 0.5f));
            Write(SoundId.MeleeAttack, Thud(140f, 45f, 0.16f, noise: 0.8f));
            Write(SoundId.EnemyHit, Tick());
            Write(SoundId.EnemyDeath, Explosion(0.35f));
            Write(SoundId.EnemySpawn, SpawnWhoosh());
            Write(SoundId.PlayerDeath, DeathFall());
            Write(SoundId.RoundStart, Arpeggio(new[] { 523.25f, 659.25f, 783.99f }, 0.12f));
            Write(SoundId.RoundWin, Arpeggio(new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.16f));
            Write(SoundId.RoundLose, Arpeggio(new[] { 392f, 311.13f, 261.63f, 196f }, 0.2f));
            Write(SoundId.UIClick, Click());
            AssetDatabase.DeleteAsset("Assets/Audio/Music"); // retired generated loop, replaced by the Pixabay track

            AssetDatabase.Refresh();
            ConfigureImporters();
            AssignToAudioManager();
            Debug.Log("[AR Survival] Sounds generated and assigned.");
        }

        // --- Sound designs ---

        static float[] Laser(float from, float to, float duration, bool square)
        {
            double phase = 0;
            return Render(duration, t =>
            {
                float p = t / duration;
                float freq = Mathf.Lerp(from, to, Mathf.Sqrt(p));
                phase += freq / SampleRate;
                float wave = square ? Mathf.Sign(Mathf.Sin((float)(phase * 2 * Math.PI))) * 0.6f
                                    : (float)(2 * (phase % 1.0) - 1); // saw
                return wave * Mathf.Exp(-5f * p);
            });
        }

        static float[] Thud(float from, float to, float duration, float noise)
        {
            double phase = 0;
            float lp = 0f;
            return Render(duration, t =>
            {
                float p = t / duration;
                phase += Mathf.Lerp(from, to, p) / SampleRate;
                lp = LowPass(lp, Noise(), 0.15f);
                return (Mathf.Sin((float)(phase * 2 * Math.PI)) + lp * noise * 3f) * Mathf.Exp(-7f * p);
            });
        }

        static float[] Tick()
        {
            const float duration = 0.06f;
            return Render(duration, t =>
            {
                float p = t / duration;
                return (Mathf.Sin(t * 2f * Mathf.PI * 950f) * 0.7f + Noise() * 0.3f) * Mathf.Exp(-9f * p);
            });
        }

        static float[] Explosion(float duration)
        {
            double phase = 0;
            float lp = 0f;
            return Render(duration, t =>
            {
                float p = t / duration;
                lp = LowPass(lp, Noise(), Mathf.Lerp(0.5f, 0.03f, p));
                phase += Mathf.Lerp(320f, 60f, p) / SampleRate;
                float body = Mathf.Sin((float)(phase * 2 * Math.PI)) * 0.5f;
                return (lp * 2.5f + body) * Mathf.Exp(-4f * p);
            });
        }

        static float[] SpawnWhoosh()
        {
            const float duration = 0.45f;
            double phase = 0;
            float lp = 0f;
            return Render(duration, t =>
            {
                float p = t / duration;
                float envelope = Mathf.Sin(p * Mathf.PI); // swell in and out
                lp = LowPass(lp, Noise(), Mathf.Lerp(0.02f, 0.25f, p));
                phase += Mathf.Lerp(180f, 720f, p * p) / SampleRate;
                return (lp * 2f + Mathf.Sin((float)(phase * 2 * Math.PI)) * 0.35f) * envelope;
            });
        }

        static float[] DeathFall()
        {
            const float duration = 1.1f;
            double phase = 0;
            float lp = 0f;
            return Render(duration, t =>
            {
                float p = t / duration;
                float vibrato = 1f + 0.04f * Mathf.Sin(t * 2f * Mathf.PI * 9f);
                phase += Mathf.Lerp(440f, 55f, p) * vibrato / SampleRate;
                float saw = (float)(2 * (phase % 1.0) - 1);
                lp = LowPass(lp, saw, 0.2f);
                return lp * (1f - p) * 1.3f;
            });
        }

        static float[] Arpeggio(float[] notes, float noteLength)
        {
            float duration = noteLength * notes.Length + 0.25f;
            return Render(duration, t =>
            {
                int index = Mathf.Min((int)(t / noteLength), notes.Length - 1);
                float local = t - index * noteLength;
                float tail = index == notes.Length - 1 ? 0.25f + noteLength : noteLength;
                float envelope = Mathf.Exp(-3.5f * local / tail);
                float s = Mathf.Sin(t * 2f * Mathf.PI * notes[index]);
                return (s + 0.3f * Mathf.Sign(s)) * 0.6f * envelope; // soft square-ish chime
            });
        }

        static float[] Click()
        {
            const float duration = 0.035f;
            return Render(duration, t => Mathf.Sin(t * 2f * Mathf.PI * 1600f) * Mathf.Exp(-12f * t / duration));
        }

        // --- DSP helpers ---

        static float[] Render(float duration, Func<float, float> generator, bool fade = true)
        {
            int count = Mathf.CeilToInt(duration * SampleRate);
            var samples = new float[count];
            float peak = 0.0001f;
            for (int i = 0; i < count; i++)
            {
                samples[i] = generator((float)i / SampleRate);
                peak = Mathf.Max(peak, Mathf.Abs(samples[i]));
            }

            float gain = 0.9f / peak; // normalise
            int fadeSamples = fade ? Mathf.Min(count / 10, 220) : 0; // ~5 ms de-click at the end
            for (int i = 0; i < count; i++)
            {
                float edge = fadeSamples > 0 && i > count - fadeSamples ? (float)(count - i) / fadeSamples : 1f;
                samples[i] *= gain * edge;
            }
            return samples;
        }

        static float Noise() => (float)(s_Random.NextDouble() * 2.0 - 1.0);

        static float LowPass(float previous, float input, float amount) => previous + (input - previous) * amount;

        // --- Files and wiring ---

        static void Write(SoundId id, float[] samples) => WriteWav($"{SfxFolder}/{id}.wav", samples);

        static void WriteWav(string path, float[] samples)
        {
            using var stream = new FileStream(path, FileMode.Create);
            using var writer = new BinaryWriter(stream);
            int dataSize = samples.Length * 2;
            writer.Write("RIFF".ToCharArray());
            writer.Write(36 + dataSize);
            writer.Write("WAVE".ToCharArray());
            writer.Write("fmt ".ToCharArray());
            writer.Write(16);
            writer.Write((short)1);          // PCM
            writer.Write((short)1);          // mono
            writer.Write(SampleRate);
            writer.Write(SampleRate * 2);    // byte rate
            writer.Write((short)2);          // block align
            writer.Write((short)16);         // bits per sample
            writer.Write("data".ToCharArray());
            writer.Write(dataSize);
            foreach (var s in samples)
                writer.Write((short)(Mathf.Clamp(s, -1f, 1f) * short.MaxValue));
        }

        static void ConfigureImporters()
        {
            foreach (SoundId id in Enum.GetValues(typeof(SoundId)))
            {
                var importer = (AudioImporter)AssetImporter.GetAtPath($"{SfxFolder}/{id}.wav");
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad; // short, played often: no decode cost at play time
                settings.compressionFormat = AudioCompressionFormat.ADPCM;
                importer.defaultSampleSettings = settings;
                importer.forceToMono = true;
                importer.SaveAndReimport();
            }

            var music = (AudioImporter)AssetImporter.GetAtPath(MusicPath);
            var musicSettings = music.defaultSampleSettings;
            musicSettings.loadType = AudioClipLoadType.Streaming; // long clip: stream instead of holding it in memory
            musicSettings.compressionFormat = AudioCompressionFormat.Vorbis;
            musicSettings.quality = 0.45f; // background ambience; keeps the APK small
            music.defaultSampleSettings = musicSettings;
            music.SaveAndReimport();
        }

        static void AssignToAudioManager()
        {
            var manager = UnityEngine.Object.FindAnyObjectByType<AudioManager>();
            if (manager == null)
            {
                Debug.LogWarning("No AudioManager in the open scene; sounds generated but not assigned.");
                return;
            }

            var so = new SerializedObject(manager);
            var sounds = so.FindProperty("sounds");
            for (int i = 0; i < sounds.arraySize; i++)
            {
                var entry = sounds.GetArrayElementAtIndex(i);
                var id = (SoundId)entry.FindPropertyRelative("id").enumValueIndex;
                var clips = entry.FindPropertyRelative("clips");
                clips.arraySize = 1;
                clips.GetArrayElementAtIndex(0).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<AudioClip>($"{SfxFolder}/{id}.wav");
                entry.FindPropertyRelative("volume").floatValue = VolumeFor(id);
            }
            so.FindProperty("music").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(MusicPath);
            so.ApplyModifiedPropertiesWithoutUndo();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        }

        /// <summary>Mix: frequent sounds quieter so they don't mask important cues.</summary>
        static float VolumeFor(SoundId id) => id switch
        {
            SoundId.PlayerShoot => 0.35f,
            SoundId.EnemyShoot => 0.5f,
            SoundId.EnemyHit => 0.45f,
            SoundId.UIClick => 0.6f,
            SoundId.EnemySpawn => 0.6f,
            _ => 0.9f,
        };

        /// <summary>Batch-mode entry point: open the game scene, generate, assign and save.</summary>
        public static void GenerateInScene()
        {
            ProjectPaths.OpenGameScene();
            GenerateAndAssign();
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        }
    }
}
