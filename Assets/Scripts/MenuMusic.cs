using UnityEngine;

namespace GameJam2026
{
    [DisallowMultipleComponent]
    public sealed class MenuMusic : MonoBehaviour
    {
        [SerializeField] private AudioClip music;
        [SerializeField, Min(0f)] private float loopStartSeconds = 25.6f;
        [SerializeField, Range(0f, 1f)] private float volume = .5f;

        private AudioSource introSource;
        private AudioSource loopSource;
        private AudioClip loopClip;

        private void Awake()
        {
            introSource = CreateSource(false);
            loopSource = CreateSource(true);
        }

        private AudioSource CreateSource(bool loop)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.volume = volume;
            return source;
        }

        private void OnEnable()
        {
            if (music == null) return;
            int startSample = Mathf.RoundToInt(loopStartSeconds * music.frequency);
            if (startSample < 0 || startSample >= music.samples)
            {
                Debug.LogError("Menu music loop start must be inside the clip.", this);
                return;
            }

            if (loopClip == null)
            {
                // Decompress On Load is required so GetData can read decoded samples.
                var samples = new float[(music.samples - startSample) * music.channels];
                if (!music.GetData(samples, startSample))
                {
                    Debug.LogError("Menu music must use Decompress On Load.", this);
                    return;
                }
                loopClip = AudioClip.Create(music.name + " loop", music.samples - startSample,
                    music.channels, music.frequency, false);
                loopClip.SetData(samples, 0);
            }

            introSource.clip = music;
            loopSource.clip = loopClip;
            double startTime = AudioSettings.dspTime + .1;
            introSource.PlayScheduled(startTime);
            loopSource.PlayScheduled(startTime + (double)music.samples / music.frequency);
        }

        private void OnDisable()
        {
            introSource.Stop();
            loopSource.Stop();
        }

        private void OnDestroy()
        {
            if (loopClip != null) Destroy(loopClip);
        }
    }
}
