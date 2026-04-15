#if AVPRO_IMPORTED
using RenderHeads.Media.AVProVideo;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Pokeduel.VideoPlayerShim
{
    /// <summary>
    /// The sole purpose of this script is to keep the video player rendering in scene mode, otherwise its not happy
    /// </summary>

	[InitializeOnLoad]
	public static class AVProHelper
    {
        static AVProHelper()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode || !EditorApplication.isPlaying)
                return;

            bool gameView = false;

			foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
			{
				if (window.GetType().Name == "GameView" && window.hasFocus)
				{
					gameView = true;
					break;
				}
			}

            if(!gameView)
            {
                foreach (var player in Object.FindObjectsByType<MediaPlayer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    player.Player?.Render();
                }

                SceneView.RepaintAll();
            }
		}
    }
}
#endif