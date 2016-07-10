using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace XUnitRunner
{
	public class DefaultDiscoveryVisitor:TestMessageVisitor<IDiscoveryCompleteMessage>
	{
		public List<ITestCase> TestCases { get; private set; }
		Func<ITestCase, bool> filter;

		public DefaultDiscoveryVisitor()
		{
			TestCases = new List<ITestCase>();
		}

		public DefaultDiscoveryVisitor(Func<ITestCase, bool> filter): this ()
		{
			this.filter = filter;
		}

		protected override bool Visit(ITestCaseDiscoveryMessage discovery)
		{
			if (filter == null || filter(discovery.TestCase))
				TestCases.Add(discovery.TestCase);

			return true;
		}
	}
}

