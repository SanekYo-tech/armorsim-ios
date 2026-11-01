using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

public static class SuperParallel
{
	public static void For(int fromInclusive, int toExclusive, int dop, Action<int> body)
	{
		ParallelOptions parallelOptions = new ParallelOptions();
		parallelOptions.MaxDegreeOfParallelism = Math.Max(1, dop);
		try
		{
			Parallel.For(fromInclusive, toExclusive, parallelOptions, body);
		}
		catch (AggregateException ae)
		{
			Rethrow(ae);
		}
	}

	public static void For<TLocal>(int fromInclusive, int toExclusive, int dop, Func<TLocal> localInit, Action<int, TLocal> body)
	{
		ParallelOptions parallelOptions = new ParallelOptions();
		parallelOptions.MaxDegreeOfParallelism = Math.Max(1, dop);
		try
		{
			Parallel.For(fromInclusive, toExclusive, parallelOptions, localInit, delegate(int i, ParallelLoopState state, TLocal local)
			{
				body(i, local);
				return local;
			}, delegate(TLocal _)
			{
			});
		}
		catch (AggregateException ae)
		{
			Rethrow(ae);
		}
	}

	private static void Rethrow(AggregateException ae)
	{
		ae = ae.Flatten();
		if (ae.InnerExceptions.Count > 0)
		{
			ExceptionDispatchInfo.Capture(ae.InnerExceptions[0]).Throw();
		}
	}
}
