using System;
using Xunit;
using Xunit.Abstractions;

namespace XUnitRunner
{
	public class DefaultExecutionVisitor:TestMessageVisitor<ITestAssemblyFinished>
	{
		IXUnitExecutionListener executionListener;

		public DefaultExecutionVisitor(IXUnitExecutionListener executionListener)
		{
			this.executionListener = executionListener;
		}

		protected override bool Visit(ITestCaseStarting testCaseStarting)
		{
			executionListener.OnTestCaseStarting(testCaseStarting.TestCase.UniqueID);
			return true;
		}

		protected override bool Visit(ITestCaseFinished testCaseFinished)
		{
			executionListener.OnTestCaseFinished(testCaseFinished.TestCase.UniqueID);
			return true;
		}

		protected override bool Visit(ITestFailed testFailed)
		{
			executionListener.OnTestFailed(testFailed.TestCase.UniqueID,
				testFailed.ExecutionTime, testFailed.Output, testFailed.ExceptionTypes, testFailed.Messages, testFailed.StackTraces);
			return true;
		}

		protected override bool Visit(ITestPassed testPassed)
		{
			executionListener.OnTestPassed(testPassed.TestCase.UniqueID, testPassed.ExecutionTime, testPassed.Output);
			return true;
		}

		protected override bool Visit(ITestSkipped testSkipped)
		{
			executionListener.OnTestSkipped(testSkipped.TestCase.UniqueID, testSkipped.Reason);
			return true;
		}
	}
}

