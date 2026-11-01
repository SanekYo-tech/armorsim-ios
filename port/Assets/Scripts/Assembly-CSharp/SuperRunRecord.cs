using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

[Serializable]
public sealed class SuperRunRecord
{
	[Serializable]
	public sealed class Predicted
	{
		public long particles;

		public long gridNodes;

		public long gpuBytes;

		public int steps;

		public double solveS;

		public double solveLowS;

		public double solveHighS;

		public double tapeS;

		public double tapeLowS;

		public double tapeHighS;

		public Predicted()
		{
			solveS = double.NaN;
			solveLowS = double.NaN;
			solveHighS = double.NaN;
			tapeS = double.NaN;
			tapeLowS = double.NaN;
			tapeHighS = double.NaN;
		}
	}

	[Serializable]
	public sealed class Actual
	{
		public long particles;

		public long gridNodes;

		public long gpuBytesAllocated;

		public int steps;

		public double wallS;

		public double solveS;

		public double tapeS;

		public double bakeUploadS;

		public Actual()
		{
			steps = -1;
			wallS = double.NaN;
			solveS = double.NaN;
			tapeS = double.NaN;
			bakeUploadS = double.NaN;
		}
	}

	[Serializable]
	public sealed class HookStages
	{
		public double downloadMs;

		public double fieldsMs;

		public double filterMs;

		public double densityMs;

		public double polygonizeMs;

		public double decimateMs;

		public double signedVolumeMs;

		public double particleHashMs;

		public double vertexFieldsMs;

		public double fieldBlockMs;

		public double telemetryMs;

		public double debrisMs;

		public double classifyMs;

		public double encodeMs;

		public double writeMs;

		public double checkpointMs;

		public double enqueueMs;

		public double reconDispatchMs;

		public double reconWaitMs;

		public double sumMs;

		public double keyframeMeanMs;

		public double keyframeMaxMs;

		public double hookMainMeanMs;

		public double hookMainMaxMs;
	}

	[Serializable]
	public sealed class Perf
	{
		public double bakeS;

		public double uploadS;

		public double stallMaxMs;

		public double firstMoveMs;

		public int moves;

		public double cpuUtil;

		public double peakWorkingSetMB;

		public double peakManagedMB;

		public double processPeakWorkingSetMB;

		public double stepLoopS;

		public int inFlightK;

		public int intraDop;

		public int poolHighWater;

		public int framesBuiltOffMain;

		public double hookWaitS;

		public double drainS;

		public double perFrameBytesEstimate;

		public double availablePhysMB;

		public double yieldS;

		public int inFlightKStart;

		public double perFrameBytesMeasured;

		public double perFrameBytesModel;

		public double paceYieldS;

		public double paceMsPerStep;

		public int paceStepsMin;

		public int paceStepsMax;

		public int paceYieldCount;

		public int paceCalibrations;

		public long chunkDispatches;

		public string topStalls;

		public int gcCount;

		public double regionFirstFraction;

		public double regionLastFraction;

		public long sparseVBlocks;

		public long sparsePoolBlocks;

		public long sparseActiveFirst;

		public long sparseActiveMax;

		public int sparseGrowths;

		public long sparseTableBytes;

		public long sparsePoolBytes;

		public float displayMarginM;

		public float solverMarginM;

		public int pauseGaps;

		public int pauseGcsInside;

		public double pauseSumS;

		public double pauseMaxMs;

		public double pauseShareOfWall;

		public int pauseFloorGaps;

		public double pauseFloorSumS;

		public double pauseFloorWallS;

		public double cpuP10;

		public double cpuP50;

		public double cpuMinSample;

		public int cpuSamples;

		public double occBuilding;

		public double occInFlight;

		public double occReadyUnwritten;

		public string kdopChanges;

		public int inFlightKMin;

		public int intraDopMin;

		public int intraDopMax;

		public int lookAheadQ;

		public double readyCapMB;

		public double frameLatticeMaxMB;

		public double frameLatticeMeanMB;

		public double frameMeshMaxMB;

		public double frameMeshMeanMB;

		public double frameFieldsMaxMB;

		public double frameFieldsMeanMB;

		public double framePayloadMaxMB;

		public double framePayloadMeanMB;

		public double frameSnapshotMB;

		public double frameHonestMaxMB;

		public double frameHonestMeanMB;

		public double frameBodyMaxMB;

		public double frameDensityMaxMB;

		public double frameDensityWorkMaxMB;

		public double frameHashMaxMB;

		public int reconSlots;

		public int reconSlotWaits;

		public int reconFramesGpu;

		public int reconVertexRequests;

		public double reconSlotWaitS;

		public double reconHookMsMean;

		public double reconHookMsMax;

		public double reconHookGpuMs;

		public double reconReadbackMsP95;

		public double reconVramMB;

		public long reconKeyMismatch;

		public long reconNodeMismatch;

		public long reconKeysChecked;

