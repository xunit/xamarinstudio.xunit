using System;
using MonoDevelop.Core.Execution;

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

			XUnitRunner.PreloadAssemblies();
			RemoteProcessServer server = new RemoteProcessServer();
			server.Connect(args, new RemoteXUnitRunner(server));
		}
	}
}
