//
// XUnitTestExecutor.cs
//
// Author:
//       Sergey Khabibullin <sergey@khabibullin.com>
//
// Copyright (c) 2014 Sergey Khabibullin
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using MonoDevelop.Core;
using System.Linq;
using System.Runtime.Remoting;
using MonoDevelop.UnitTesting.XUnit;
using MonoDevelop.UnitTesting;
using Newtonsoft.Json;

namespace MonoDevelop.XUnit
{
	/// <summary>
	/// Wrapper around XUnitTestRunner. It extracts all information needed to
	/// run tests, then dispatches rusults back to tests.
	/// </summary>
	internal class XUnitTestExecutor
	{
		/// <summary>
		/// Runs a single test case.
		/// </summary>
		/// <returns>The test case.</returns>
		/// <param name="rootSuite">Root suite.</param>
		/// <param name="testCase">Test case.</param>
		/// <param name="context">Context.</param>
		public UnitTestResult RunTestCase (XUnitAssemblyTestSuite rootSuite, XUnitTestCase testCase, TestContext context)
		{
			return Run (new List<XUnitTestCase> { testCase }, rootSuite, testCase, context, false);
		}

		/// <summary>
		/// Runs the entire test suite.
		/// </summary>
		/// <returns>The test suite.</returns>
		/// <param name="rootSuite">Root suite.</param>
		/// <param name="testSuite">Test suite.</param>
		/// <param name="context">Context.</param>
		public UnitTestResult RunTestSuite (XUnitAssemblyTestSuite rootSuite, XUnitTestSuite testSuite, TestContext context)
		{
			var testCases = new List<XUnitTestCase> ();
			CollectTestCases (testCases, testSuite);

			return Run (testCases, rootSuite, testSuite, context);
		}

		/// <summary>
		/// Runs all test suites in the assembly.
		/// </summary>
		/// <returns>The assembly test suite.</returns>
		/// <param name="assemblyTestSuite">Assembly test suite.</param>
		/// <param name="context">Context.</param>
		public UnitTestResult RunAssemblyTestSuite (XUnitAssemblyTestSuite assemblyTestSuite, TestContext context)
		{
			var testCases = new List<XUnitTestCase> ();

			foreach (var test in assemblyTestSuite.Tests) {
				var testSuite = test as XUnitTestSuite;
				if (testSuite != null)
					CollectTestCases (testCases, testSuite);
			}

			return Run (testCases, assemblyTestSuite, assemblyTestSuite, context);
		}

		void CollectTestCases (List<XUnitTestCase> testCases, XUnitTestSuite testSuite)
		{
			foreach (var test in testSuite.Tests) {
				if (test is XUnitTestCase)
					testCases.Add ((XUnitTestCase)test);
				else
					CollectTestCases (testCases, (XUnitTestSuite)test);
			}
		}

		/// <summary>
		/// Run the specified testCases, rootSuite, test, context and reportToMonitor.
		/// </summary>
		/// <param name="testCases">Test cases.</param>
		/// <param name="rootSuite">Root suite.</param>
		/// <param name="test">Test.</param>
		/// <param name="context">Context.</param>
		/// <param name="reportToMonitor">Report to monitor.</param>
		/// <remarks>
		/// This is actual the code that executes the test cases.
		/// 
		/// It uses the MonoDevelop built-in .NET remoting helper to execute the code of <seealso cref="XUnitTestRunner"/> in a separate process.
		/// 
		/// If any debugging is required, simply comment out the remoting part, and call <seealso cref="XUnitTestRunner"/> directly,
		/// so that the code executes inside MonoDevelop.
		/// </remarks>
		UnitTestResult Run (List<XUnitTestCase> testCases, XUnitAssemblyTestSuite rootSuite, IExecutableTest test, TestContext context, bool reportToMonitor = true)
		{
			using (var session = test.CreateExecutionSession (reportToMonitor)) {
				var executionListener = new RemoteExecutionListener (new LocalExecutionListener (context, testCases));
				RemotingServices.Marshal (executionListener, null, typeof (IXUnitExecutionListener));

				XUnitTestRunner runner = (XUnitTestRunner)Runtime.ProcessService.CreateExternalProcessObject (typeof (XUnitTestRunner),
					context.ExecutionContext.ExecutionHandler, rootSuite.SupportAssemblies);

				var data = JsonConvert.SerializeObject(testCases.Select(tc => tc.TestInfo).ToArray());
				try {
					runner.Execute (rootSuite.AssemblyPath, data, executionListener);
				} catch (Exception ex) {
					Console.WriteLine (ex);
				} finally {
					runner.Dispose ();
				}

				return session.Result;
			}
		}
	}

