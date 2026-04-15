using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.Networking;

namespace Pokeduel.VideoPlayerShim
{
	[InitializeOnLoad]
	public static class AVProDownloader
	{
		private const string AVProPackageURL = "https://github.com/RenderHeads/UnityPlugin-AVProVideo/releases/download/3.3.6/UnityPlugin-AVProVideo-v3.3.6-Trial.unitypackage";
		private const string AVProCheckGUID = "79e446998599e1647804321292c80f42";

		private static bool AvProImported
		{
			get
			{
				if (File.Exists("Assets/AVProVideo/Runtime/Scripts/Internal/Helper.cs"))
				{
					AddScriptDefineVariable();
					return true;
				}

				if (File.Exists(AssetDatabase.GUIDToAssetPath(AVProCheckGUID)))
				{
					AddScriptDefineVariable();
					return true;
				}
				RemoveScriptDefineVariable();
				return false;
			}
		}

		static AVProDownloader()
		{

			bool prompted = SessionState.GetBool("AVProDownloader.userWasPrompted", false);
			if (!prompted && !AvProImported)
			{
				if (EditorUtility.DisplayDialog("AVPro is not imported.", "Would you like to install the trial version of AVPro?", "Yes", "No"))
				{
					ImportAVPro();
				}

				SessionState.SetBool("AVProDownloader.userWasPrompted", true);
			}
		}

		[MenuItem("Pokeduel/Video Player Shim/Import AVPro")]
		private static void ImportAVPro()
		{
			if (AvProImported) return;
			Debug.Log("[AVProDownloader] Downloading AVPro...");
			var tempFile = $"{Application.temporaryCachePath}/UnityPlugin-AVProVideo-v3.3.6-Trial.unitypackage";

			UnityWebRequest avproRequest = new UnityWebRequest(AVProPackageURL);
			avproRequest.downloadHandler = new DownloadHandlerFile(tempFile);

			var res = avproRequest.SendWebRequest();
			res.completed += data =>
			{
				if (!File.Exists(tempFile))
				{
					Debug.LogError("[AVProDownloader] Error: Failed to download AVPro Trial");
					return;
				}

				Debug.Log("[AVProDownloader] AVPro Trial downloaded, importing package...");
				AssetDatabase.ImportPackage(tempFile, false);
				AssetDatabase.Refresh();
			};
		}

		private static void AddScriptDefineVariable()
		{

			var buildTarget = EditorUserBuildSettings.selectedBuildTargetGroup;
			var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTarget);

			if (!defines.Contains("AVPRO_IMPORTED"))
			{
				defines += (defines.Length > 0 ? ";" : "") + "AVPRO_IMPORTED";
				PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTarget, defines);
			}
		}

		private static void RemoveScriptDefineVariable()
		{
			var buildTarget = EditorUserBuildSettings.selectedBuildTargetGroup;
			var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTarget);

			var list = new System.Collections.Generic.List<string>(defines.Split(';'));
			if (list.Remove("AVPRO_IMPORTED"))
			{
				PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTarget, string.Join(";", list));
			}
		}
	}
}