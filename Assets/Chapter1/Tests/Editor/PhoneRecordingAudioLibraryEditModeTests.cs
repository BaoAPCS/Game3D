using NUnit.Framework;
using UnityEngine;

namespace DormitoryMystery.Chapter1.Tests
{
    public sealed class PhoneRecordingAudioLibraryEditModeTests
    {
        [Test]
        public void DefaultLibraryAndCatalogResolveMixedRecordingAndEveryStem()
        {
            PhoneRecordingAudioLibrary library = PhoneRecordingAudioLibrary.LoadDefault();

            Assert.NotNull(library, "Default phone recording library is missing from Resources.");
            AssertResolved(library, LanAudioRecordingCatalog.MixedRecordingId);

            foreach (LanAudioStemId stem in LanAudioRecordingCatalog.StemOrder)
            {
                AssertResolved(library, LanAudioRecordingCatalog.GetOutputRecordingId(stem));
            }
        }

        private static void AssertResolved(
            PhoneRecordingAudioLibrary library,
            string recordingId)
        {
            AudioClip libraryClip = library.ResolveClip(recordingId);
            AudioClip catalogClip = LanAudioRecordingCatalog.ResolveClip(
                recordingId,
                null,
                null);

            Assert.NotNull(libraryClip, recordingId + " is not assigned in the library.");
            Assert.AreSame(
                libraryClip,
                catalogClip,
                recordingId + " did not use the scene-independent fallback.");
        }
    }
}
