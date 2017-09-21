//
// ExternalTestRunner.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.IO;
using System.Collections.Generic;

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using System.Threading.Tasks;
using MonoDevelop.XUnit;

namespace MonoDevelop.UnitTesting.XUnit.External
{
	class ExternalTestRunner : IDisposable
	{
		RemoteProcessConnection connection;
		IRemoteEventListener listener;

		public Task Connect(XUnitVersion version, IExecutionHandler executionHandler = null, OperationConsole console = null)
		{
			var bitness = Environment.GetEnvironmentVariable("MONODEVELOP_XUNIT_RUNNER_BITNESS")?.ToUpperInvariant();
			if (string.IsNullOrEmpty(bitness))
			{
				bitness = "X64";
			}

			var executableName = bitness == "X64" ? "XUnitRunner.exe" : "XUnitRunner.x86.exe";
			var exePath = Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location), version.ToString(), executableName);
			connection = new RemoteProcessConnection(exePath, executionHandler, console, Runtime.MainSynchronizationContext);
			connection.AddListener(this); // execution handler is where the debugger kicks in.
			return connection.Connect();
		}

		public async Task<UnitTestResult> Run(IRemoteEventListener listener, string[] nameFilter, string path, string suiteName, List<string> supportAssemblies, string testRunnerType, string testRunnerAssembly, string crashLogFile)
		{
			this.listener = listener;

			var msg = new RunRequest {
				NameFilter = nameFilter,
				Path = path,
				SuiteName = suiteName,
				SupportAssemblies = supportAssemblies.ToArray(),
				TestRunnerType = testRunnerType,
				TestRunnerAssembly = testRunnerAssembly,
				CrashLogFile = crashLogFile
			};

			var r = (await connection.SendMessage(msg)).Result;

			await connection.ProcessPendingMessages();

			return ToUnitTestResult(r);
		}

		UnitTestResult ToUnitTestResult(RemoteTestResult r)
		{
			if (r == null)
				return null;

			return new UnitTestResult {
				TestDate = r.TestDate,
				Status = (ResultStatus)(int)r.Status,
				Passed = r.Passed,
				Errors = r.Errors,
				Failures = r.Failures,
				Inconclusive = r.Inconclusive,
				NotRunnable = r.NotRunnable,
				Skipped = r.Skipped,
				Ignored = r.Ignored,
				Time = r.Time,
				Message = r.Message,
				StackTrace = r.StackTrace,
				ConsoleOutput = r.ConsoleOutput,
				ConsoleError = r.ConsoleError
			};
		}

		public async Task<XUnitTestInfo> GetTestInfo(string path, List<string> supportAssemblies)
		{
			var msg = new GetTestInfoRequest {
				Path = path,
				SupportAssemblies = supportAssemblies.ToArray()
			};
			try {
				return (await connection.SendMessage(msg)).Result;
			} catch (Exception ex) {
				LoggingService.LogError(ex.ToString ());
				return new XUnitTestInfo();
			}
		}

		[MessageHandler]
		public void OnTestStarted(TestStartedMessage msg)
		{
			listener.OnTestCaseStarting(msg.TestCaseId);
		}

		[MessageHandler]
		public void OnTestFinished(TestFinishedMessage msg)
		{
			listener.OnTestCaseFinished(msg.TestCaseId);
		}

		[MessageHandler]
		public void OnTestPassed(TestPassedMessage msg)
		{
			listener.OnTestPassed(msg.TestCaseId, (decimal)(msg.ExecutionTime.TotalMilliseconds/1000.0), msg.Output);
		}

		[MessageHandler]
		public void OnTestFailed(TestFailedMessage msg)
		{
			listener.OnTestFailed(msg.TestCaseId, (decimal)(msg.ExecutionTime.TotalMilliseconds / 1000.0), msg.Output, msg.ExceptionTypes, msg.Messages, msg.StackTraces);
		}

		[MessageHandler]
		public void OnTestSkipped(TestSkippedMessage msg)
		{
			listener.OnTestSkipped(msg.TestCaseId, msg.Reason);
		}

        public void Dispose ()
        {
            connection.Dispose ();
        }
    }

    class LocalTestMonitor : MarshalByRefObject, IRemoteEventListener
    {
        TestContext context;
        UnitTest rootTest;
        public bool Canceled;

        public LocalTestMonitor (TestContext context, UnitTest rootTest, string rootFullName, bool singleTestRun)
        {
            this.rootTest = rootTest;
            this.context = context;
        }

		XUnitTestCase GetLocalTest (string sname)
        {
            if (sname == null)
                return null;
            UnitTest tt = FindTest (rootTest, sname);
            return tt as XUnitTestCase;
        }

        UnitTest FindTest (UnitTest t, string testPath)
        {
            var group = t as UnitTestGroup;
            if (group == null)
                return null;
            return SearchRecursive (group, testPath);
        }

        UnitTest SearchRecursive (UnitTestGroup group, string testPath)
        {
            UnitTest result;
            foreach (var t in group.Tests) {
                if (t.TestId == testPath)
                    return t;
                var childGroup = t as UnitTestGroup;
                if (childGroup != null) {
                    result = SearchRecursive (childGroup, testPath);
                    if (result != null)
                        return result;
                }
            }
            return null;
        }

		public void OnTestCaseStarting(string id)
		{
			if (Canceled)
				return;
			
			var t = GetLocalTest(id);
			if (t == null)
				return;
			t.OnStarting(context, id);
		}

		public void OnTestCaseFinished(string id)
		{
			if (Canceled)
				return;
			var t = GetLocalTest(id);
			if (t == null)
				return;
			t.OnFinished(context, id);
		}

		public void OnTestFailed(string id, decimal executionTime, string output, string[] exceptionTypes, string[] messages, string[] stackTraces)
		{
			var t = GetLocalTest(id);
			if (t == null)
				return;
			t.OnFailed(context, id,executionTime,output,exceptionTypes,messages,stackTraces);
		}

		public void OnTestPassed(string id, decimal executionTime, string output)
		{
			var t = GetLocalTest(id);
			if (t == null)
				return;
			t.OnPassed(context,id, executionTime, output);
		}

		public void OnTestSkipped(string id, string reason)
		{
			var t = GetLocalTest(id);
			if (t == null)
				return;
			t.OnSkipped(context, id, reason);
		}
	}

	public enum XUnitVersion
	{
		Unknown,
		XUnit,
		XUnit2
	}
}
