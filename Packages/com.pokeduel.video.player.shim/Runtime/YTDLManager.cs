using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace Pokeduel.VideoPlayerShim
{
	public static class YTDLManager
	{
		private static string ytdlPath = "/AppData/LocalLow/PoligonTechnologies/PoligonVR/yt-dlp.exe";

		public static async Task<VideoData> GetVideoInfo(string url, short resolution = 1080)
		{
			string userProfilePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);

			if (!File.Exists(userProfilePath + ytdlPath))
			{
				UnityEngine.Debug.LogError("[YTDLManager] yt-dlp was not found, launch PoligonVR to download it!");
				return null;
			}

			string ytdlpArgs = $"--no-check-certificate --no-cache-dir --rm-cache-dir -f \"(mp4,webm)[height<=?{resolution}]/best[height<=?{resolution}]\" --get-thumbnail --print %(title)s -g \"{url}\"";

			Process ytdlProcess = new Process();
			
			ProcessStartInfo startInfo = new ProcessStartInfo();
			startInfo.WindowStyle = ProcessWindowStyle.Hidden;
			startInfo.CreateNoWindow = true;
			startInfo.UseShellExecute = false;
			startInfo.RedirectStandardError = true;
			startInfo.RedirectStandardOutput = true;
			startInfo.FileName = userProfilePath + ytdlPath;
			startInfo.Arguments = ytdlpArgs;

			ytdlProcess.StartInfo = startInfo;

			int i = 0;
			bool finished = false;

			VideoData videoData = new VideoData();

			ytdlProcess.OutputDataReceived += (obj, data) =>
			{
				switch(i)
				{
					case 0: videoData.title = data.Data; break;
					case 1: videoData.url = data.Data;  break;
					case 2:
						videoData.thumbnailUrl = data.Data;
						finished = true;
						break;
					default: finished = true; break;
				}
				i++;
			};

			ytdlProcess.ErrorDataReceived += (obj, data) =>
			{
			
			};

			ytdlProcess.Start();
			ytdlProcess.BeginErrorReadLine();
			ytdlProcess.BeginOutputReadLine();

			while (!finished) await Task.Delay(10);

			if(videoData.url == null)
			{
				UnityEngine.Debug.LogError($"[YTDLManager] Error: Failed to download {url}");
				return null;
			}
			else
			{
				return videoData;
			}
		}

		public sealed class VideoData
		{
			public string title;
			public string thumbnailUrl;
			public string url;
		}
	}
}