using System;
using System.Collections.Generic;
using System.IO;
using MonoDevelop.Core.Execution;
using MonoDevelop.UnitTesting.XUnit;
using MonoDevelop.UnitTesting.XUnit.External;
using Xunit.Abstractions;

namespace XUnitRunner
{
	public class RemoteXUnitRunner: MessageListener
	{
		RemoteProcessServer server;
		public RemoteXUnitRunner(RemoteProcessServer server)
		{
			this.server = server;
		}

		[MessageHandler]
		public GetTestInfoResponse GetTestInfo(GetTestInfoRequest req)
		{
			var runner = new XUnitRunner();

			return new GetTestInfoResponse() { Result = runner.GetTestInfo(req.Path,null) };
		}

		[MessageHandler]
		public RunResponse Run(RunRequest r)
		{
			var res = Run(r.NameFilter, r.Path, r.SuiteName, r.SupportAssemblies, r.TestRunnerType, r.TestRunnerAssembly, r.CrashLogFile);
			return new RunResponse() { Result = res };
		}

		public RemoteTestResult Run(string[] nameFilter, string path, string suiteName, string[] supportAssemblies, string testRunnerType, string testRunnerAssembly, string crashLogFile)
		{
			var listener = new EventListenerWrapper(server);
			var runner = new XUnitRunner();
			runner.Execute(path, nameFilter, listener);
			return new RemoteTestResult();
		}
	}

    class EventListenerWrapper : IRemoteEventListener
	{
		RemoteProcessServer server;
		public EventListenerWrapper (RemoteProcessServer server)
		{
			this.server = server;
		}

		public void OnTestCaseFinished(string id)
		{
			server.SendMessage(new TestFinishedMessage {
				TestCaseId=id
			});
		}

		public void OnTestCaseStarting(string id)
		{
			server.SendMessage(new TestStartedMessage {
				TestCaseId = id
			});
		}

		public void OnTestFailed(string id, decimal executionTime, string output, string[] exceptionTypes, string[] messages, string[] stackTraces)
		{
			server.SendMessage(new TestFailedMessage {
				TestCaseId=id,
				ExecutionTime = TimeSpan.FromMilliseconds(1000*(double)executionTime),
				Output = output,
				ExceptionTypes=exceptionTypes,
				Messages=messages,
				StackTraces=stackTraces
			});
		}

		public void OnTestPassed(string id, decimal executionTime, string output)
		{
			server.SendMessage(new TestPassedMessage {
				TestCaseId=id,
				ExecutionTime=TimeSpan.FromMilliseconds(1000 * (double)executionTime),
				Output=output
			});
		}

		public void OnTestSkipped(string id, string reason)
		{
			server.SendMessage(new TestSkippedMessage {
				TestCaseId =id,
				Reason=reason
			});
		}
	}
}

