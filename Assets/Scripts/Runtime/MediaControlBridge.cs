using GLTFast.Schema;
using UnityEngine;

namespace rt.xr.unity
{
    /// <summary>
    /// Connects ACTION_MEDIA (ISO/IEC 23090-14:2024, Table 40) to the media players of the scene viewer.
    /// </summary>
    public static class MediaControlBridge
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            GLTFast.MpegMediaControl.Handler = Apply;
        }

        static void Apply(int mediaIndex, MediaControl control)
        {
            var viewer = Object.FindObjectOfType<SceneViewer>();
            var players = viewer != null ? viewer.MediaPlayers : null;
            if (players == null || mediaIndex < 0 || mediaIndex >= players.Count || players[mediaIndex] == null)
            {
                Debug.LogWarning("ACTION_MEDIA: no media player for media index " + mediaIndex);
                return;
            }
            var mp = players[mediaIndex];
            switch (control)
            {
                case MediaControl.MEDIA_PLAY:
                    mp.ResumePlayback();
                    mp.Play();
                    break;
                case MediaControl.MEDIA_PAUSE:
                    mp.PausePlayback();
                    break;
                case MediaControl.MEDIA_RESUME:
                    mp.ResumePlayback();
                    break;
                case MediaControl.MEDIA_STOP:
                    mp.ResumePlayback();
                    mp.Stop();
                    break;
            }
        }
    }
}
