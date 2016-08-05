using System;
namespace MonoDevelop.UnitTesting.XUnit
{
	internal class TestCaseInfo
	{
		public string Id;
		public string Type;
		public string Method;
		public string DisplayName;
		public object[] Args;

		void parseName()
		{
			// this is [theory], xunit v2 where each theory is a separate test case
			if (Args != null && Args.Length > 0) {
				string[] typeParts = Type.Split('.');
				nameParts = new string[typeParts.Length + 2];
				typeParts.CopyTo(nameParts, 0);
				nameParts[typeParts.Length] = Method;
				DisplayName = Method + '(' + string.Join(",", Args) + ')';
				nameParts[typeParts.Length + 1] = DisplayName;
			}
			// this is [fact]
			else {
				string[] typeParts = Type.Split('.');
				nameParts = new string[typeParts.Length + 1];
				typeParts.CopyTo(nameParts, 0);
				nameParts[typeParts.Length] = Method;
				DisplayName = Method;
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

