using System;
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
