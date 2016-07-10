using System;
using Xunit;

namespace XUnitRunner
{
	[Collection("col")]
	public class Sample
	{
		[Fact]
		public void TestFoo()
		{
			Assert.Equal(1,1);
		}

		[Fact]
		public void TestBar()
		{
			Assert.Equal(2, 1);
		}

		[Theory]
		[InlineData(1)]
		[InlineData(2)]
		public void TestTheory(int a)
		{
			Assert.Equal(a, 1);
		}
	}
}

