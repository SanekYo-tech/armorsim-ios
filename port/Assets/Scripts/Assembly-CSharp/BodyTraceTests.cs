using System;
using System.Collections.Generic;
using UnityEngine;

public class BodyTraceTests : MonoBehaviour
{
	private sealed class RecordingResolver : ISegmentResolver
	{
		public int InboundCount;

		public string TissueId;

		public SegmentOutcome Resolve(in TraceSegment segment, in ProjectileState incoming, IReadOnlyList<TransferEvent> inbound)
		{
			return default;
		}

		SegmentOutcome ISegmentResolver.Resolve(in TraceSegment segment, in ProjectileState incoming, IReadOnlyList<TransferEvent> inbound)
		{
			return default;
		}
	}

	private const float PotROuter = 100f;

	private const float PotWall = 1.5f;

	private const float PotHeight = 150f;

	private const float RimHeight = 4f;

	private const float RimRInner = 97f;

	private const float WallTopY = 146f;

	private const float RimMidY = 148f;

	private const float PotMidY = 75f;

	private static readonly CertifiedMaterialRef Steel;

	private static readonly EstimatedMaterialRef Alu;

	private static readonly double[] FarAims;

	private static readonly float[] Distances;

	private static readonly string[] ForbiddenWords;

	private const ulong PotWallFingerprint = 15319212462038621198uL;

	private const ulong PotRim97Fingerprint = 16578742008639732489uL;

	private static int _errors;

	private static bool _counting;

	[ContextMenu("★ Прогнать все проверки обхода BODY")]
	public void RunAll()
	{
	}

	public static bool CheckAll()
	{
		return false;
	}

	private static BodyShotSetup Shot()
	{
		return default;
	}

	private static BodyTraceResult TraceOne(IShotObject obj, Quaternion rot, Vector2 aim, float standoff = 0f)
	{
		return null;
	}

	private static ShellOfRevolutionShotObject Pot()
	{
		return null;
	}

	private static List<TraceSegment> Materials(BodyTraceResult r)
	{
		return null;
	}

	private static float TotalMaterial(BodyTraceResult r)
	{
		return 0f;
	}

	private static double RimChord(double d)
	{
		return 0.0;
	}

	private static bool PlateObliquity()
	{
		return false;
	}

	private static bool PotWallShot()
	{
		return false;
	}

	private static bool PotRimShot()
	{
		return false;
	}

	private static bool RimToWallRatio()
	{
		return false;
	}

	private static bool AimSensitivity()
	{
		return false;
	}

	private static bool LaminateStack()
	{
		return false;
	}

	private static bool ObjectKindRouting()
	{
		return false;
	}

	private static bool Determinism()
	{
		return false;
	}

	private static bool Rejections()
	{
		return false;
	}

	private static bool HasCode(string what, BodyRejectCode code, BodyPlacement placement)
	{
		return false;
	}

	private static bool HasCodeIn(BodyTraceResult r, BodyRejectCode code)
	{
		return false;
	}

	private static bool LoudDefaultFires()
	{
		return false;
	}

	private static bool Contrafactual()
	{
		return false;
	}

	private static bool GateTakesCertifiedConcretely()
	{
		return false;
	}

	private static bool PlaceholdersCannotCalibrate()
	{
		return false;
	}

	private static bool FiringDistanceIndependence()
	{
		return false;
	}

	private static bool FingerprintDistanceParity()
	{
		return false;
	}

	private static BodyTraceResult PotAt(float d, float back)
	{
		return null;
	}

	private static BodyTraceResult PotBehind(float d, float gap)
	{
		return null;
	}

	private static List<TraceSegment> PotOnly(BodyTraceResult r)
	{
		return null;
	}

	private static float PotBehindPlate(float d, float gap)
	{
		return 0f;
	}

	private static int PotBehindPlateCount(float d, float gap)
	{
		return 0;
	}

	private static ulong PotSegmentsFingerprint(float d, float gap)
	{
		return 0uL;
	}

	private static bool EdgeAccuracy()
	{
		return false;
	}

	private static float CylinderChord(float d, float back)
	{
		return 0f;
	}

	private static bool RayOriginConditioning()
	{
		return false;
	}

	private static bool EllipsoidChord()
	{
		return false;
	}

	private static BodyTraceResult TorsoShot(float x, float y)
	{
		return null;
	}

	private static BodyTraceResult TorsoShot(BodyShotObject torso, float x, float y)
	{
		return null;
	}

	private static List<string> Tissues(BodyTraceResult r)
	{
		return null;
	}

	private static bool LayerOrder()
	{
		return false;
	}

	private static bool RibGate()
	{
		return false;
	}

	private static float BonePath(BodyTraceResult r)
	{
		return 0f;
	}

	private static bool LungIsOneSegment()
	{
		return false;
	}

	private static bool ChannelConsistency()
	{
		return false;
	}

	private static bool NoOutcomeDeclared()
	{
		return false;
	}

	private static bool Forbidden(string name)
	{
		return false;
	}

	private static bool IsNumeric(Type t)
	{
		return false;
	}

	private static bool IsLengthName(string n)
	{
		return false;
	}

	private static bool InboundReachesFirstTissue()
	{
		return false;
	}

	private static bool TorsoDeterminism()
	{
		return false;
	}

	private static bool HeartContrafactual()
	{
		return false;
	}

	private static bool TissueLoudDefault()
	{
		return false;
	}

	private static bool TorsoGrazeAndMiss()
	{
		return false;
	}

	private static bool EllipticSectionChord()
	{
		return false;
	}

	private static bool FrontVersusSideDepth()
	{
		return false;
	}

	private static bool CircularUnchanged()
	{
		return false;
	}

	private static ulong PotWallShotFingerprint()
	{
		return 0uL;
	}

	private static bool DeclaredThicknessHolds()
	{
		return false;
	}

	private static BodyTraceResult TorsoAt(BodyShotObject torso, Quaternion rot)
	{
		return null;
	}

	private static bool EllipseLayerChord(in BodyLocalRay ray, double aO, double bO, double aI, double bI, out double thickness)
	{
		thickness = default;
		return false;
	}

	private static bool EllipseHits(in BodyLocalRay ray, double a, double b, out double t0, out double t1)
	{
		t0 = default;
		t1 = default;
		return false;
	}

	private static float FirstLength(ChannelReport ch, string tissueId)
	{
		return 0f;
	}

	private static void OnLog(string condition, string stackTrace, LogType type)
	{
	}

	private static void BeginCountingErrors()
	{
	}

	private static int ErrorsSoFar()
	{
		return 0;
	}

	private static int EndCountingErrors()
	{
		return 0;
	}
}
