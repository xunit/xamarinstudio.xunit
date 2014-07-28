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
using MonoDevelop.NUnit;
using System.Collections.Generic;
using System.Threading;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using System.Linq;
using XUnitRunner;

namespace MonoDevelop.XUnit
{
	public class XUnitTestLoader
	{
		bool isRunning = false;
		Queue<XUnitAssemblyTestSuite> loadQueue = new Queue<XUnitAssemblyTestSuite> ();

		public void AsyncLoadTestSuite (XUnitAssemblyTestSuite testSuite)
		{
			lock (loadQueue) {
				if (!isRunning) {
					var thread = new Thread (new ThreadStart (RunAsyncLoadTestSuite)) {
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

		void RunAsyncLoadTestSuite ()
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

				using (var runner = (XUnitTestRunner)Runtime.ProcessService.CreateExternalProcessObject (typeof(XUnitTestRunner), false)) {
					testSuite.TestInfo = runner.GetTestInfo (testSuite.Assembly, testSuite.SupportAssemblies.ToArray());
				}

				testSuite.OnTestSuiteLoaded ();
			}
		}
	}

}
