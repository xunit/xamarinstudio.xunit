﻿using System;
using System.Reflection;
using System.Threading.Tasks;
using MonoDevelop.Core.Execution;
using RollbarDotNet;

namespace XUnitRunner
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			if (args.Length == 1)
			{
				XUnitRunner.PreloadAssemblies();
				var runner = new XUnitRunner();
				var info = runner.GetTestInfo(args[0], null);
				return;
			}

			RegisterRollbar();

			XUnitRunner.PreloadAssemblies();
			RemoteProcessServer server = new RemoteProcessServer();
			server.Connect(args, new RemoteXUnitRunner(server));
		}

		private static void RegisterRollbar()
		{
			// IMPORTANT: if no .runsettings file is found, use the extension setting from environment variable.
			var enabledString = Environment.GetEnvironmentVariable("MONODEVELOP_XUNIT_ROLLBAR_ENABLED")?.ToUpperInvariant();
			if (enabledString == null) {
				// IMPORTANT: by default enable rollbar.
				enabledString = "TRUE";
			}

			bool enabled;
			if (!bool.TryParse(enabledString, out enabled)) {
				enabled = true;
			}

			if (!enabled) {
				return;
			}

			Console.WriteLine($"This extension uses Rollbar to log information. To disable logging, set environment variable MONODEVELOP_XUNIT_ROLLBAR_ENABLED to FALSE");

			Rollbar.Init(new RollbarConfig {
				AccessToken = "ed0dab184230478c97c81d0a6b77ce67",
				Environment = "production"
			});
			var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
			var userName = $"{version}";
			Rollbar.PersonData(() => new Person(version) {
				UserName = userName
			});

			AppDomain.CurrentDomain.UnhandledException += (sender, args) => {
				Rollbar.Report(args.ExceptionObject as System.Exception);
			};

			TaskScheduler.UnobservedTaskException += (sender, args) => {
				Rollbar.Report(args.Exception);
			};
		}
	}
}
