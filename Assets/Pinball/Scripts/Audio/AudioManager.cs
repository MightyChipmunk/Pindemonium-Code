using GameCore.Audio;
using UnityEngine;

namespace Pinball.Audio
{
    /// <summary>
    /// 게임 효과음 키(EAudioClipType)를 GameCore.SfxPlayer 클립 재생으로 매핑하는 파사드.
    /// </summary>
    public static class AudioManager
    {
        public const int INVALID_LOOP_ID = SfxPlayer.INVALID_LOOP_ID;

        const float DEFAULT_PITCH = 1f;

        public static void Play(AudioStorage.EAudioClipType clipType)
        {
            Play(clipType, DEFAULT_PITCH);
        }

        public static void Play(AudioStorage.EAudioClipType clipType, float pitch)
        {
            SfxPlayer.Play(AudioStorage.GetClip(clipType), AudioStorage.GetVolume(clipType), pitch, AudioStorage.GetOverlapPolicy(clipType));
        }

        public static void PlayScaled(AudioStorage.EAudioClipType clipType, float volumeRatio)
        {
            float volume = AudioStorage.GetVolume(clipType) * Mathf.Clamp01(volumeRatio);
            SfxPlayer.Play(AudioStorage.GetClip(clipType), volume, DEFAULT_PITCH, AudioStorage.GetOverlapPolicy(clipType));
        }

        public static void PlaySequenced(AudioStorage.EAudioClipType clipType, float pitch)
        {
            SfxPlayer.PlaySequenced(AudioStorage.GetClip(clipType), AudioStorage.GetVolume(clipType), pitch, AudioStorage.GetOverlapPolicy(clipType));
        }

        public static void Play(AudioClip clip, float volume, float pitch)
        {
            SfxPlayer.Play(clip, volume, pitch);
        }

        public static int PlayLoop(AudioStorage.EAudioClipType clipType)
        {
            return SfxPlayer.PlayLoop(AudioStorage.GetClip(clipType), AudioStorage.GetVolume(clipType));
        }

        public static void SetLoopVolumeRatio(int loopId, float volumeRatio)
        {
            SfxPlayer.SetLoopVolumeRatio(loopId, volumeRatio);
        }

        public static void StopLoop(int loopId)
        {
            SfxPlayer.StopLoop(loopId);
        }
    }
}
