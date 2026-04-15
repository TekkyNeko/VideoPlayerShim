#if AVPRO_IMPORTED
using PVR.CCK.Worlds.Components;
using RenderHeads.Media.AVProVideo;
using System;
using UnityEngine;


namespace Pokeduel.VideoPlayerShim
{
	public sealed class AVProVideoManager : MonoBehaviour
	{

		public MediaPlayer mediaPlayer;
		public ResolveToRenderTexture resolveToRenderTexture;
		public PVR_AVProVideoProvider provider;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		public static void InitOnLoad()
		{
			PVR_AVProVideoProvider[] providers = UnityEngine.Object.FindObjectsByType<PVR_AVProVideoProvider>(FindObjectsInactive.Include, FindObjectsSortMode.None);

			foreach (var provider in providers)
			{
				provider.gameObject.AddComponent<AVProVideoManager>();
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		public static void Initialize()
		{
			PVR_AVProVideoProvider.OnPlayUrl += async (instance, url) =>
			{
				YTDLManager.VideoData data = await YTDLManager.GetVideoInfo(url, (short)instance.Resolution);

				if (data == null)
				{
					instance.OnVideoError?.Invoke();
					return;
				}

				instance.videoTitle = data.title;
				instance.thumbnailUrl = data.thumbnailUrl;

				var manager = instance.GetComponent<AVProVideoManager>();

				if (manager != null)
				{
					manager.PlayVideo(data.url);
				}
			};

			PVR_AVProVideoProvider.OnPlayVideo += async (instance, videoInfo) =>
			{
				var manager = instance.GetComponent<AVProVideoManager>();

				if(manager != null)
				{
					manager.PlayVideo(videoInfo.url);
				}
			};

			PVR_AVProVideoProvider.OnGetVideoInfo += async (url, action) => 
			{
				YTDLManager.VideoData data = await YTDLManager.GetVideoInfo(url);

				if(data == null)
				{
					action?.Invoke(new());
					return;
				}

				PVR_AVProVideoProvider.VideoInfo videoInfo = new()
				{
					isValid = true,
					title = data.title,
					thumbnailUrl = data.thumbnailUrl,
					extractedUrl = data.url,
					url = url
				};

				action?.Invoke(videoInfo);
			};
		}

		void Awake()
		{
			if(Application.isPlaying)
			{
				provider = GetComponent<PVR_AVProVideoProvider>();
				mediaPlayer = gameObject.AddComponent<MediaPlayer>();

				mediaPlayer.PlatformOptionsWindows._audioMode = Windows.AudioOutput.Unity;
				mediaPlayer.PlatformOptionsWindows.videoApi = Windows.VideoApi.MediaFoundation;
				mediaPlayer.PlatformOptionsWindows.useLowLatency = false;
				mediaPlayer.PlatformOptionsWindows.useLowLiveLatency = false;
				mediaPlayer.SetMediaSource(MediaSource.Path);
				mediaPlayer.AutoOpen = false;
				
				resolveToRenderTexture = gameObject.AddComponent<ResolveToRenderTexture>();
				resolveToRenderTexture.MediaPlayer = mediaPlayer;
				resolveToRenderTexture.ExternalTexture = provider.videoTexture;
				resolveToRenderTexture.VideoResolveOptions = new()
				{
					aspectRatio = VideoResolveOptions.AspectRatio.FitInside,
					tint = Color.white,
				};

				foreach(var speaker in provider.speakers)
				{
					if (speaker.audioSource == null)
						continue;

					AudioOutput audioOutput = speaker.audioSource.gameObject.AddComponent<AudioOutput>();
					audioOutput.SetAudioSource(speaker.audioSource);
					audioOutput.ChangeMediaPlayer(mediaPlayer);

					speaker.audioSource.spatialize = true;

					audioOutput.ChannelMask = (
						(speaker.channel1 ? 1 << 0 : 0) |
						(speaker.channel2 ? 1 << 1 : 0) |
						(speaker.channel3 ? 1 << 2 : 0) |
						(speaker.channel4 ? 1 << 3 : 0) |
						(speaker.channel5 ? 1 << 4 : 0) |
						(speaker.channel6 ? 1 << 5 : 0) |
						(speaker.channel7 ? 1 << 6 : 0) |
						(speaker.channel8 ? 1 << 7 : 0)
					);
				}

				mediaPlayer.Events.AddListener(OnVideoEvent);

				provider._OnGetTimestamp += videoProvider =>
				{
					return Math.Clamp(mediaPlayer.Control.GetCurrentTime(), double.MinValue, double.MaxValue);
				};

				provider._OnGetIsPaused += videoProvider =>
				{
					return mediaPlayer.Control.IsPaused();
				};

				provider._OnGetLoop += videoProvider =>
				{
					return mediaPlayer.Loop;
				};

				provider._OnGetPlaybackRate += videoProvider =>
				{
					return mediaPlayer.PlaybackRate;
				};

				provider._OnSetPaused += paused =>
				{
					if(paused)
					{
						mediaPlayer.Pause();
					}
					else
					{
						mediaPlayer.Play();
					}
				};

				provider._OnSetLoop += loop =>
				{
					mediaPlayer.Loop = loop;
				};

				provider._OnSetTime += time =>
				{
					mediaPlayer.Control.SeekFast(time);
				};

				provider._OnSetPlaybackRate += playbackRate =>
				{
					mediaPlayer.PlaybackRate = playbackRate;

					foreach (var speaker in provider.speakers)
					{
						speaker.audioSource.pitch = playbackRate;
					}
				};

				//provider._OnSetVolume += volume =>
				//{
				//	mediaPlayer.AudioVolume = volume;
				//};

				provider._OnStop += () =>
				{
					mediaPlayer.Stop();
				};


			}
		}

		private static void OnVideoEvent(MediaPlayer player, MediaPlayerEvent.EventType eventType, ErrorCode errorCode)
		{
			player.TryGetComponent<PVR_AVProVideoProvider>(out PVR_AVProVideoProvider provider);
			if (provider == null) return;

			switch(eventType)
			{
				case MediaPlayerEvent.EventType.Started:
					provider.isLoading = false;
					provider.isPlaying = true;
					provider.OnVideoStarted?.Invoke();
					break;

				case MediaPlayerEvent.EventType.MetaDataReady:
					double duration = player.Info.GetDuration();

					provider.isLiveStream = double.IsInfinity(duration);

					if(!provider.isLiveStream)
					{
						provider.duration = Math.Clamp(duration, 0, double.MaxValue);
					}
					else
					{
						provider.duration = -1;
					}

					provider.OnVideoReady?.Invoke();
					break;
				case MediaPlayerEvent.EventType.FinishedPlaying:
					provider.isPlaying = false;
					provider.OnVideoEnd?.Invoke();
					break;
				case MediaPlayerEvent.EventType.Error:
					if (!provider.isTryingToStart)
						provider.OnVideoError?.Invoke();
					break;

			}
		}

		private async void PlayVideo(string url)
		{
			provider.isTryingToStart = true;

			try
			{
				mediaPlayer.OpenMedia(MediaPathType.AbsolutePathOrURL, url, true);
			}
			catch (Exception e)
			{
				Debug.LogError($"Failed to play Video: {e}");
			}
			finally
			{
				provider.isTryingToStart = false;
			}
		}
	}
}
#endif