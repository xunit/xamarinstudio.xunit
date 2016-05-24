//
// XUnitProjectTestSuite.cs
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

using System.IO;
using System.Linq;
using System.Collections.Generic;

using MonoDevelop.Projects;
using MonoDevelop.Ide;
using System;
using ProjectReference = MonoDevelop.Projects.ProjectReference;
using System.Threading.Tasks;
using MonoDevelop.XUnit;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.UnitTesting.XUnit
{
	/// <summary>
	/// Root test node for every project that has references to xunit dlls
	/// </summary>
	public class XUnitProjectTestSuite : XUnitAssemblyTestSuite
	{
		DotNetProject project;
		string resultsPath;
		string storeId;

		public XUnitProjectTestSuite(DotNetProject project) : base(project.Name, project)
		{
			this.project = project;
			storeId = Path.GetFileName(project.FileName);
			resultsPath = GetTestResultsDirectory(project.BaseDirectory);
			ResultsStore = new BinaryResultsStore(resultsPath, storeId);
			project.NameChanged += new SolutionItemRenamedEventHandler(OnProjectRenamed);
			IdeApp.ProjectOperations.EndBuild += new BuildEventHandler(OnProjectBuilt);
		}

		public override string AssemblyPath
		{
			get
			{
				return project.GetOutputFileName(IdeApp.Workspace.ActiveConfiguration);
			}
		}

		public override string CachePath
		{
			get
			{
				return Path.Combine(resultsPath, storeId + ".xunit-test-cache");
			}
		}

		public override IList<string> SupportAssemblies
		{
			get
			{
				return project.References // references that are not copied localy
					.Where(r => !r.LocalCopy && r.ReferenceType != ReferenceType.Package)
					.SelectMany(r => r.GetReferencedFileNames(IdeApp.Workspace.ActiveConfiguration)).ToList();
			}
		}

		public static XUnitProjectTestSuite CreateTest(DotNetProject project)
		{
			if (!project.ParentSolution.GetConfiguration(IdeApp.Workspace.ActiveConfiguration).BuildEnabledForItem(project))
				return null;

			foreach (var p in project.References)
			{
				var nv = GetXUnitVersion(p);
				if (nv != null)
					return new XUnitProjectTestSuite(project);
			}
			return null;
		}

		public static bool IsXUnitReference(ProjectReference p)
		{
			return GetXUnitVersion(p).HasValue;
		}

		public static XUnitVersion? GetXUnitVersion(ProjectReference p)
		{
			if (p.Reference == "xunit") // xUnit.Net 1.x
				return XUnitVersion.XUnit;
			if (p.Reference.IndexOf("xunit.core", StringComparison.OrdinalIgnoreCase) != -1) // xUnit.Net 2.x
				return XUnitVersion.XUnit2;

			return null;
		}

		protected override SourceCodeLocation GetSourceCodeLocation(string fixtureTypeNamespace, string fixtureTypeName, string testName)
		{
			if (string.IsNullOrEmpty(fixtureTypeName) || string.IsNullOrEmpty(fixtureTypeName))
				return null;
			var task = XUnitSourceCodeLocationFinder.TryGetSourceCodeLocationAsync(project, fixtureTypeNamespace, fixtureTypeName, testName);
			if (!task.Wait(2000))
				return null;
			return task.Result;
		}


		void OnProjectRenamed(object sender, SolutionItemRenamedEventArgs e)
		{
			UnitTestGroup parent = Parent as UnitTestGroup;
			if (parent != null)
				parent.UpdateTests();
		}

		void OnProjectBuilt(object s, BuildEventArgs args)
		{
			if (RefreshRequired)
				UpdateTests();
		}

		public override void Dispose()
		{
			project.NameChanged -= new SolutionItemRenamedEventHandler(OnProjectRenamed);
			IdeApp.ProjectOperations.EndBuild -= new BuildEventHandler(OnProjectBuilt);
			base.Dispose();
		}

		static string GetTestResultsDirectory(string baseDirectory)
		{
			var cacheDir = TypeSystemService.GetCacheDirectory(baseDirectory, true);
			var resultsDir = Path.Combine(cacheDir, "test-results");

			if (!Directory.Exists(resultsDir))
				Directory.CreateDirectory(resultsDir);

			return resultsDir;
		}
	}
}
