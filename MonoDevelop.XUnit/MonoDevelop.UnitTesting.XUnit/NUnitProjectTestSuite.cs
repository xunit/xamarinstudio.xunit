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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MonoDevelop.Ide;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;
using MonoDevelop.UnitTesting.XUnit.External;
using ProjectReference = MonoDevelop.Projects.ProjectReference;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MonoDevelop.UnitTesting.XUnit
{
    /// <summary>
    /// Root test node for every project that has references to xunit dlls.
    /// </summary>
    public class XUnitProjectTestSuite : XUnitAssemblyTestSuite
    {
        DotNetProject project;
        string resultsPath;
        string storeId;
		static string PROJECT_JSON = "project.json";

        public XUnitProjectTestSuite (DotNetProject project) : base (project.Name, project)
        {
            this.project = project;
            storeId = Path.GetFileName (project.FileName);
            resultsPath = GetTestResultsDirectory (project.BaseDirectory);
            ResultsStore = new BinaryResultsStore (resultsPath, storeId);
            project.NameChanged += OnProjectRenamed;
            IdeApp.ProjectOperations.EndBuild += OnProjectBuilt;
        }

        /// <summary>
        /// Gets the assembly path.
        /// </summary>
        /// <value>The assembly path.</value>
        public override string AssemblyPath {
            get {
                return project.GetOutputFileName (IdeApp.Workspace.ActiveConfiguration);
            }
        }

        /// <summary>
        /// Gets the cache path.
        /// </summary>
        /// <value>The cache path.</value>
        public override string CachePath {
            get {
                return Path.Combine (resultsPath, storeId + ".xunit-test-cache");
            }
        }

        /// <summary>
        /// Gets the support assemblies for the project.
        /// </summary>
        /// <value>The support assemblies.</value>
        public override IList<string> SupportAssemblies {
            get {
                return project.References // references that are not copied localy
                    .Where (r => !r.LocalCopy && r.ReferenceType != ReferenceType.Package)
                    .SelectMany (r => r.GetReferencedFileNames (IdeApp.Workspace.ActiveConfiguration)).ToList ();
            }
        }

        /// <summary>
        /// Creates the test.
        /// </summary>
        /// <returns>The test.</returns>
        /// <param name="project">Project.</param>
        /// <remarks>This is where all test cases are located.</remarks>
		public static XUnitProjectTestSuite CreateTest (DotNetProject project)
        {
            if (!project.ParentSolution.GetConfiguration (IdeApp.Workspace.ActiveConfiguration).BuildEnabledForItem (project))
                return null;

			// First test against nuget V3 API first
			if (HasXunitReference(project))
				return new XUnitProjectTestSuite(project);
			else {
				foreach (var p in project.References) {
					var nv = GetXUnitVersion(p);
					if (nv != null)
						return new XUnitProjectTestSuite(project);
				}
			}

            return null;
        }

        /// <summary>
        /// Tests if a reference is from xunit.net.
        /// </summary>
        /// <returns><code>true</code> if the reference is from xunit.net. Otherwise, <code>false</code>.</returns>
        /// <param name="p">Project reference.</param>
		public static bool IsXUnitReference (ProjectReference p)
        {
            return GetXUnitVersion (p).HasValue;
        }

		/// <summary>
		/// Tests if a reference is from xunit.net.
		/// </summary>
		/// <returns><code>true</code> if the reference is from xunit.net. Otherwise, <code>false</code>.</returns>
		/// <param name="args">Any changed file in the project.</param>
		public static bool IsXUnitRelevant(ProjectFileEventArgs args)
		{
			return IsProjectJson(args);
		}

        /// <summary>
        /// Gets the xunit.net version from the reference.
        /// </summary>
        /// <returns>The xunit.net version.</returns>
        /// <param name="p">Project reference.</param>
		public static XUnitVersion? GetXUnitVersion (ProjectReference p)
        {
            if (p.Reference == "xunit") // xUnit.Net 1.x
                return XUnitVersion.XUnit;
            if (p.Reference.IndexOf ("xunit.core", StringComparison.OrdinalIgnoreCase) != -1) // xUnit.Net 2.x
                return XUnitVersion.XUnit2;

            return null;
        }


		/// <summary>
		/// Gets the source code location.
		/// </summary>
		/// <returns>The source code location.</returns>
		/// <param name="fixtureTypeNamespace">Fixture type namespace.</param>
		/// <param name="fixtureTypeName">Fixture type name.</param>
		/// <param name="testName">Test name.</param>
		/// <remarks>
		/// The NUnit source code locator is reused.
		/// </remarks>
        protected override SourceCodeLocation GetSourceCodeLocation (string fixtureTypeNamespace, string fixtureTypeName, string testName)
        {
            if (string.IsNullOrEmpty (fixtureTypeName) || string.IsNullOrEmpty (fixtureTypeName))
                return null;
            var task = NUnitSourceCodeLocationFinder.TryGetSourceCodeLocationAsync (project, fixtureTypeNamespace, fixtureTypeName, testName);
            if (!task.Wait (2000))
                return null;
            return task.Result;
        }

		protected static string GetAssemblyPath(Project project)
		{
			return project.GetOutputFileName(IdeApp.Workspace.ActiveConfiguration);
		}

		protected static bool HasProjectJson(Project project)
		{
			return project.MSBuildProject.ItemGroups.SelectMany(x => x.Items).Where(x => x.Include == XUnitProjectTestSuite.PROJECT_JSON).Count() > 0;
		}

		protected static bool IsProjectJson(ProjectFileEventArgs args)
		{
			var projectEvent = args.FirstOrDefault();
			return projectEvent.ProjectFile.Name.IndexOf(XUnitProjectTestSuite.PROJECT_JSON, StringComparison.OrdinalIgnoreCase) != -1 &&
				               HasProjectJson(projectEvent.Project);
		}

		protected static bool HasXunitReference(Project project)
		{
			if (HasProjectJson(project)) {
				var jsonFile = Path.Combine(project.BaseDirectory, "project.json");
				if (File.Exists(jsonFile)) {
					using (StreamReader file = File.OpenText(jsonFile))
					using (JsonTextReader reader = new JsonTextReader(file)) {
						var jProject = JToken.ReadFrom(reader);
						var dependencies = jProject["dependencies"].Value<JObject>();
						List<string> dependency = dependencies.Properties().Select(p => p.Name).ToList();
						return dependency.Count(x => x == "xunit") > 0;
					}
				}
			}

			return false;
		}

        void OnProjectRenamed (object sender, SolutionItemRenamedEventArgs e)
        {
            UnitTestGroup parent = Parent as UnitTestGroup;
            if (parent != null)
                parent.UpdateTests ();
        }

        void OnProjectBuilt (object s, BuildEventArgs args)
        {
            if (RefreshRequired)
                UpdateTests ();
        }

        public override void Dispose ()
        {
            project.NameChanged -= OnProjectRenamed;
            IdeApp.ProjectOperations.EndBuild -= OnProjectBuilt;
            base.Dispose ();
        }

        static string GetTestResultsDirectory (string baseDirectory)
        {
            var cacheDir = TypeSystemService.GetCacheDirectory (baseDirectory, true);
            var resultsDir = Path.Combine (cacheDir, "test-results");

            if (!Directory.Exists (resultsDir))
                Directory.CreateDirectory (resultsDir);

            return resultsDir;
        }
    }
}
