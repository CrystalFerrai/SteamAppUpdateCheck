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

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SteamAppUpdateCheck
{
	internal static class UpdateChecker
	{
		public static UpdateCheckResult CheckForUpdate(Options options, Logger logger)
		{
			long localTime, remoteTime;
			string? branch;
			if (!FindLocalUpdateTime(options, logger, out localTime, out branch))
			{
				return UpdateCheckResult.Failure();
			}
			if (!FindRemoteUpdateTime(options, branch, logger, out remoteTime))
			{
				return UpdateCheckResult.Failure();
			}

			logger.Log(LogLevel.Debug, $"branch={branch}");
			logger.Log(LogLevel.Debug, $"local={localTime}");
			logger.Log(LogLevel.Debug, $"remote={remoteTime}");

			return UpdateCheckResult.Success(remoteTime > localTime);
		}

		private static bool FindLocalUpdateTime(Options options, Logger logger, out long result, [NotNullWhen(true)] out string? branch)
		{
			result = 0;
			branch = null;

			SteamMetaFile? manifest;
			if (options.AppsDir == null)
			{
				if (!AppLocator.TryLocateAppManifest(options.AppId, logger, out manifest))
				{
					return false;
				}
			}
			else
			{
				string manifestPath = Path.Combine(options.AppsDir, $"appmanifest_{options.AppId}.acf");
				try
				{
					manifest = SteamMetaFile.Load(manifestPath);
				}
				catch
				{
					logger.LogError($"appmanifest_{options.AppId}.acf not found at {options.AppsDir}");
					return false;
				}
			}

			if (manifest.RootObject is null)
			{
				logger.LogError($"appmanifest_{options.AppId}.acf: manifest format is not recognized.");
				return false;
			}

			SteamMetaValue? lastUpdateToken = manifest.RootObject["LastUpdated"] as SteamMetaValue;
			if (lastUpdateToken is null)
			{
				logger.LogError($"appmanifest_{options.AppId}.acf: manifest is missing LastUpdated property.");
				return false;
			}

			if (!long.TryParse(lastUpdateToken.Value, out long lastUpdateTime))
			{
				logger.LogError($"appmanifest_{options.AppId}.acf: LastUpdated property value could not be parsed.");
				return false;
			}

			string betaKey = "public";
			if (options.Branch is null)
			{
				SteamMetaObject? userConfigObject = manifest.RootObject["UserConfig"] as SteamMetaObject;
				if (userConfigObject is not null)
				{
					if (userConfigObject["BetaKey"] is SteamMetaValue userConfigToken)
					{
						if (!string.IsNullOrEmpty(userConfigToken.Value))
						{
							betaKey = userConfigToken.Value;
						}
					}
				}
			}

			result = lastUpdateTime;
			branch = betaKey;
			return true;
		}

		private static bool FindRemoteUpdateTime(Options options, string branch, Logger logger, out long result)
		{
			result = 0;

			string json;
			using (HttpClient client = new())
			{
				string url = $"https://api.steamcmd.net/v1/info/{options.AppId}";

				HttpResponseMessage response = client.GetAsync(url).Result;
				if (!response.IsSuccessStatusCode)
				{
					logger.LogError($"Steam API request returned error code {response.StatusCode}");
					return false;
				}
				json = response.Content.ReadAsStringAsync().Result;
			}

			JObject root = JObject.Parse(json);

			string timePath = $"data.{options.AppId}.depots.branches.{branch}.timeupdated";
			JToken? timeToken = root.SelectToken(timePath);
			if (timeToken is null)
			{
				logger.LogError($"Failed to find token \"data.{options.AppId}.depots.branches.public.timeupdated\" in response from Steam API request.");
				return false;
			}

			string? timeStr = timeToken.Value<string>();
			if (timeStr is null)
			{
				logger.LogError($"Failed to read token \"data.{options.AppId}.depots.branches.public.timeupdated\" in response from Steam API request.");
				return false;
			}

			if (!long.TryParse(timeStr, out var time))
			{
				logger.LogError($"Failed to parse token \"data.{options.AppId}.depots.branches.public.timeupdated\" in response from Steam API request.");
				return false;
			}

			result = time;
			return true;
		}
	}

	internal class UpdateCheckResult
	{
		public bool CheckSucceeded { get; }

		public bool IsUpdateAvailable { get; }

		private UpdateCheckResult(bool checkSucceeded, bool isUpdateAvailable)
		{
			CheckSucceeded = checkSucceeded;
			IsUpdateAvailable = isUpdateAvailable;
		}

		public static UpdateCheckResult Success(bool isUpdateAvailable)
		{
			return new UpdateCheckResult(true, isUpdateAvailable);
		}

		public static UpdateCheckResult Failure()
		{
			return new UpdateCheckResult(false, false);
		}
	}
}
