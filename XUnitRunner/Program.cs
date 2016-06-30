using System;
using MonoDevelop.Core.Execution;

namespace XUnitRunner
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			XUnitRunner.PreloadAssemblies();
			RemoteProcessServer server = new RemoteProcessServer();
			server.Connect(args,new RemoteXUnitRunner(server));
		}
	}
}
