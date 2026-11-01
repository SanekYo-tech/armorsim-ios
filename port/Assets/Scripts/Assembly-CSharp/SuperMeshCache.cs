using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public static class SuperMeshCache
{
	public struct Status
	{
		public SuperMeshVoxels voxels;

		public bool pending;

		public float progress;

		public ModelImportException refusal;

		public bool Ready => voxels != null && !pending;
	}

	private sealed class Job
	{
		public string key;

		public float progress;

		public Task<SuperMeshVoxels> task;

		public CancellationTokenSource cts;
	}

	private static readonly Dictionary<string, SuperMeshVoxels> _done = new Dictionary<string, SuperMeshVoxels>();

	private static readonly List<string> _order = new List<string>();

	private static readonly Dictionary<string, ModelImportException> _refused = new Dictionary<string, ModelImportException>();

	private static Job _job;

	private const int Keep = 32;

	public static SuperMeshVoxels GetOrCompute(SuperMeshRef r)
	{
		string key = r.VoxelKey;
		lock (_done)
		{
			if (_done.TryGetValue(key, out SuperMeshVoxels cached))
			{
				return cached;
			}
		}
		SuperMeshVoxels v = SuperMeshVoxelizer.Voxelize(SuperMeshStore.Get(r.source).parts, r.ModelToRegion, r.spacing, r.hu, r.hv, r.hn, refuseAmbiguous: false, null, default(CancellationToken));
		v.name = key;
		Put(key, v);
		return v;
	}

	public static Status Request(SuperMeshRef r)
	{
		Status st = default(Status);
		string key = r.VoxelKey;
		lock (_done)
		{
			if (_done.TryGetValue(key, out SuperMeshVoxels ready))
			{
				st.voxels = ready;
				return st;
			}
			if (_refused.TryGetValue(key, out ModelImportException refusal))
			{
				st.refusal = refusal;
				return st;
			}
		}
		Job j = _job;
		if (j != null && j.key == key)
		{
			if (j.task.IsCompleted)
			{
				_job = null;
				if (j.task.IsFaulted)
				{
					Exception ex = j.task.Exception?.GetBaseException();
					ModelImportException refuse = ex as ModelImportException;
					if (refuse != null)
					{
						lock (_done)
						{
							_refused[key] = refuse;
						}
						st.refusal = refuse;
						return st;
					}
					throw new ModelImportException("super.s40.refuse.lattice_cap", new object[3]
					{
						0f,
						0f,
						0f
					}, "Вокселизация упала: " + (ex?.ToString() ?? "?"));
				}
				if (j.task.IsCanceled)
				{
					st.pending = true;
					return st;
				}
				SuperMeshVoxels result = j.task.Result;
				result.name = key;
				Put(key, result);
				st.voxels = result;
				return st;
			}
			st.pending = true;
			st.progress = j.progress;
			return st;
		}
		if (j != null)
		{
			j.cts.Cancel();
		}
		Job job = new Job();
		job.key = key;
		job.cts = new CancellationTokenSource();
		Func<SuperMeshVoxels> fn = delegate
		{
			return GetOrCompute(r.CloneForPart(r.part, r.partName));
		};
		job.task = Task.Run(fn, job.cts.Token);
		_job = job;
		st.pending = true;
		return st;
	}

	private static void Put(string key, SuperMeshVoxels v)
	{
		lock (_done)
		{
			_done[key] = v;
			_order.Remove(key);
			_order.Add(key);
			while (_order.Count > Keep)
			{
				string oldest = _order[0];
				_done.Remove(oldest);
				_order.RemoveAt(0);
			}
		}
	}

	public static void Clear()
	{
		_job = null;
		_done.Clear();
		_order.Clear();
		_refused.Clear();
	}
}
