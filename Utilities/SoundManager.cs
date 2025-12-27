using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace GazeStream.Utilities
{
    public static class SoundManager
    {
        private static readonly List<MediaPlayer> _activePlayers = new();

        public static void PlayOneShot(Uri uri, double volume = 1.0)
        {
            var player = new MediaPlayer
            {
                Volume = volume
            };

            player.Open(uri);
            player.Play();

            _activePlayers.Add(player);

            player.MediaEnded += (_, __) => Cleanup(player);
            player.MediaFailed += (_, __) => Cleanup(player);
        }

        private static void Cleanup(MediaPlayer player)
        {
            player.Stop();
            player.Close();
            _activePlayers.Remove(player);
        }
    }
}