		public long reconAabbChecks;

		public Perf()
		{
			bakeS = double.NaN;
			uploadS = double.NaN;
			stallMaxMs = double.NaN;
			firstMoveMs = double.NaN;
			cpuUtil = double.NaN;
			peakWorkingSetMB = double.NaN;
			peakManagedMB = double.NaN;
			processPeakWorkingSetMB = double.NaN;
			stepLoopS = double.NaN;
			hookWaitS = double.NaN;
			drainS = double.NaN;
			perFrameBytesEstimate = double.NaN;
			availablePhysMB = double.NaN;
			yieldS = double.NaN;
			perFrameBytesMeasured = double.NaN;
			perFrameBytesModel = double.NaN;
			paceYieldS = double.NaN;
			paceMsPerStep = double.NaN;
			topStalls = "";
			regionFirstFraction = double.NaN;
			regionLastFraction = double.NaN;
			sparseVBlocks = -1L;
			sparsePoolBlocks = -1L;
			sparseActiveFirst = -1L;
			sparseActiveMax = -1L;
			sparseTableBytes = -1L;
			sparsePoolBytes = -1L;
			pauseSumS = double.NaN;
			pauseMaxMs = double.NaN;
			pauseShareOfWall = double.NaN;
			pauseFloorGaps = -1;
			pauseFloorSumS = double.NaN;
			pauseFloorWallS = double.NaN;
			cpuP10 = double.NaN;
			cpuP50 = double.NaN;
			cpuMinSample = double.NaN;
			occBuilding = double.NaN;
			occInFlight = double.NaN;
			occReadyUnwritten = double.NaN;
			kdopChanges = "";
			readyCapMB = double.NaN;
			frameLatticeMaxMB = double.NaN;
			frameLatticeMeanMB = double.NaN;
			frameMeshMaxMB = double.NaN;
			frameMeshMeanMB = double.NaN;
			frameFieldsMaxMB = double.NaN;
			frameFieldsMeanMB = double.NaN;
			framePayloadMaxMB = double.NaN;
			framePayloadMeanMB = double.NaN;
			frameSnapshotMB = double.NaN;
			frameHonestMaxMB = double.NaN;
			frameHonestMeanMB = double.NaN;
			frameBodyMaxMB = double.NaN;
			frameDensityMaxMB = double.NaN;
			frameDensityWorkMaxMB = double.NaN;
			frameHashMaxMB = double.NaN;
			reconSlots = -1;
			reconSlotWaits = -1;
			reconFramesGpu = -1;
			reconVertexRequests = -1;
			reconSlotWaitS = double.NaN;
			reconHookMsMean = double.NaN;
			reconHookMsMax = double.NaN;
			reconHookGpuMs = double.NaN;
			reconReadbackMsP95 = double.NaN;
			reconVramMB = double.NaN;
			reconKeyMismatch = -1L;
			reconNodeMismatch = -1L;
			reconKeysChecked = -1L;
			reconAabbChecks = -1L;
		}
	}

	[Serializable]
	public sealed class View
	{
		public float cutAzimuthDeg;
	}

	public enum FailClass
	{
		None = 0,
		SolverRefused = 1,
		QualityGuard = 2,
		Resource = 3
	}

	public const int CurrentVersion = 1;

	public const string SidecarSuffix = ".run.json";

	public int v;

	public string utc;

	public string host;

	public string app;

	public string unity;

	public string device;

	public string api;

	public int vramMB;

	public int ramMB;

	public string kind;

	public string scene;

	public string tape;

	public float spacingM;

	public float durationS;

	public float marginM;

	public int frames;

	public long tapeBytes;

	public Predicted predicted;

	public Actual actual;

	public int pipelineVersion;

	public int workers;

	public string gpuPath;

	public string fieldsPath;

	public string fieldsPathWhy;

	public string reconReadback;

	public int xiScatter;

	public float xiM;

	public long xiSalt;

	public double s43Alpha;

	public double s43Beta;

	public double s43VRefMm3;

	public double s43HRefMm;

	public HookStages hookStages;

	public Perf perf;

	public View view;

	public bool ok;

	public string failure;

	public int unityErrors;

	public int allocFailures;

	public int nativeLogFailures;

	public string invalidReason;

	public FailClass failClass;

	public int resumedFromStep;

	public bool completed;

	public SuperRunRecord()
	{
		v = 1;
		utc = "";
		host = "";
		app = "";
		unity = "";
		device = "";
		api = "";
		kind = "";
		scene = "";
		tape = "";
		predicted = new Predicted();
		actual = new Actual();
		gpuPath = "";
		fieldsPath = "";
		fieldsPathWhy = "";
		reconReadback = "";
		hookStages = new HookStages();
		perf = new Perf();
		view = new View();
		failure = "";
		invalidReason = "";
	}