	/// <summary>
	/// Remote execution listener.
	/// </summary>
	/// <remarks>
	/// It is used to pass cancellation status between the add-in and the remote process. Thus, <see cref="SerializableAttribute"/> is required.
	/// 
	/// It wraps the <see cref="LocalExecutionListener"/>.
	/// </remarks>
	[Serializable]
	public class RemoteExecutionListener: MarshalByRefObject, IXUnitExecutionListener
	{
		IXUnitExecutionListener localListener;

		public bool IsCancelRequested {
			get {
				return localListener.IsCancelRequested;
			}
		}

		public RemoteExecutionListener (IXUnitExecutionListener localListener)
		{
			this.localListener = localListener;
		}

		public void OnTestCaseStarting (string id)
		{
			localListener.OnTestCaseStarting (id);
		}

		public void OnTestCaseFinished (string id)
		{
			localListener.OnTestCaseFinished (id);
		}

		public void OnTestFailed (string id, decimal executionTime, string output, string[] exceptionTypes, string[] messages, string[] stackTraces)
		{
			localListener.OnTestFailed (id, executionTime, output, exceptionTypes, messages, stackTraces);
		}

		public void OnTestPassed (string id, decimal executionTime, string output)
		{
			localListener.OnTestPassed (id, executionTime, output);
		}

		public void OnTestSkipped (string id, string reason)
		{
			localListener.OnTestSkipped (id, reason);
		}
	}

	/// <summary>
	/// Local execution listener.
	/// </summary>
	/// <remarks>
	/// It is the actual listener in the add-in process, which is wrapped by <see cref="RemoteExecutionListener"/> passed through .NET remoting boundary.
	/// </remarks>
	internal class LocalExecutionListener: IXUnitExecutionListener
	{
		TestContext context;
		Dictionary<string, XUnitTestCase> lookup;

		public LocalExecutionListener (TestContext context, List<XUnitTestCase> testCases)
		{
			this.context = context;

			// create a lookup table so later we can identify a test case by it's id
			lookup = testCases.ToDictionary (tc => tc.TestInfo.Id);
		}

		public bool IsCancelRequested {
			get {
				return context.Monitor.CancellationToken.IsCancellationRequested;
			}
		}

		public void OnTestCaseStarting (string id)
		{
			var testCase = lookup [id];
			testCase.OnStarting (context, id);
		}

		public void OnTestCaseFinished (string id)
		{
			var testCase = lookup [id];
			testCase.OnFinished (context, id);
		}

		public void OnTestFailed (string id, decimal executionTime, string output, string[] exceptionTypes, string[] messages, string[] stackTraces)
		{
			var testCase = lookup [id];
			testCase.OnFailed (context, id, executionTime, output, exceptionTypes, messages, stackTraces);
		}

		public void OnTestPassed (string id, decimal executionTime, string output)
		{
			var testCase = lookup [id];
			testCase.OnPassed (context, id, executionTime, output);
		}

		public void OnTestSkipped (string id, string reason)
		{
			var testCase = lookup [id];
			testCase.OnSkipped (context, id, reason);
		}
	}
}
