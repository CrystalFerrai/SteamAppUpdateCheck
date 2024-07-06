// Copyright 2024 Crystal Ferrai
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Win32;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;

namespace SteamAppUpdateCheck
{
	/// <summary>
	/// Utility for locating the install directory of a Steam app
	/// </summary>
	internal static class AppLocator
	{
		/// <summary>
		/// Attempts to locate an app's installed manifest using Steam installer data
		/// </summary>
		/// <param name="appId">The Steam app id of the app to look for</param>
		/// <param name="logger">Will be used to log error messages</param>
		/// <param name="manifest">If successful, outputs the app manifest</param>
		/// <returns>Whether the app installation could be located</returns>
		public static bool TryLocateAppManifest(string appId, Logger logger, [NotNullWhen(true)] out SteamMetaFile? manifest)
		{
			manifest = null;
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				logger.LogError("Automatic manifest location detection is only available in Windows.");
				return false;
			}

			RegistryKey? baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
			if (baseKey == null)
			{
				logger.LogError("Automatic manifest location detection failed to access the system registry.");
				return false;
			}

			RegistryKey? steamKey = baseKey.OpenSubKey("SOFTWARE\\Valve\\Steam", RegistryKeyPermissionCheck.ReadSubTree);
			if (steamKey == null)
			{
				logger.LogError("Automatic manifest location detection failed to locate Steam in the system registry.");
				return false;
			}

			string? steamPath = steamKey.GetValue("InstallPath") as string;
			if (steamPath == null)
			{
				logger.LogError("Automatic manifest location detection failed to locate Steam install location in the system registry.");
				return false;
			}

			string libraryPath = Path.Combine(steamPath, "steamapps\\libraryfolders.vdf");
			if (!File.Exists(libraryPath))
			{
				logger.LogError("Automatic manifest location detection failed to locate Steam library directory information file (libraryfolders.vdf).");
				return false;
			}

			string? appsPath = null;
			try
			{
				SteamMetaFile library = SteamMetaFile.Load(libraryPath);

				foreach (SteamMetaObject obj in library.RootObject!)
				{
					SteamMetaObject apps = (SteamMetaObject)obj["apps"];

					foreach (SteamMetaValue app in apps)
					{
						if (app.Name == appId)
						{
							appsPath = ((SteamMetaValue)obj["path"]).Value;
							break;
						}
					}
					if (appsPath != null) break;
				}
			}
			catch
			{
				logger.LogError("Automatic manifest location detection failed to read Steam library directory information file (libraryfolders.vdf).");
				return false;
			}
			if (appsPath == null)
			{
				logger.LogError($"Automatic manifest location detection failed to locate app {appId} in any Steam library.");
				return false;
			}
			appsPath = appsPath.Replace("\\\\", "\\");

			string appManifestPath = Path.Combine(appsPath, $"steamapps\\appmanifest_{appId}.acf");
			if (!File.Exists(appManifestPath))
			{
				logger.LogError($"Automatic manifest location detection failed to locate the manifest for app {appId} in the detected location.");
				return false;
			}

			manifest = SteamMetaFile.Load(appManifestPath);
			return true;
		}
	}
}
