//
// XUnitAssemblyTestSuite.cs
//
// Author:
//   Lluis Sanchez Gual
//   Lex Li
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
// Copyright (C) 2016 Lex Li (https://lextudio.com)
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
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Projects;
using MonoDevelop.XUnit;

namespace MonoDevelop.UnitTesting.XUnit
{
    public abstract class XUnitAssemblyTestSuite : UnitTestGroup, IExecutableTest
	{
		object locker = new object();

		static XUnitTestLoader loader = new XUnitTestLoader();
		static XUnitTestExecutor executor = new XUnitTestExecutor();
		XUnitTestInfoCache cache;

		public abstract string AssemblyPath { get; }
		public abstract string CachePath { get; }
		public abstract IList<string> SupportAssemblies { get; }
		public DotNetProject Project { get; }

		DateTime lastAssemblyTime;
		XUnitExecutionSession session;

		UnitTest[] oldList;

		public XUnitAssemblyTestSuite(string name) : base(name)
		{
			cache = new XUnitTestInfoCache(this);
		}

		public XUnitAssemblyTestSuite(string name, DotNetProject project) : base(name, project)
		{
			cache = new XUnitTestInfoCache(this);
			Project = project;
		}

		public XUnitExecutionSession CreateExecutionSession(bool reportToMonitor)
		{
			session = new XUnitExecutionSession(this, reportToMonitor);

			foreach (var test in Tests)
			{
				var xunitTest = test as IExecutableTest;
				if (xunitTest != null)
				{
					var childSession = xunitTest.CreateExecutionSession(reportToMonitor);
					session.AddChildSession(childSession);
				}
			}

			return session;
		}

		public override bool HasTests
		{
			get
			{
				return true;
			}
		}

		internal SourceCodeLocation GetSourceCodeLocation(UnitTest test)
		{
			return GetSourceCodeLocation(test.FixtureTypeNamespace, test.FixtureTypeName, test.Name);
		}

		protected virtual SourceCodeLocation GetSourceCodeLocation(string fixtureTypeNamespace, string fixtureTypeName, string testName)
		{
			return null;
		}

		protected bool RefreshRequired
		{
			get
			{
				return lastAssemblyTime != GetAssemblyTime();
			}
		}

		DateTime GetAssemblyTime()
		{
			string path = AssemblyPath;
			if (File.Exists(path))
				return File.GetLastWriteTime(path);
			else
				return DateTime.MinValue;
		}

		protected override void OnCreateTests()
		{
			lock (locker)
			{
				if (Status == TestStatus.Loading)
					return;

				var testInfo = cache.GetTestInfo();
				if (testInfo != null)
				{
					FillTests(testInfo);
					return;
				}

				Status = TestStatus.Loading;
			}

			lastAssemblyTime = GetAssemblyTime();

			if (oldList != null) {
				foreach (UnitTest t in oldList)
					Tests.Add (t);
			}

			loader.AsyncLoadTestInfo(this, cache);
		}

		public override Task Refresh(CancellationToken ct)
		{
			return Task.Run(delegate
			{
				lock (locker)
				{
					try
					{
						while (Status == TestStatus.Loading)
						{
							Monitor.Wait(locker);
						}
						if (RefreshRequired)
						{
							lastAssemblyTime = GetAssemblyTime();
							UpdateTests();
							OnCreateTests(); // Force loading
							while (Status == TestStatus.Loading)
							{
								Monitor.Wait(locker);
							}
						}
					}
					catch
					{
					}
				}
			});
		}

		public void OnTestSuiteLoaded(XUnitTestInfo testInfo)
		{
			lock (locker)
			{
				Status = TestStatus.Ready;
				Monitor.PulseAll(locker);
			}

			if (testInfo.Tests == null && testInfo.Name == null)
			{
				return;
			}

			Runtime.RunInMainThread(delegate
			{
				AsyncCreateTests(testInfo);
			});
		}

		void AsyncCreateTests(XUnitTestInfo testInfo)
		{
			Tests.Clear();
			FillTests(testInfo);
			cache.SetTestInfo(testInfo);
			OnTestChanged();
		}

		void FillTests(XUnitTestInfo testInfo)
		{
			if (testInfo == null)
				return;

			if (testInfo.Tests == null)
				Tests.Add(new XUnitTestCase(this, executor, testInfo));
			else
				foreach (var child in testInfo.Tests)
				{
					UnitTest test;
					if (child.Tests == null)
						test = new XUnitTestCase(this, executor, child);
					else
						test = new XUnitTestSuite(this, executor, child);
					Tests.Add(test);
				}

			oldList = new UnitTest [Tests.Count];
			Tests.CopyTo (oldList, 0);
		}

		protected override bool OnCanRun(IExecutionHandler executionContext)
		{
			return Runtime.ProcessService.IsValidForRemoteHosting(executionContext);
		}

		protected override UnitTestResult OnRun(TestContext testContext)
		{
			return executor.RunAssemblyTestSuite(this, testContext);
		}

		protected override void OnActiveConfigurationChanged()
		{
			UpdateTests();
			base.OnActiveConfigurationChanged();
		}
	}
}
