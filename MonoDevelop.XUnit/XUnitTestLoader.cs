//
// XUnitTestLoader.cs
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
using System.Threading;
using MonoDevelop.Core;
using System.Linq;
using MonoDevelop.UnitTesting.XUnit;
using MonoDevelop.UnitTesting.XUnit.External;

namespace MonoDevelop.XUnit
{
	/// <summary>
	/// Loads data into XUnitAssemblyTestSuite using either xUnit.NET tools
	/// or cache when possible. This class intended to be instantiated only
	/// once and loads data asynchronously using internal queue.
	/// </summary>
	public class XUnitTestLoader
	{
		bool isRunning;
		readonly Queue<XUnitAssemblyTestSuite> loadQueue = new Queue<XUnitAssemblyTestSuite> ();

		/// <summary>
		/// Asyncs the load test case info.
		/// </summary>
		/// <returns>The load test info.</returns>
		/// <param name="testSuite">Test suite.</param>
		/// <param name="cache">Cache.</param>
		/// <remarks>
		/// It loads test case info asynchronously.
		/// </remarks>
		public void AsyncLoadTestInfo (XUnitAssemblyTestSuite testSuite, XUnitTestInfoCache cache)
		{
			lock (loadQueue) {
				if (!isRunning) {
					var thread = new Thread (new ThreadStart (() => RunAsyncLoadTestInfo (cache))) {
						Name = "xUnit.NET test loader",
						IsBackground = true
					};

					thread.Start ();
					isRunning = true;
				}
				loadQueue.Enqueue (testSuite);
				Monitor.Pulse (loadQueue);
			}
		}

		/// <summary>
		/// Runs the async load test info.
		/// </summary>
		/// <returns>The async load test info.</returns>
		/// <param name="cache">Cache.</param>
		/// <remarks>
		/// It loads the test case info from cache if there is something in the cache.
		/// 
		/// Otherwise, the test case info is queried from xunit, by executing a separate process of <seealso cref="XUnitTestRunner"/> 
		/// with MonoDevelop built-in .NET remoting helper.
		/// 
		/// To debug, the .NET remoting part can be commented out, and replaced with direct calls to <seealso cref="XUnitTestRunner"/>.
		/// </remarks>
		void RunAsyncLoadTestInfo (XUnitTestInfoCache cache)
		{
			while (true) {
				XUnitAssemblyTestSuite testSuite;
				lock (loadQueue) {
					if (loadQueue.Count == 0) {
						if (!Monitor.Wait (loadQueue, 5000, true)) {
							isRunning = false;
							return;
						}
					}
					testSuite = loadQueue.Dequeue ();
				}

				XUnitTestInfo testInfo;
				try {
					var runner = new XUnitRunner.XUnitRunner();
					testInfo = runner.GetTestInfo(testSuite.AssemblyPath, testSuite.SupportAssemblies.ToArray());
					testSuite.OnTestSuiteLoaded(testInfo);
				} catch (Exception ex) {
					LoggingService.LogError(ex.ToString());
				}
			}
		}
	}
}
