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

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace SteamAppUpdateCheck
{
	/// <summary>
	/// Program options
	/// </summary>
	internal class Options
	{
		/// <summary>
		/// The app id to check
		/// </summary>
		public string AppId { get; set; }

		/// <summary>
		/// The location of the steamapps directory associated with the installed app
		/// </summary>
		public string? AppsDir { get; set; }

		/// <summary>
		/// The app branch to check for updates
		/// </summary>
		public string? Branch { get; set; }

		private Options()
		{
			AppId = null!;
			AppsDir = null;
			Branch = null;
		}

		/// <summary>
		/// Create an Options instance from command line arguments
		/// </summary>
		/// <param name="args">The command line arguments to parse</param>
		/// <param name="logger">For logging parse errors</param>
		/// <param name="options">Outputs the options if parsing is successful</param>
		/// <returns>Whether parsing was successful</returns>
		public static bool TryParseCommandLine(string[] args, Logger logger, [NotNullWhen(true)] out Options? result)
		{
			if (args.Length == 0)
			{
				result = null;
				return false;
			}

			Options instance = new();

			int positionalArgIndex = 0;

			for (int i = 0; i < args.Length; ++i)
			{
				if (args[i].StartsWith("--"))
				{
					// Explicit arg
					string argValue = args[i][2..];
					switch (argValue)
					{
						case "appsdir":
							if (i < args.Length - 1 && !args[i + 1].StartsWith("--"))
							{
								instance.AppsDir = Path.GetFullPath(args[i + 1]);
								if (instance.AppsDir is null || !Directory.Exists(instance.AppsDir))
								{
									logger.LogError($"Specified AppsDir does not exist: {args[i + 1]}");
									result = null;
									return false;
								}
								else
								{
									string dirName = Path.GetFileName(instance.AppsDir);
									if (!dirName.Equals("steamapps", StringComparison.OrdinalIgnoreCase))
									{
										instance.AppsDir = Path.Combine(instance.AppsDir, "steamapps");
										if (!Directory.Exists(instance.AppsDir))
										{
											logger.LogError($"Could not locate \"steamapps\" folder in AppsDir: {args[i + 1]}");
											result = null;
											return false;
										}
									}
								}
								++i;
							}
							else
							{
								logger.LogError("Missing parameter for --key argument");
								result = null;
								return false;
							}
							break;
						case "branch":
							if (i < args.Length - 1 && !args[i + 1].StartsWith("--"))
							{
								instance.Branch = args[i + 1];
								++i;
							}
							else
							{
								logger.LogError("Missing parameter for --key argument");
								result = null;
								return false;
							}
							break;
						default:
							logger.LogError($"Unrecognized argument '{args[i]}'");
							result = null;
							return false;
					}
				}
				else
				{
					// Positional arg
					switch (positionalArgIndex)
					{
						case 0:
							instance.AppId = args[i];
							break;
						case 1:
						default:
							logger.LogError("Too many positional arguments.");
							result = null;
							return false;
					}
					++positionalArgIndex;
				}
			}

			if (positionalArgIndex < 1)
			{
				logger.LogError($"Not enough positional arguments");
				result = null;
				return false;
			}

			result = instance;
			return true;
		}

		/// <summary>
		/// Prints how to use the program, including all possible command line arguments
		/// </summary>
		/// <param name="logger">Where the message will be printed</param>
		/// <param name="logLevel">The log level for the message</param>
		/// <param name="indent">Every line of the output will be prefixed with this</param>
		public static void PrintUsage(Logger logger, LogLevel logLevel, string indent = "")
		{
			string programName = Assembly.GetExecutingAssembly().GetName().Name ?? "SteamAppUpdateCheck";
			logger.Log(logLevel, $"{indent}Checks if there is an update available for an installed Steam app.");
			logger.Log(logLevel, $"{indent}Returns 2 if no update is available or 3 if an update is available.");
			logger.LogEmptyLine(logLevel);
			logger.Log(logLevel, $"{indent}Usage: {programName} [[options]] [app id]");
			logger.LogEmptyLine(logLevel);
			logger.Log(logLevel, $"{indent}  [app id]  The Steam App ID of the app to check for an update");
			logger.LogEmptyLine(logLevel);
			logger.Log(logLevel, $"{indent}Options");
			logger.LogEmptyLine(logLevel);
			logger.Log(logLevel, $"{indent}  --appsdir  Path to a directory containing the steamapps directory with information");
			logger.Log(logLevel, $"{indent}             about the app. If not specified, will check Steam library directories.");
			logger.LogEmptyLine(logLevel);
			logger.Log(logLevel, $"{indent}  --branch   The branch of the app to check for an update. If not specified, will");
			logger.Log(logLevel, $"{indent}             check the branch the installed app is currently using.");
		}
	}
}
