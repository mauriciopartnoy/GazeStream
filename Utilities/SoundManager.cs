using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Media;
using System.Windows.Resources;
using System.Diagnostics;

namespace GazeStream.Utilities
{
    public static class SoundManager
    {
        private static readonly List<MediaPlayer> _activePlayers = new();

        public static void PlayOneShot(Uri uri, double volume = 1.0)
        {
            Debug.WriteLine("Trying to play one shot!!!!!!!!!!!");
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

        public static void PlayUISound(Uri packUri)
        {
            StreamResourceInfo sri =
                System.Windows.Application.GetResourceStream(packUri);

            if (sri == null)
                return;

            using var player = new SoundPlayer(sri.Stream);
            player.Play();
        }
        private static void Cleanup(MediaPlayer player)
        {
            player.Stop();
            player.Close();
            _activePlayers.Remove(player);
        }

        public static void SetSystemVolume()
        { 
            //TODO: Implementar nativo o usando NAudio
        }

    }
}