	public static SuperRunRecord NewOnThisMachine()
	{
		SuperRunRecord r = new SuperRunRecord();
		r.utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
		r.host = (Application.isEditor ? "editor" : "player");
		r.app = Application.version ?? "";
		r.unity = Application.unityVersion ?? "";
		r.device = SystemInfo.graphicsDeviceName ?? "";
		r.api = SystemInfo.graphicsDeviceVersion ?? "";
		r.vramMB = SystemInfo.graphicsMemorySize;
		r.ramMB = SystemInfo.systemMemorySize;
		return r;
	}

	public void FillPredicted(SuperScenePlan plan)
	{
		if (plan == null)
		{
			throw new ArgumentNullException("plan");
		}
		kind = plan.kind ?? "";
		spacingM = plan.spacingM;
		durationS = plan.durationS;
		marginM = plan.marginM;
		predicted.particles = plan.particles;
		predicted.gridNodes = plan.gridNodes;
		predicted.gpuBytes = plan.gpuBytesTotal;
		predicted.steps = plan.steps;
		predicted.solveS = plan.solveSecondsPredicted;
		SuperScenePlan.SolveBand(plan.solveSecondsPredicted, out predicted.solveLowS, out predicted.solveHighS);
	}

	public void SetTapePrediction(double medianK, double minK, double maxK, long particleCount, int expectedFrames)
	{
		double particleFrames = (double)particleCount * expectedFrames;
		predicted.tapeS = particleFrames * medianK;
		predicted.tapeLowS = particleFrames * minK;
		predicted.tapeHighS = particleFrames * maxK;
	}

	public void NormaliseMissing()
	{
		if (predicted == null)
		{
			predicted = new Predicted();
		}
		if (actual == null)
		{
			actual = new Actual();
		}
		if (hookStages == null)
		{
			hookStages = new HookStages();
		}
		if (perf == null)
		{
			perf = new Perf();
		}
		if (view == null)
		{
			view = new View();
		}
		if (gpuPath == null)
		{
			gpuPath = "";
		}
		if (fieldsPath == null)
		{
			fieldsPath = "";
		}
		if (fieldsPathWhy == null)
		{
			fieldsPathWhy = "";
		}
		if (reconReadback == null)
		{
			reconReadback = "";
		}
	}

	public static string NormaliseGpuPath(string p)
	{
		if (string.IsNullOrEmpty(p))
		{
			p = "full";
		}
		return p;
	}

	public static string NormaliseFieldsPath(string p)
	{
		if (string.IsNullOrEmpty(p))
		{
			p = "cpu";
		}
		return p;
	}

	public bool CountsForBand()
	{
		return v == 1 && completed && ok && allocFailures == 0 && nativeLogFailures == 0 && string.IsNullOrEmpty(invalidReason) && resumedFromStep == 0;
	}

	public static string SidecarPathFor(string tapePath)
	{
		if (string.IsNullOrEmpty(tapePath))
		{
			throw new ArgumentException("SuperRunRecord: пустой путь ленты", "tapePath");
		}
		return SuperTapeFiles.FinalPathOf(tapePath) + ".run.json";
	}

	public void Save(string sidecarPath)
	{
		if (string.IsNullOrEmpty(sidecarPath))
		{
			throw new ArgumentException("SuperRunRecord: пустой путь спутника", "sidecarPath");
		}
		string directory = Path.GetDirectoryName(Path.GetFullPath(sidecarPath));
		if (!string.IsNullOrEmpty(directory))
		{
			Directory.CreateDirectory(directory);
		}
		string temporary = sidecarPath + ".part";
		File.WriteAllText(temporary, JsonUtility.ToJson(this, true), new UTF8Encoding(false));
		if (File.Exists(sidecarPath))
		{
			File.Replace(temporary, sidecarPath, null);
		}
		else
		{
			File.Move(temporary, sidecarPath);
		}
	}

	public static bool TryLoad(string tapePath, out SuperRunRecord record)
	{
		record = null;
		if (string.IsNullOrEmpty(tapePath))
		{
			return false;
		}
		string sidecarPath = SidecarPathFor(tapePath);
		if (!File.Exists(sidecarPath))
		{
			return false;
		}
		try
		{
			string json;
			using (FileStream stream = new FileStream(sidecarPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
			{
				using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
				{
					json = reader.ReadToEnd();
				}
			}
			SuperRunRecord loaded = JsonUtility.FromJson<SuperRunRecord>(json);
			if (loaded == null)
			{
				throw new InvalidDataException("пустой JSON");
			}
			if (loaded.v != 1)
			{
				Debug.LogWarning(string.Format("[Super] Спутник ленты {0}: версия записи {1}, читатель знает {2} — пропущен.", sidecarPath, loaded.v, 1));
				return false;
			}
			loaded.NormaliseMissing();
			record = loaded;
			return true;
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[Super] Спутник ленты " + sidecarPath + " не прочитан: " + ex.Message);
			return false;
		}
	}
}
