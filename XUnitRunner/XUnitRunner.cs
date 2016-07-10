using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MonoDevelop.UnitTesting.XUnit;
using MonoDevelop.XUnit;
using Xunit;
using Xunit.Abstractions;

namespace XUnitRunner
{
	public class XUnitRunner
	{
		public XUnitRunner()
		{
			PreloadAssemblies();
		}

		public static void PreloadAssemblies()
		{
			var path = Path.GetDirectoryName(typeof(XUnitRunner).Assembly.Location);
			Assembly.LoadFrom(Path.Combine(path, "xunit.abstractions.dll"));
			Assembly.LoadFrom(Path.Combine(path, "xunit.assert.dll"));
			Assembly.LoadFrom(Path.Combine(path, "xunit.core.dll"));
			Assembly.LoadFrom(Path.Combine(path, "xunit.execution.desktop.dll"));
			Assembly.LoadFrom(Path.Combine(path, "xunit.runner.utility.desktop.dll"));
		}

		//public void Run(string testAssemblyPath)
		//{
		//	if (null != testAssemblyPath && File.Exists(testAssemblyPath)) {
		//		using (var controller = new XunitFrontController(AppDomainSupport.Denied, testAssemblyPath))
		//		using (var discoveryVisitor = new DefaultDiscoveryVisitor())
		//		using (var executionVisitor = new DefaultExecutionVisitor()) {
		//			controller.Find(false, discoveryVisitor, TestFrameworkOptions.ForDiscovery());
		//			discoveryVisitor.Finished.WaitOne();

		//			controller.RunTests(discoveryVisitor.TestCases, executionVisitor, TestFrameworkOptions.ForExecution());
		//			executionVisitor.Finished.WaitOne();
		//		}
		//	}
		//}

		TestAssemblyConfiguration LoadTestAssemblyConfiguration(string assembly)
		{
			Type t = Type.GetType("Mono.Runtime");
			var conf = ConfigReader.Load(assembly);
			if (t != null) {
				//TODO support below
				conf.PreEnumerateTheories = true;
				conf.ParallelizeTestCollections = false;
				conf.AppDomain = AppDomainSupport.Denied;
			}
			return conf;
		}

		public XUnitTestInfo GetTestInfo(string path, string[] supportAssemblies)
		{
			var infos = new List<TestCaseInfo>();
			if (null != path && File.Exists(path)) {
				using (var controller = new XunitFrontController(AppDomainSupport.Denied, path))
				using (var discoveryVisitor = new DefaultDiscoveryVisitor()) {
					controller.Find(false, discoveryVisitor, TestFrameworkOptions.ForDiscovery());
					discoveryVisitor.Finished.WaitOne();
					foreach (var testCase in discoveryVisitor.TestCases) {
						infos.Add(new TestCaseInfo {
							Id = testCase.UniqueID,
							Type = testCase.TestMethod.TestClass.Class.Name,
							Method = testCase.TestMethod.Method.Name,
							DisplayName = testCase.DisplayName,
							Args = testCase.TestMethodArguments
						});
					}
				}
				// sort by type, method
				infos.Sort((info1, info2) => {
					int i = info1.Type.CompareTo(info2.Type);
					if (i == 0)
						i = info1.Method.CompareTo(info2.Method);
					return i;
				});
			}
			var testInfo = new XUnitTestInfo();
			BuildTestInfo(testInfo, infos, 0);
			return testInfo;
		}

		/// <summary>
		/// Organizes test case info as a hierarchy.
		/// </summary>
		/// <returns>The test info.</returns>
		/// <param name="testInfo">Test info.</param>
		/// <param name="infos">Infos.</param>
		/// <param name="step">Step.</param>
		void BuildTestInfo(XUnitTestInfo testInfo, IEnumerable<TestCaseInfo> infos, int step)
		{
			int count = infos.Count();

			if (count == 0)
				return;

			var firstItem = infos.First();

			// if the test is the last element in the group
			// then it's going to be a leaf node in the structure
			if (count == 1) {
				if (step == firstItem.NameParts.Length) {
					testInfo.Id = firstItem.Id;
					testInfo.Type = firstItem.Type;
					testInfo.Method = firstItem.Method;
					testInfo.Name = firstItem.Name;
					testInfo.Args = firstItem.Args;
					return;
				}
			}

			// build the tree structure based on the parts of the name, so
			// [a.b.c, a.b.d, a.e] would become
			//  (a)
			//   |-(b)
			//      |-(c)
			//      |-(d)
			//   |-(e)
			var groups = infos.GroupBy(info => info.NameParts[step]);
			var children = new List<XUnitTestInfo>();
			foreach (var group in groups) {
				var child = new XUnitTestInfo {
					Name = group.Key
				};

				BuildTestInfo(child, group, step + 1);
				children.Add(child);
			}
			testInfo.Tests = children.ToArray();
		}

		/// <summary>
		/// Execute test cases.
		/// </summary>
		/// <param name="assembly">Assembly.</param>
		/// <param name="testInfos">Test infos.</param>
		/// <param name="executionListener">Execution listener.</param>
		/// <remarks>It uses xunit execution engine to execute the test cases.</remarks>
		public void Execute(string assembly, string[] nameFilter, IXUnitExecutionListener executionListener)
		{
			var lookup = new HashSet<string>();
			foreach (var testId in nameFilter)
				lookup.Add(testId);

			TestAssemblyConfiguration conf = LoadTestAssemblyConfiguration(assembly);
			var discoveryOptions = TestFrameworkOptions.ForDiscovery(conf);
			var executionOptions = TestFrameworkOptions.ForExecution(conf);
			executionOptions.SetSynchronousMessageReporting(true);

			// we don't want to run every test in the assembly
			// only the tests passed in "testInfos" argument
			using (var controller = new XunitFrontController(conf.AppDomainOrDefault, assembly, null, conf.ShadowCopyOrDefault, null, new NullSourceInformationProvider()))
			using (var discoveryVisitor = new DefaultDiscoveryVisitor(tc => lookup.Contains(tc.UniqueID)))
			using (var executionVisitor = new DefaultExecutionVisitor(executionListener)) {
				controller.Find(false, discoveryVisitor, discoveryOptions);
				discoveryVisitor.Finished.WaitOne();

				controller.RunTests(discoveryVisitor.TestCases, executionVisitor, executionOptions);
				executionVisitor.Finished.WaitOne();
			}
		}

		class TestCaseInfo
		{
			public string Id;
			public string Type;
			public string Method;
			public string DisplayName;
			public object[] Args;

			string name;
			public string Name {
				get {
					if (name == null) {
						if (DisplayName.StartsWith(Type + "." + Method))
							name = Method;
						else
							name = DisplayName;
					}
					return name;
				}
			}

			void parseName()
			{
				// this is [theory], xunit v2 where each theory is a separate test case
				if (Args != null && Args.Length > 0) {
					string[] typeParts = Type.Split('.');
					nameParts = new string[typeParts.Length + 2];
					typeParts.CopyTo(nameParts, 0);
					nameParts[typeParts.Length] = DisplayName.Split('.').Last();
					nameParts[typeParts.Length + 1] = Method;
				}
				// this is [fact]
				else {
					string[] typeParts = Type.Split('.');
					nameParts = new string[typeParts.Length + 1];
					typeParts.CopyTo(nameParts, 0);
					nameParts[typeParts.Length] = Method;
				}
			}

			string[] nameParts;
			public string[] NameParts {
				get {
					if (nameParts == null)
						parseName();
					return nameParts;
				}
			}

		}
	}
}
