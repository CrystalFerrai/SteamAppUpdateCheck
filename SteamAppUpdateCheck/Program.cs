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

namespace SteamAppUpdateCheck
{
	internal class Program
	{
		private static int Main(string[] args)
		{
			Logger logger = new ConsoleLogger();

			if (args.Length == 0)
			{
				Options.PrintUsage(logger, LogLevel.Information);
				return OnExit(0, logger);
			}

			Options? options;
			if (!Options.TryParseCommandLine(args, logger, out options))
			{
				Options.PrintUsage(logger, LogLevel.Information);
				return OnExit(1, logger);
			}

			UpdateCheckResult result = UpdateChecker.CheckForUpdate(options, logger);
			if (result.CheckSucceeded)
			{
				return OnExit(result.IsUpdateAvailable ? 3 : 2, logger);
			}

			return OnExit(1, logger);
		}

		private static int OnExit(int code, Logger logger)
		{
			logger.Log(LogLevel.Debug, $"Returning {code}");
			if (System.Diagnostics.Debugger.IsAttached)
			{
				Console.Out.WriteLine("Press a key to exit");
				Console.ReadKey();
			}
			return code;
		}
	}
}
