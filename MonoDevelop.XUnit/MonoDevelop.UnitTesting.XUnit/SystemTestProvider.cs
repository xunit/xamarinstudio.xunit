//
// SystemTestProvider.cs
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
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using RollbarDotNet;

namespace MonoDevelop.UnitTesting.XUnit
{
	/// <summary>
	/// xunit.net test provider.
	/// </summary>
	/// <remarks>This is where the xunit.net extension hooks itself to MonoDevelop/Xamarin Studio.</remarks>
	public class SystemTestProvider : ITestProvider
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:MonoDevelop.UnitTesting.XUnit.SystemTestProvider"/> class.
		/// </summary>
		public SystemTestProvider ()
		{
			var attribute = GetAssemblyAttribute<AddinAttribute> (Assembly.GetExecutingAssembly ());
			LoggingService.LogInfo ($"xUnit.net for MonoDevelop/Xamarin Studio version {attribute.Version}");
			RegisterRollbar(attribute.Version);
			IdeApp.Workspace.ReferenceAddedToProject += OnReferenceChanged;
			IdeApp.Workspace.ReferenceRemovedFromProject += OnReferenceChanged;			
		}

		private static bool rollbarInitialized = false;

		private static void RegisterRollbar(string version)
		{
			if (rollbarInitialized)
			{
				return;
			}

			rollbarInitialized = true;
			Rollbar.Init(new RollbarConfig {
				AccessToken = "ab0cc28be33c444fbf877dd320c89598",
				Environment = "production"
			});
			var userName = $"{version}";
			Rollbar.PersonData(() => new Person(version) {
				UserName = userName
			});

			AppDomain.CurrentDomain.UnhandledException += (sender, args) => {
				Rollbar.Report(args.ExceptionObject as System.Exception);
			};

			TaskScheduler.UnobservedTaskException += (sender, args) => {
				Rollbar.Report(args.Exception);
			};
		}

		static T GetAssemblyAttribute<T>(System.Reflection.Assembly ass) where T : Attribute
		{
			object[] attributes = ass.GetCustomAttributes(typeof(T), false);
			if (attributes == null || attributes.Length == 0)
				return null;
			return attributes.OfType<T>().SingleOrDefault();
		}

		/// <summary>
		/// Creates the unit test.
		/// </summary>
		/// <returns>The unit test.</returns>
		/// <param name="entry">Entry.</param>
		/// <remarks>
		/// This is where unit testing integration starts.
		/// </remarks>
		public UnitTest CreateUnitTest (WorkspaceObject entry)
		{
			UnitTest test = null;

			var dotnet = entry as DotNetProject;
			if (dotnet != null)
				test = XUnitProjectTestSuite.CreateTest (dotnet);

			UnitTestGroup grp = test as UnitTestGroup;
			if (grp != null && !grp.HasTests) {
				test.Dispose ();
				return null;
			}

			return test;
		}

		void OnReferenceChanged (object s, ProjectReferenceEventArgs args)
		{
			if (XUnitProjectTestSuite.IsXUnitReference (args.ProjectReference))
				UnitTestService.ReloadTests (); // trigger a panel refresh.
		}

		/// <summary>
		/// Releases all resource used by the <see cref="T:MonoDevelop.UnitTesting.XUnit.SystemTestProvider"/> object.
		/// </summary>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the
		/// <see cref="T:MonoDevelop.UnitTesting.XUnit.SystemTestProvider"/>. The <see cref="Dispose"/> method leaves
		/// the <see cref="T:MonoDevelop.UnitTesting.XUnit.SystemTestProvider"/> in an unusable state. After calling
		/// <see cref="Dispose"/>, you must release all references to the
		/// <see cref="T:MonoDevelop.UnitTesting.XUnit.SystemTestProvider"/> so the garbage collector can reclaim the
		/// memory that the <see cref="T:MonoDevelop.UnitTesting.XUnit.SystemTestProvider"/> was occupying.</remarks>
		public void Dispose ()
		{
			IdeApp.Workspace.ReferenceAddedToProject -= OnReferenceChanged;
			IdeApp.Workspace.ReferenceRemovedFromProject -= OnReferenceChanged;
		}
	}
}
