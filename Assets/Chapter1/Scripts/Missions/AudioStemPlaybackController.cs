using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    public sealed class AudioStemPlaybackController : MonoBehaviour
    {
        [SerializeField] private AudioStemFader[] faders = new AudioStemFader[0];
        [SerializeField, Min(0.01f)] private double scheduleLeadSeconds = 0.08d;

        private bool isPlaying;
        private double lastScheduledDspStartTime;

        public bool IsPlaying => isPlaying;
        public double LastScheduledDspStartTime => lastScheduledDspStartTime;

        private void Update()
        {
            if (!isPlaying)
            {
                return;
            }

            for (int i = 0; i < faders.Length; i++)
            {
                AudioSource source = faders[i] != null ? faders[i].AudioSource : null;
                if (source == null || source.clip == null)
                {
                    continue;
                }

                if (AudioSettings.dspTime > lastScheduledDspStartTime + 0.2d &&
                    !source.isPlaying)
                {
                    StopAll();
                    return;
                }
            }
        }

        public void Configure(AudioStemFader[] configuredFaders)
        {
            faders = configuredFaders ?? new AudioStemFader[0];
            ConfigureSources();
        }

        public void PlayAll()
        {
            if (isPlaying)
            {
                return;
            }

            ConfigureSources();
            lastScheduledDspStartTime = AudioSettings.dspTime + scheduleLeadSeconds;
            for (int i = 0; i < faders.Length; i++)
            {
                AudioStemFader fader = faders[i];
                AudioSource source = fader != null ? fader.AudioSource : null;
                if (source == null || source.clip == null)
                {
                    continue;
                }

                source.Stop();
                source.timeSamples = 0;
                source.volume = fader.NormalizedValue;
                source.PlayScheduled(lastScheduledDspStartTime);
            }

            isPlaying = true;
        }

        public void StopAll()
        {
            for (int i = 0; i < faders.Length; i++)
            {
                AudioSource source = faders[i] != null ? faders[i].AudioSource : null;
                if (source != null)
                {
                    source.Stop();
                }
            }

            isPlaying = false;
        }

        private void ConfigureSources()
        {
            for (int i = 0; i < faders.Length; i++)
            {
                AudioSource source = faders[i] != null ? faders[i].AudioSource : null;
                if (source == null)
                {
                    continue;
                }

                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.loop = false;
            }
        }
    }
}
