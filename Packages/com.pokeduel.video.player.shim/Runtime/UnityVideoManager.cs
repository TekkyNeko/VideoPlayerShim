using PVR.CCK.Worlds.Components;
using UnityEngine;

namespace Pokeduel.VideoPlayerShim
{
	public class UnityVideoManager : MonoBehaviour
	{

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		public static void InitOnLoad()
		{
			PVR_VideoProvider[] providers = UnityEngine.Object.FindObjectsByType<PVR_VideoProvider>(FindObjectsInactive.Include, FindObjectsSortMode.None);

			foreach (var provider in providers)
			{
				provider.gameObject.AddComponent<UnityVideoManager>();
			}

			PVR_VideoProvider.OnPlay += (provider, url, autoStart) =>
			{
				provider.isLoadingVideo = true;
				provider.OnVideoLoading?.Invoke();
				PlayVideo(provider, url, autoStart);
			};
		}

		public static async void PlayVideo(PVR_VideoProvider provider, string url, bool autoStart)
		{
			var data = await YTDLManager.GetVideoInfo(url, 360); // Limit to 360p due to Unity Video not supporting HLS manifests

			provider.isLoadingVideo = false;
			if(data == null)
			{
				Debug.LogError("[UnityVideoManager] Video Failed to load: " + url);
				provider.OnVideoError?.Invoke();
				return;
			}

			provider.isStream = false;

			provider.thumbnailUrl = data.thumbnailUrl;
			provider.videoTitle = data.title;

			// Video Player Stuff

			provider.videoPlayer.source = UnityEngine.Video.VideoSource.Url;
			provider.videoPlayer.url = data.url;
			provider.videoPlayer.aspectRatio = UnityEngine.Video.VideoAspectRatio.FitInside;


			provider.videoPlayer.Prepare();

			provider.OnVideoReady?.Invoke();

			if (autoStart)
				provider.videoPlayer.Play();
		}
	}
}