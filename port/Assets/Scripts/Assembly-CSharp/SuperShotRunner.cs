// DONE: восстановлены MaterialNoteKey, JetSpacingCeiling, JetGridOk, RodSpacingFor,
// SpacingForCeiling, PerpPart, SignedTilt (обе), MapMaterial, MapCore, MaterialRefusalKey,
// CoreRefusalKey, ShownObjectName, PresetName, CoreName, EraPresetOf, NeraPresetOf,
// Hollow* (6), MeasureHollowBurst, TryJetFor, DecorBytesFor, NeraPartsRootFlipped, BakerMatrix,
// MeshRefFor (2), AppendClip, AppendEdgesFree, EmitEra, EmitNera, Refuse, свойства, .cctor,
// TryBegin, PlanFor, VramRefusal, SpacingForVram, ReadScene (все 4 перегрузки — включая
// главную 11-аргументную, 0x180769910, ~6900 строк ISIL), Cost, SceneUpAxis, Choose,
// PosedObject.Name/Shown/.ctor, ReadGeneralBodies (0x180760C60, ~6900 строк ISIL).
// ReadGeneralBodies: полное тело снято по ISIL+capstone (карта выравнивания инструкций
// на адреса; сверены test/jcc-полярности, statics Vector3, Layouts и литералы).
// Известная ловушка: isil.py неверно переводит test+jne как Compare/JumpIfNotEqual —
// булевы ветвления сверялись с сырым дизассемблером.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class SuperShotRunner
{
	public enum PoseRoute
	{
		Legacy = 0,
		General = 1
	}

	public struct SceneView
	{
		public PoseRoute route;

		public float cutAzimuthDeg;

		public int onLine;

		public int bystanders;

		public int bonded;

		public SuperDecor.Sources decor;
	}

	private struct SceneObject
	{
		public ArmorPlate plate;

		public float faceAlong;

		public float tilt;

		public float boxT;

		public float sideM;

		public float lateralM;
	}

	private sealed class PosedObject
	{
		public ArmorPlate plate;

		public int order;

		public readonly List<SuperPlateSpec> specs;

		public bool onLine;

		public float firstContactZ;

		public int firstOnLineSpec;

		public float obliquityDeg;

		public float lateralM;

		public EraPreset ep;

		public string epName;

		public SuperEra.SceneCassette cassette;

		public NeraPreset np;

		public string npName;

		public SuperNera.Pack pack;

		public float packTilt;

		public float sideM;

		public bool isMesh;

		public SandboxModel model;

		public int part;

		public string Name => isMesh ? (model.DisplayName + "/" + model.prepared.parts[part].name) : plate.name;

		public string Shown => isMesh ? (model.DisplayName + "/" + model.prepared.parts[part].name) : ShownObjectName(plate);

		public PosedObject()
		{
			specs = new List<SuperPlateSpec>();
			firstContactZ = float.NegativeInfinity;
			firstOnLineSpec = -1;
		}
	}

	public struct HollowBurst
	{
		public bool present;

		public float beforeFaceM;

		public float behindRearM;

		public float plateThickM;

		public string targetShown;

		public bool aheadBodies;
	}

	public struct SceneCost
	{
		public bool ok;

		public string reasonKey;

		public object[] reasonArgs;

		public string reasonText;

		public double particles;

		public int bodies;

		public float thinnestM;

		public bool isJet;

		public float jetDiameterM;

		public bool isHollow;

		public float hollowTriggerRowS;

		public float hollowCavity;

		public float hollowWallVMps;

		public float hollowSpacingCeilingM;

		public HollowBurst hollowBurst;

		public float windowS;

		public string passport;

		public SuperScenePlan plan;
	}

	public const string KeyNoPlates = "super.refuse.no_plates";

	public const string KeyNonPlate = "super.refuse.non_plate";

	public const string KeyRound = "super.refuse.round";

	public const string KeyMaterialNoRow = "super.refuse.material.no_row";

	public const string KeyMaterialNeedsModel = "super.refuse.material.needs_model";

	public const string KeyMaterialNeedsJ3 = "super.refuse.material.needs_j3";

	public const string KeyMaterialAnisotropic = "super.refuse.material.anisotropic";

	public const string KeyMaterialNotAccepted = "super.refuse.material.not_accepted";

	public const string UraniumRegistryName = "DU_075TI";

	public const string KeyUraniumRodNote = "super.note.uranium_rod";

	public const string KeyUraniumPlateNote = "super.note.uranium_plate";

	public const string UraniumRodNoteRu = "уран в Super грибится, как вольфрам: самозатачивания нет";

	public const string UraniumPlateNoteRu = "уран-броня в Super: строка DU-0.75Ti (Johnson & Cook 1983), повреждаемости нет — рвётся только отколом";

	public static bool NightUraniumRowOff;

	public const string KeyBusy = "super.refuse.busy";
	public const string KeyTooBig = "super.refuse.too_big";

	public const string KeyVram = "super.refuse.vram";

	public const string KeyVramNoFit = "super.refuse.vram_nofit";

	public const string KeyBuffer = "super.refuse.buffer";

	public const string KeyJetGrid = "super.refuse.jet_grid";

	public const string KeyJetTooFine = "super.refuse.jet_too_fine";

	public const string KeyMixedTilt = "super.refuse.mixed_tilt";

	public const string KeyOffAxis = "super.refuse.off_axis";

	public const string KeyNoLine = "super.refuse.no_line";

	public const string KeyBodiesOverlap = "super.refuse.bodies_overlap";

	public static PoseRoute? NightForceRoute;

	public const string KeyHollowPairing = "super.refuse.hollow_pairing";

	public const string KeyHollowWindow = "super.refuse.hollow_window";

	public const string KeyHollowWindowNoFit = "super.refuse.hollow_window_nofit";

	public const string KeyHollowSpacing = "super.refuse.hollow_spacing";

	public const string KeyHollowTarget = "super.refuse.hollow_target";

	public const string KeyHollowCeiling = "super.refuse.hollow_ceiling";

	public const float OffAxisTolerance = 0.1f;

	public const float LegacyLateralM = 0.001f;

	public const float MixedTiltToleranceDeg = 1f;

	private const float TiltEpsilon = 0.0001f;

	public const string KeyTandem = "super.refuse.tandem";

	public const string KeyReactiveLast = "super.refuse.reactive_last";

	public static bool SabotageSkipModeGuard = false;

	public const string EdgesFreeRu = "«{0}»: плита свободна — края не закреплены";

	public const string KeyEdgesFree = "super.s40.passport.edges_free";

	public static bool NightSabotageEdgeLiteral;

	public const string KeyRegionMissing = "super.s40.refuse.region_missing";

	public const string KeyRegionTwo = "super.s40.refuse.region_two";

	public const string KeyRegionOffLine = "super.s40.refuse.region_off_line";

	public const string KeyRegionEmpty = "super.s40.refuse.region_empty";

	public const string KeyRegionPartial = "super.s40.refuse.region_partial";

	public const string KeyFirstContact = "super.s40.refuse.first_contact";

	public const string KeyPlateInModel = "super.s40.refuse.plate_in_model";

	public const string KeyPending = "super.s40.pending";

	public const string KeyLegacyS40 = "super.s40.refuse.legacy_route";

	public const string KeyPassportRegion = "super.s40.passport.region";

	public const string KeyPassportClip = "super.s40.passport.clip";

	public const string KeyPassportClipOpen = "super.s40.passport.clip_open";

	public const string KeyPassportOutside = "super.s40.passport.outside";

	public const string KeyPassportModel = "super.s40.passport.model";

	public const string KeyPassportPart = "super.s40.passport.part";

	public const string KeyPassportPartClosed = "super.s40.passport.part_closed";

	public const string KeyPassportPartOpen = "super.s40.passport.part_open";

	public const string KeyPassportOverlap = "super.s40.passport.overlap";

	public const string KeyPassportMeshBonded = "super.s40.passport.mesh_bonded";

	public const string KeyPassportDecor = "super.s40.passport.decor";

	public const string KeyBodyMeshPart = "super.s40.body.mesh_part";

	public const string KeyPassportRegionAuto = "super.s40.passport.region_auto";

	public const string KeyBodyRod = "super.s40.body.rod";

	public const string KeyPassportRod = "super.s40.passport.rod";

	public static PoseRoute LastRoute { get; private set; }

	public static string LastPassport { get; private set; } = "";

	public static string LastPassportShown { get; private set; } = "";

	public static HollowBurst LastHollowBurst { get; private set; }

	public static string MaterialNoteKey(string registryName, bool asRod)
	{
		if (registryName == "DU_075TI")
		{
			return asRod ? "super.note.uranium_rod" : "super.note.uranium_plate";
		}
		return null;

	}

	public static bool TryJetFor(ProjectileDefinition d, out SuperJetSpec jet)
	{
		jet = default(SuperJetSpec);
		if (d == null || d.kind != ProjectileClass.ShapedCharge)
		{
			return false;
		}
		ShapedChargeSpec spec = d.GetShapedChargeSpec();
		float standoffM = MathF.Max(0.001f, d.fuzeStandoffMM * 0.001f);
		jet = SuperJet.FromCharge(in spec, standoffM, 0f);
		return true;
	}

	public static float JetSpacingCeiling(float jetDiameterM)
	{
		return jetDiameterM * 0.125f;

	}

	public static bool JetGridOk(float jetDiameterM, float spacingM)
	{
		return jetDiameterM * 0.125f >= spacingM;

	}

	public static float RodSpacingFor(in SuperRodSpec rod, float sceneSpacingM)
	{
		if (rod.spacingM > 0f)
		{
			return SuperBaker.ResolveSpacing(rod.spacingM, sceneSpacingM, "стержень");
		}
		return sceneSpacingM;

	}

	public static bool HollowCavityRefusal(in SuperRodSpec rod, float sceneSpacingM, out string key, out object[] args)
	{
		key = null;
		args = null;
		if (rod.cavityFraction > 0f)
		{
			float spacing = RodSpacingFor(in rod, sceneSpacingM);
			if (!SuperCavity.TryResolve(in rod, spacing, out SuperCavity cavity, out string failKey, out object[] failArgs, out string prose))
			{
				key = failKey;
				args = failArgs;
				return true;
			}
		}
		return false;
	}

	public static float SpacingCeilingForWall(in SuperRodSpec rod)
	{
		float hi = 0.008f;
		if (rod.cavityFraction <= 0f)
		{
			return hi;
		}
		SuperRodSpec probe = rod;
		bool Ok(float sp) => SuperCavity.TryResolve(in probe, sp, out _, out _, out _, out _);
		if (Ok(hi))
		{
			return hi;
		}
		float lo = 0.0002f;
		if (!Ok(lo))
		{
			return -1f;
		}
		for (int i = 0; i < 24; i++)
		{
			float mid = (hi + lo) * 0.5f;
			if (Ok(mid))
			{
				lo = mid;
			}
			else
			{
				hi = mid;
			}
		}
		if (!Ok(lo))
		{
			return -1f;
		}
		return lo;
	}

	public static int WallLayersAt(in SuperRodSpec rod, float spacingM)
	{
		if (rod.cavityFraction > 0f && SuperCavity.TryResolve(in rod, spacingM, out SuperCavity cavity, out _, out _, out _))
		{
			return cavity.wallLayers;
		}
		return 0;
	}

	public static void HollowBurstEstimate(float cavityCentreFromTipM, float axialGapM, float targetTiltDeg, float velocityMps, float triggerDistanceM, float triggerTimeS, float plateThickM, out float beforeFaceM, out float behindRearM)
	{
		float c = MathF.Max(0.05f, MathF.Abs(MathF.Cos(targetTiltDeg * 0.017453292f)));
		float gap = axialGapM - triggerDistanceM / c;
		float t = MathF.Max(0f, triggerTimeS);
		float g = MathF.Max(0f, gap);
		beforeFaceM = (axialGapM - g + cavityCentreFromTipM - t * velocityMps) * c;
		behindRearM = -beforeFaceM - MathF.Max(0f, plateThickM);
	}

	public static bool HollowWindowRefusal(float axialGapM, float targetTiltDeg, float velocityMps, float triggerDistanceM, float triggerTimeS, float rampS, float windowS, string aheadNote, out string key, out object[] args)
	{
		key = null;
		args = null;
		float c = MathF.Max(0.05f, MathF.Abs(MathF.Cos(targetTiltDeg * 0.017453292f)));
		float v = MathF.Max(1f, velocityMps);
		float gap = axialGapM - triggerDistanceM / c;
		float t0 = MathF.Max(0f, gap) / v;
		float total = MathF.Max(0f, triggerTimeS) + t0 + MathF.Max(0f, rampS);
		if (windowS >= total)
		{
			return false;
		}
		string note = aheadNote ?? "";
		if (0.01f >= total)
		{
			key = KeyHollowWindow;
			args = new object[6]
			{
				t0 * 1000000f,
				triggerTimeS * 1000000f,
				rampS * 1000000f,
				total * 1000000f,
				windowS * 1000000f,
				note
			};
			return true;
		}
		float slack = MathF.Max(0f, 0.01f - t0 - MathF.Max(0f, rampS));
		key = KeyHollowWindowNoFit;
		args = new object[7]
		{
			t0 * 1000000f,
			triggerTimeS * 1000000f,
			rampS * 1000000f,
			total * 1000000f,
			10000f,
			slack * 1000000f,
			note
		};
		return true;
	}

	public static bool HollowCeilingRefusal(float impactVMps, float wallVMps, out string key, out object[] args)
	{
		key = null;
		args = null;
		if (impactVMps > 0f && wallVMps > 0f)
		{
			float boosted = impactVMps * 10f;
			if (boosted < impactVMps + wallVMps)
			{
				key = KeyHollowCeiling;
				args = new object[5] { wallVMps, impactVMps, impactVMps + wallVMps, impactVMps * 10f, wallVMps / 9f };
				return true;
			}
		}
		return false;
	}

	public static bool HollowRowSilent(ProjectileDefinition d, out string key, out object[] args)
	{
		key = null;
		args = null;
		if (d == null || !d.IsHollowBody)
		{
			return false;
		}
		if (d.cavityFrac > 0f && d.cavityFrac < 1f && d.fragmentVelocityMps > 0f && !(d.fuzeDelayS < 0f))
		{
			return false;
		}
		key = KeyHollowPairing;
		args = new object[3] { (double)d.cavityFrac, d.fragmentVelocityMps, d.fuzeDelayS * 1000000f };
		return true;
	}

	public static bool HollowPairingBroken(ProjectileDefinition d, float cavityOnRod, bool shellDriveBuilt, out string key, out object[] args)
	{
		if (HollowRowSilent(d, out key, out args))
		{
			return true;
		}
		key = null;
		args = null;
		if (d == null)
		{
			return false;
		}
		bool paired = d.IsHollowBody ? (cavityOnRod > 0f && shellDriveBuilt) : (cavityOnRod <= 0f && !shellDriveBuilt);
		if (!paired)
		{
			key = KeyHollowPairing;
			args = new object[3] { (double)cavityOnRod, d.fragmentVelocityMps, SuperPreset.TriggerTimeFor(d.fuzeDelayS) * 1000000f };
			Debug.LogError(string.Concat(string.Format("[Super] полое тело: полость {0:F4} и привод стенки ", cavityOnRod), "(", shellDriveBuilt ? "построен" : "НЕТ", ") разошлись, класс снаряда ", string.Format("{0}. Это дефект маршрута, а не выбора игрока: пара обязана ", d.kind), "строиться и доезжать до решателя вместе."));
			return true;
		}
		return false;
	}

	public static bool TryBegin(Projectile proj)
	{
		if (AppFlow.ActiveMode != SimMode.Super && !SabotageSkipModeGuard)
		{
			return false;
		}
		if (proj == null)
		{
			return false;
		}
		if (SuperSolveJob.Running)
		{
			Refuse(KeyBusy);
			return true;
		}
		if (!ReadScene(proj, out SuperRodSpec rod, out SuperJetSpec jet, out bool isJet, out List<SuperPlateSpec> plates, out SuperEraDrive drive, out string reasonKey, out object[] reasonArgs, out SceneView view))
		{
			Refuse(reasonKey, reasonArgs);
			return true;
		}
		SuperScenePlan plan = PlanFor(isJet, rod, jet, plates, SuperPreset.SpacingM, out float windowS);
		if (VramRefusal(plan, isJet, rod, jet, plates, out reasonKey, out reasonArgs))
		{
			Refuse(reasonKey, reasonArgs);
			return true;
		}
		if (!isJet && HollowPairingBroken(proj.GetDefinition(), rod.cavityFraction, drive is SuperShellDrive, out reasonKey, out reasonArgs))
		{
			Refuse(reasonKey, reasonArgs);
			return true;
		}
		byte[] decor = DecorBytesFor(view.decor);
		if (isJet)
		{
			SuperSolveJob.Begin(jet, plates, drive, view.cutAzimuthDeg, decor);
		}
		else
		{
			SuperSolveJob.Begin(rod, plates, drive, view.cutAzimuthDeg, decor);
		}
		return true;
	}

	public static byte[] DecorBytesFor(SuperDecor.Sources src)
	{
		if (src == null || !src.Any)
		{
			return null;
		}
		Stopwatch clock = Stopwatch.StartNew();
		SuperDecor decor = SuperDecor.Build(src);
		if (decor.Empty)
		{
			return null;
		}
		byte[] bytes = decor.Encode();
		string skipped = "";
		if (decor.droppedTriangles > 0L)
		{
			skipped = string.Format(" (сверх потолка не записано {0})", decor.droppedTriangles);
		}
		Debug.Log(string.Format("[Super] Декор за областью: частей {0}, треугольников {1}", decor.pieces.Count, decor.TriangleCount) + skipped + string.Format(", {0:F0} КБ, резка {1:F0} мс.", (double)bytes.Length / 1024.0, clock.Elapsed.TotalMilliseconds));
		return bytes;
	}

	public static SuperScenePlan PlanFor(bool isJet, SuperRodSpec rod, SuperJetSpec jet, IList<SuperPlateSpec> plates, float spacingM, out float windowS)
	{
		float speed;
		if (isJet)
		{
			windowS = MathF.Min(SuperPreset.DurationS, SuperSolveJob.JetWindowS(in jet, plates));
			speed = jet.vTipMps;
		}
		else
		{
			windowS = SuperPreset.DurationS;
			speed = rod.velocityMps;
		}
		float margin = SuperScenePlan.GameMarginFor(plates, speed, windowS);
		if (isJet)
		{
			return SuperScenePlan.For(in jet, plates, spacingM, windowS, margin, gameDomain: true);
		}
		return SuperScenePlan.For(in rod, plates, spacingM, windowS, margin, gameDomain: true);
	}

	public static bool VramRefusal(SuperScenePlan plan, bool isJet, SuperRodSpec rod, SuperJetSpec jet, IList<SuperPlateSpec> plates, out string key, out object[] args)
	{
		key = null;
		args = null;
		if (plan == null || !plan.vramKnown)
		{
			return false;
		}
		if (!plan.fitsVram)
		{
			float fit = SpacingForVram(isJet, rod, jet, plates);
			if (!isJet)
			{
				float wall = SpacingCeilingForWall(in rod);
				if (rod.cavityFraction > 0f && fit > 0f && (wall < 0f || wall < fit))
				{
					key = KeyHollowSpacing;
					args = new object[4] { fit * 1000f, ((wall > 0f) ? wall * 1000f : 0.2f), WallLayersAt(in rod, fit), 4 };
					return true;
				}
			}
			if (fit > 0f)
			{
				key = KeyVram;
				args = new object[4]
				{
					(double)plan.gpuBytesTotal / 1073741824.0,
					(double)plan.vramCeilingBytes / 1073741824.0,
					plan.vramMB / 1024.0,
					fit * 1000f
				};
			}
			else
			{
				key = KeyVramNoFit;
				args = new object[4]
				{
					(double)plan.gpuBytesTotal / 1073741824.0,
					(double)plan.vramCeilingBytes / 1073741824.0,
					plan.vramMB / 1024.0,
					8f
				};
			}
			return true;
		}
		if (!plan.fitsResourceCap)
		{
			key = KeyBuffer;
			args = new object[3] { plan.largestBuffer, (double)plan.largestBufferBytes / 1048576.0, (double)plan.resourceCapBytes / 1048576.0 };
			return true;
		}
		return false;
	}

	public static float SpacingForVram(bool isJet, SuperRodSpec rod, SuperJetSpec jet, IList<SuperPlateSpec> plates)
	{
		float hi = 0.008f;
		float lo = SuperPreset.SpacingM;
		bool Fits(float sp) => PlanFor(isJet, rod, jet, plates, sp, out _).fitsVram;
		if (!Fits(hi))
		{
			return -1f;
		}
		if (Fits(lo))
		{
			return lo;
		}
		for (int i = 0; i < 20; i++)
		{
			float mid = (hi + lo) * 0.5f;
			if (Fits(mid))
			{
				hi = mid;
			}
			else
			{
				lo = mid;
			}
		}
		return hi;
	}

	public static bool ReadScene(Projectile proj, out SuperRodSpec rod, out List<SuperPlateSpec> plates, out string reasonKey)
	{
		if (!ReadScene(proj, out rod, out SuperJetSpec jet, out bool isJet, out plates, out SuperEraDrive drive, out reasonKey, out object[] reasonArgs, out SceneView view, reactive: false, quiet: false))
		{
			return false;
		}
		if (isJet)
		{
			reasonKey = KeyRound;
			return false;
		}
		return true;
	}

	public static bool ReadScene(Projectile proj, out SuperRodSpec rod, out SuperJetSpec jet, out bool isJet, out List<SuperPlateSpec> plates, out string reasonKey)
	{
		return ReadScene(proj, out rod, out jet, out isJet, out plates, out SuperEraDrive drive, out reasonKey, out object[] reasonArgs, out SceneView view, reactive: false, quiet: false);
	}

	private static Vector3 PerpPart(Vector3 plateNormal, Vector3 fwd)
	{
		Vector3 n = plateNormal;
		float m = n.magnitude;
		if (m > 1e-05f)
		{
			n /= m;
		}
		else
		{
			n = Vector3.forward;
		}
		if (Vector3.Dot(n, fwd) < 0f)
		{
			n = -n;
		}
		return n - fwd * Vector3.Dot(n, fwd);

	}

	public static Vector3 SceneUpAxis(IList<ArmorPlate> plates, Vector3 fwd, out string mixedName, out float mixedDeg)
	{
		mixedName = null;
		mixedDeg = 0f;
		Vector3 refUp = Vector3.up - fwd * Vector3.Dot(fwd, Vector3.up);
		if (refUp.sqrMagnitude > 1e-08f)
		{
			float m = refUp.magnitude;
			if (m > 1e-05f)
			{
				refUp /= m;
			}
			else
			{
				refUp = Vector3.zero;
			}
		}
		else
		{
			refUp = Vector3.zero;
		}
		Vector3 best = Vector3.zero;
		float bestMag = 0f;
		foreach (ArmorPlate p in plates)
		{
			Vector3 n = p.transform.forward;
			float m = n.magnitude;
			if (m > 1e-05f)
			{
				n /= m;
			}
			else
			{
				n = Vector3.zero;
			}
			if (Vector3.Dot(n, fwd) < 0f)
			{
				n = -n;
			}
			Vector3 proj = n - fwd * Vector3.Dot(n, fwd);
			float mag = (float)Math.Sqrt(proj.sqrMagnitude);
			if (mag > bestMag)
			{
				bestMag = mag;
				best = proj;
			}
		}
		Vector3 up;
		if (bestMag < 1e-04f)
		{
			if (refUp.sqrMagnitude > 0f)
			{
				up = refUp;
			}
			else
			{
				Vector3 side = Vector3.Cross(fwd, Vector3.right);
				float sm = side.magnitude;
				if (sm > 1e-05f)
				{
					up = side / sm;
				}
				else
				{
					up = Vector3.zero;
				}
			}
		}
		else
		{
			up = best / bestMag;
			float dot = Vector3.Dot(up, refUp);
			if (MathF.Abs(dot) <= 1e-06f)
			{
				up = -up;
			}
			else if (dot < 0f)
			{
				up = -up;
			}
			Vector3 cross = Vector3.Cross(up, refUp);
			if (cross.sqrMagnitude < 1e-10f)
			{
				up = refUp;
			}
		}
		float sinTol = MathF.Sin(0.017453292f);
		foreach (ArmorPlate p in plates)
		{
			Vector3 n = p.transform.forward;
			float m = n.magnitude;
			if (m > 1e-05f)
			{
				n /= m;
			}
			else
			{
				n = Vector3.zero;
			}
			if (Vector3.Dot(n, fwd) < 0f)
			{
				n = -n;
			}
			Vector3 proj = n - fwd * Vector3.Dot(n, fwd);
			float mag = (float)Math.Sqrt(proj.sqrMagnitude);
			if (mag < 1e-04f)
			{
				continue;
			}
			n = proj / mag;
			Vector3 c = Vector3.Cross(n, up);
			float cm = c.magnitude;
			if (cm <= sinTol)
			{
				continue;
			}
			if (mixedName != null)
			{
				continue;
			}
			mixedName = p.name;
			mixedDeg = MathF.Asin(Mathf.Clamp(cm, 0f, 1f)) * 57.29578f;
		}
		return up;
	}

	public static PoseRoute Choose(IList<ArmorPlate> sceneObjects, Vector3 origin, Vector3 dir, float spacingM)
	{
		Vector3 d;
		if (dir.sqrMagnitude <= 1e-09f)
		{
			d = Vector3.forward;
		}
		else
		{
			float m = dir.magnitude;
			if (m > 1e-05f)
			{
				d = dir / m;
			}
			else
			{
				d = Vector3.zero;
			}
		}
		Vector3 negD = -d;
		Vector3 perp = Vector3.up - negD * Vector3.Dot(Vector3.up, negD);
		Vector3 up;
		if (perp.sqrMagnitude > 1e-08f)
		{
			float m = perp.magnitude;
			if (m > 1e-05f)
			{
				up = perp / m;
			}
			else
			{
				up = Vector3.zero;
			}
		}
		else
		{
			up = Vector3.zero;
		}
		foreach (ArmorPlate p in sceneObjects)
		{
			if (p == null)
			{
				continue;
			}
			Vector3 w = PerpPart(p.transform.forward, negD);
			float mag = w.magnitude;
			if (mag >= 1e-04f)
			{
				if (up.sqrMagnitude == 0f)
				{
					return PoseRoute.General;
				}
				Vector3 wn = w / mag;
				Vector3 cross = Vector3.Cross(up, wn);
				if (cross.sqrMagnitude >= 1e-10f)
				{
					return PoseRoute.General;
				}
			}
			Vector3 rel = p.transform.position - origin;
			float t = Vector3.Dot(d, rel);
			float perpSqr = MathF.Max(0f, rel.sqrMagnitude - t * t);
			float dist = MathF.Sqrt(perpSqr);
			if (dist > LegacyLateralM)
			{
				return PoseRoute.General;
			}
			if (p.kind == ArmorPlate.ObjectKind.Plate && MathF.Abs(p.widthM - p.heightM) > spacingM * 0.5f)
			{
				return PoseRoute.General;
			}
			if (p.kind == ArmorPlate.ObjectKind.Nera && p.neraMountAngleDeg != 0f && !NeraPartsRootFlipped())
			{
				return PoseRoute.General;
			}
		}
		return PoseRoute.Legacy;
	}

	private static bool NeraPartsRootFlipped()
	{
		return Vector3.Dot(SectionFrame.PlateBase() * Vector3.forward, SectionFrame.StackAxis()) > 0f;
	}

	// Восстановлено по ISIL/дизассемблеру 0x180760C60 (~6900 строк ISIL): выравнивание
	// инструкций на адреса — tools_decomp/isil.py + capstone; сверены полярности test/jcc
	// (ISIL неверно переводит test+jne), Layouts SuperClip/SuperPlateSpec/PosedObject,
	// statics Vector3 (forward@0x48, right@0x3C), Phase-ветки (2900-3400) и все литералы.
	private static bool ReadGeneralBodies(List<ArmorPlate> inFront, Vector3 origin, Vector3 dir, float rodRadiusM, float threatSpeed, bool reactive, bool quiet, List<SuperPlateSpec> plates, List<string> labels, List<string> labelsShown, List<int> objOf, List<ArmorPlate.ObjectKind> kindOf, StringBuilder passport, StringBuilder shown, ref SuperEraDrive chain, ref int hollowTargetSpec, ref ArmorPlate hollowTargetPlate, ref int hollowAheadCount, ref SceneView view, int projBodies, int triggerBodyIndex, out string reasonKey, out object[] reasonArgs, List<SandboxModel> models = null, RegionBox region = null)
	{
		reasonKey = null;
		reasonArgs = new object[0];
		SuperScenePose.BakerFrame frame = SuperScenePose.FrameFor(origin, dir);
		SuperClip clip = ((region != null) ? region.ClipIn(frame) : default(SuperClip));
		SuperDecor.Sources sources = null;
		if (clip.present && region != null && !region.auto)
		{
			sources = new SuperDecor.Sources();
			sources.clip = clip;
			sources.pad = SuperClip.Band * SuperPreset.SpacingM;
		}
		List<PosedObject> posed = new List<PosedObject>();
		List<(string, string, float)> tuples = new List<(string, string, float)>();
		List<(SandboxModel, SuperMeshRef, SuperMeshVoxels)> meshTuples = new List<(SandboxModel, SuperMeshRef, SuperMeshVoxels)>();
		for (int order = 0; order < inFront.Count; order++)
		{
			ArmorPlate p = inFront[order];
			if (p.kind != ArmorPlate.ObjectKind.Plate && !p.IsRod && !(reactive && (p.kind == ArmorPlate.ObjectKind.Era || p.kind == ArmorPlate.ObjectKind.Nera)))
			{
				reasonKey = KeyNonPlate;
				return false;
			}
			Vector3 point = frame.Point(p.transform.position);
			Vector3 rightB = frame.Dir(p.transform.right);
			Vector3 upB = frame.Dir(p.transform.up);
			Vector3 fwdB = frame.Dir(p.transform.forward);
			if (fwdB.z < 0f)
			{
				fwdB = -fwdB;
				rightB = -rightB;
			}
			float thickM = MathF.Max(p.thicknessMM * 0.001f, 0.0001f);
			if (sources != null)
			{
				sources.boxes.Add(new SuperDecor.BoxSource
				{
					name = p.name,
					centre = point,
					u = rightB,
					v = upB,
					n = fwdB,
					widthM = MathF.Max(p.widthM, 0.02f),
					heightM = MathF.Max(p.heightM, 0.02f),
					thicknessM = thickM
				});
			}
			PosedObject o = new PosedObject();
			o.plate = p;
			o.order = order;
			o.obliquityDeg = SuperScenePose.ObliquityDeg(fwdB);
			o.lateralM = new Vector2(point.x, point.y).magnitude;
			Vector3 anchor = point + new Vector3(0f, 0f, thickM * 0.5f / MathF.Max(0.001f, fwdB.z));
			float tiltAzimuth = SuperScenePose.TiltAzimuthRad(fwdB);
			if (p.kind == ArmorPlate.ObjectKind.Nera)
			{
				NeraPreset np = NeraPresetOf(p);
				string npName = PresetName(np.id, np.displayName);
				float side = MathF.Max(MathF.Max(p.widthM, p.heightM), 0.02f);
				Quaternion q = p.transform.rotation * (NeraPartsRootFlipped() ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity);
				q *= Quaternion.Euler(p.neraMountAngleDeg, 0f, 0f);
				Vector3 fwdB2 = frame.Dir(q * Vector3.forward);
				Vector3 sideB2 = frame.Dir(q * Vector3.right);
				if (fwdB2.z < 0.05f)
				{
					float tiltDeg = MathF.Asin(Mathf.Clamp(fwdB2.z, -1f, 1f)) * 57.29578f;
					reasonKey = "super.nera.refuse.tilt";
					reasonArgs = new object[1] { tiltDeg };
					if (!quiet)
					{
						Debug.LogWarning(string.Format("[Super] Пакет «{0}»: наклон пакета {1:F0}° (объект + угол ", np.displayName, tiltDeg) + string.Format("установки {0:F0}°) — пакет почти вдоль выстрела или обращён к ", p.neraMountAngleDeg) + "угрозе тыльной пластиной.");
					}
					return false;
				}
				float packTilt = SuperScenePose.ObliquityDeg(fwdB2);
				float phi = SuperScenePose.TiltAzimuthRad(fwdB2);
				SuperNera.Pack pk = SuperNera.Build(np, side, packTilt, 0f, SuperPreset.SpacingM, p.neraFrontMaterial, p.neraRearMaterial, np.bonding, p.neraInterlayer);
				if (pk.refuseKey != null)
				{
					reasonKey = pk.refuseKey;
					reasonArgs = pk.refuseArgs;
					if (!quiet)
					{
						Debug.LogWarning("[Super] Пакет «" + np.displayName + "»: " + pk.refuseDetail);
					}
					return false;
				}
				foreach (SuperPlateSpec layerL in pk.plates)
				{
					SuperPlateSpec layer = SuperScenePose.PlaceLegacyLayer(in layerL, phi, anchor);
					SuperScenePose.AlignInPlane(ref layer, sideB2);
					o.specs.Add(layer);
				}
				o.np = np;
				o.npName = npName;
				o.pack = pk;
				o.packTilt = packTilt;
				o.sideM = side;
			}
			else if (p.kind == ArmorPlate.ObjectKind.Rod)
			{
				string mat = MapMaterial(p.armorType);
				if (mat == null)
				{
					reasonKey = MaterialRefusalKey(p.armorType);
					reasonArgs = new object[1] { Catalog.MaterialName(p.armorType) };
					return false;
				}
				float rodThickM = MathF.Max(p.thicknessMM * 0.001f, 0.002f);
				float sideW = MathF.Max(p.widthM, 0.02f);
				SuperPlateSpec spec = SuperScenePose.FromBox(point, rightB, upB, fwdB, sideW, rodThickM, rodThickM);
				spec.material = mat;
				spec.roundU = true;
				spec.clampedEdges = p.edgeClamp || NightSabotageEdgeLiteral;
				o.specs.Add(spec);
			}
			else if (p.kind == ArmorPlate.ObjectKind.Plate)
			{
				string mat2 = MapMaterial(p.armorType);
				if (mat2 == null)
				{
					reasonKey = MaterialRefusalKey(p.armorType);
					reasonArgs = new object[1] { Catalog.MaterialName(p.armorType) };
					return false;
				}
				SuperPlateSpec spec2 = SuperScenePose.FromBox(point, rightB, upB, fwdB, MathF.Max(p.widthM, 0.02f), MathF.Max(p.heightM, 0.02f), thickM);
				spec2.material = mat2;
				spec2.clampedEdges = p.edgeClamp || NightSabotageEdgeLiteral;
				o.specs.Add(spec2);
			}
			else if (p.kind == ArmorPlate.ObjectKind.Era)
			{
				EraPreset ep = EraPresetOf(p);
				string epName = PresetName(ep.id, ep.displayName);
				float side2 = MathF.Max(MathF.Min(p.widthM, p.heightM), 0.02f);
				SuperEra.SceneCassette cassette = SuperEra.BuildInScene(ep, side2, o.obliquityDeg, 0f, SuperPreset.SpacingM, threatSpeed);
				if (cassette.refuseKey != null)
				{
					reasonKey = cassette.refuseKey;
					reasonArgs = cassette.refuseArgs;
					if (!quiet)
					{
						Debug.LogWarning("[Super] Кассета «" + ep.displayName + "»: " + cassette.refuseDetail);
					}
					return false;
				}
				SuperPlateSpec front = SuperScenePose.PlaceLegacyLayer(in cassette.front, tiltAzimuth, anchor);
				SuperScenePose.AlignInPlane(ref front, rightB);
				SuperPlateSpec rear = SuperScenePose.PlaceLegacyLayer(in cassette.rear, tiltAzimuth, anchor);
				SuperScenePose.AlignInPlane(ref rear, rightB);
				o.specs.Add(front);
				o.specs.Add(rear);
				o.ep = ep;
				o.epName = epName;
				o.cassette = cassette;
				o.sideM = side2;
			}
			if (clip.present)
			{
				float bestZ = float.NegativeInfinity;
				int clipped = 0;
				for (int i = 0; i < o.specs.Count; i++)
				{
					SuperPlateSpec sp = o.specs[i];
					sp.clip = clip;
					o.specs[i] = sp;
					float spacing = SuperBaker.ResolveSpacing(sp.spacingM, SuperPreset.SpacingM, "«" + p.name + "»");
					if (SuperClip.CountPosedPlate(in sp, spacing).Kept == 0)
					{
						clipped++;
						if (SuperScenePose.OnLine(in sp, rodRadiusM, out float entryZ) && entryZ > bestZ)
						{
							bestZ = entryZ;
						}
					}
				}
				if (clipped == o.specs.Count)
				{
					tuples.Add((p.name, ShownObjectName(p), bestZ));
					continue;
				}
				if (clipped > 0)
				{
					reasonKey = KeyRegionPartial;
					reasonArgs = new object[1] { ShownObjectName(p) };
					return false;
				}
			}
			for (int j = 0; j < o.specs.Count; j++)
			{
				SuperPlateSpec sp2 = o.specs[j];
				if (SuperScenePose.OnLine(in sp2, rodRadiusM, out float entryZ2))
				{
					o.onLine = true;
					if (entryZ2 > o.firstContactZ)
					{
						o.firstContactZ = entryZ2;
						o.firstOnLineSpec = j;
					}
				}
			}
			posed.Add(o);
		}
		if (models != null && models.Count > 0 && clip.present)
		{
			foreach (SandboxModel sm in models)
			{
				if (sm.prepared == null && sm.source == null)
				{
					continue;
				}
				SuperMeshRef meshRef = MeshRefFor(sm, region, frame);
				SuperMeshCache.Status st = SuperMeshCache.Request(meshRef);
				if (st.pending)
				{
					reasonKey = KeyPending;
					reasonArgs = new object[1] { (double)st.progress };
					return false;
				}
				if (st.refusal != null)
				{
					reasonKey = st.refusal.Key;
					reasonArgs = st.refusal.Args;
					return false;
				}
				meshTuples.Add((sm, meshRef, st.voxels));
				for (int part = 0; part < st.voxels.parts.Length; part++)
				{
					SuperMeshVoxels.Part partObj = st.voxels.parts[part];
					if (partObj.Count == 0)
					{
						continue;
					}
					ArmorPlate.ArmorType mat3 = sm.MaterialOf(part);
					string matName = MapMaterial(mat3);
					if (matName == null)
					{
						reasonKey = MaterialRefusalKey(mat3);
						reasonArgs = new object[1] { Catalog.MaterialName(mat3) };
						return false;
					}
					float iC = (float)(partObj.bi1 + 1 + partObj.bi0) * 0.5f * meshRef.spacing;
					float jC = (float)(partObj.bj1 + 1 + partObj.bj0) * 0.5f * meshRef.spacing;
					float kC = (float)(partObj.bk1 + 1 + partObj.bk0) * 0.5f * meshRef.spacing;
					Vector3 centreB = clip.centre + clip.u * iC + clip.v * jC + clip.n * kC;
					SuperPlateSpec spec3 = SuperScenePose.FromBox(centreB, clip.u, clip.v, clip.n, (float)(partObj.bi1 - partObj.bi0 + 1) * meshRef.spacing, (float)(partObj.bj1 - partObj.bj0 + 1) * meshRef.spacing, (float)(partObj.bk1 - partObj.bk0 + 1) * meshRef.spacing);
					spec3.material = matName;
					spec3.clampedEdges = false;
					spec3.clip = clip;
					spec3.mesh = meshRef.CloneForPart(part, partObj.name);
					PosedObject mo = new PosedObject();
					mo.isMesh = true;
					mo.model = sm;
					mo.part = part;
					mo.order = inFront.Count + posed.Count;
					mo.obliquityDeg = SuperScenePose.ObliquityDeg(clip.n);
					mo.lateralM = new Vector2(centreB.x, centreB.y).magnitude;
					mo.specs.Add(spec3);
					if (SuperScenePose.MeshOnLine(st.voxels, part, clip, meshRef.spacing, rodRadiusM, out float meshEntryZ))
					{
						mo.onLine = true;
						mo.firstContactZ = meshEntryZ;
						mo.firstOnLineSpec = 0;
					}
					posed.Add(mo);
				}
			}
		}
		if (sources != null && models != null)
		{
			foreach (SandboxModel sm2 in models)
			{
				if (sm2 == null || sm2.prepared == null)
				{
					continue;
				}
				Matrix4x4 toBaker = BakerMatrix(frame) * sm2.ModelToWorld;
				for (int pi = 0; pi < sm2.prepared.parts.Count; pi++)
				{
					sources.meshes.Add(new SuperDecor.MeshSource
					{
						name = sm2.DisplayName + "/" + sm2.prepared.parts[pi].name,
						part = sm2.prepared.parts[pi],
						toFrame = toBaker
					});
				}
			}
		}
		float zNear = 0f;
		if (clip.present)
		{
			SuperPlateSpec target = SuperScenePose.FromBox(clip.centre, clip.u, clip.v, clip.n, clip.hu * 2f, clip.hv * 2f, clip.hn * 2f);
			if (!SuperScenePose.LineThroughBox(in target, 0f, 0f, out zNear, out float _))
			{
				reasonKey = KeyRegionOffLine;
				return false;
			}
			float halfSpacing = SuperPreset.SpacingM * 0.5f;
			float bestZ2 = float.NegativeInfinity;
			string bestShown = null;
			foreach (PosedObject q in posed)
			{
				if (q.isMesh || !q.onLine)
				{
					continue;
				}
				if (q.firstContactZ > halfSpacing + zNear && q.firstContactZ > bestZ2)
				{
					bestZ2 = q.firstContactZ;
					bestShown = q.Shown;
				}
			}
			foreach ((string tName, string tShown, float tZ) in tuples)
			{
				if (tZ != float.NegativeInfinity && tZ > halfSpacing + zNear && tZ > bestZ2)
				{
					bestZ2 = tZ;
					bestShown = tShown;
				}
			}
			if (models != null)
			{
				foreach (SandboxModel sm3 in models)
				{
					if (sm3.prepared == null)
					{
						continue;
					}
					Matrix4x4 toBaker2 = BakerMatrix(frame) * sm3.ModelToWorld;
					for (int pj = 0; pj < sm3.prepared.parts.Count; pj++)
					{
						float z = SuperScenePose.MeshSurfaceEntry(sm3.prepared.parts[pj], toBaker2, rodRadiusM, sm3.source.contentSha + "#" + pj);
						if (z != float.NegativeInfinity && z > halfSpacing + zNear && z > bestZ2)
						{
							bestZ2 = z;
							bestShown = sm3.DisplayName + "/" + sm3.prepared.parts[pj].name;
						}
					}
				}
			}
			if (bestShown != null)
			{
				reasonKey = KeyFirstContact;
				reasonArgs = new object[2] { bestShown, (bestZ2 - zNear) * 1000f };
				return false;
			}
		}
		if (posed.Count == 0)
		{
			reasonKey = (clip.present ? KeyRegionEmpty : KeyNoPlates);
			return false;
		}
		List<PosedObject> onLine = posed.FindAll((PosedObject q) => q.onLine);
		List<PosedObject> bystanders = posed.FindAll((PosedObject q) => !q.onLine);
		if (onLine.Count == 0)
		{
			reasonKey = KeyNoLine;
			if (!quiet)
			{
				Debug.LogWarning(string.Format("[Super] Линия выстрела (цилиндр ⌀{0:F1} мм) не задевает ", rodRadiusM * 2f * 1000f) + string.Format("ни одного из {0} тел — считать нечего.", posed.Count));
			}
			return false;
		}
		onLine.Sort((PosedObject a, PosedObject b) =>
		{
			int z = b.firstContactZ.CompareTo(a.firstContactZ);
			return (z != 0) ? z : a.order.CompareTo(b.order);
		});
		PosedObject deepest = onLine[onLine.Count - 1];
		if (!deepest.isMesh && deepest.plate.kind != ArmorPlate.ObjectKind.Plate && !deepest.plate.IsRod)
		{
			reasonKey = KeyReactiveLast;
			return false;
		}
		List<PosedObject> sorted = new List<PosedObject>(posed.Count);
		for (int k = 0; k + 1 < onLine.Count; k++)
		{
			sorted.Add(onLine[k]);
		}
		sorted.AddRange(bystanders);
		sorted.Add(deepest);
		posed = sorted;
		PosedObject first = onLine[0];
		float z0;
		if (first.isMesh)
		{
			SuperPlateSpec firstSpec = first.specs[0];
			SuperMeshVoxels vx = SuperMeshCache.GetOrCompute(firstSpec.mesh);
			z0 = (SuperScenePose.MeshOnLine(vx, firstSpec.mesh.part, clip, firstSpec.mesh.spacing, 0f, out float meshZ) ? meshZ : first.firstContactZ);
		}
		else
		{
			SuperPlateSpec firstSpec2 = first.specs[first.firstOnLineSpec];
			z0 = (SuperScenePose.LineThroughBox(in firstSpec2, 0f, 0f, out float zNear0, out float _) ? zNear0 : first.firstContactZ);
		}
		float dz = -z0;
		view.onLine = onLine.Count;
		view.bystanders = bystanders.Count;
		view.cutAzimuthDeg = SuperScenePose.CutAzimuthDeg(onLine[0].specs[onLine[0].firstOnLineSpec].poseN);
		if (sources != null)
		{
			sources.dz = dz;
			view.decor = sources;
		}
		if (clip.present)
		{
			if (region.auto)
			{
				passport.Append(string.Format("области счёта нет — считается вся сцена целиком (коробка {0:F0}×{1:F0}×", clip.hu * 2f * 1000f, clip.hv * 2f * 1000f) + string.Format("{0:F0} мм поставлена автоматически, ничего не срезано); ", clip.hn * 2f * 1000f));
				shown.Append(Localization.T(KeyPassportRegionAuto, clip.hu * 2f * 1000f, clip.hv * 2f * 1000f, clip.hn * 2f * 1000f)).Append("; ");
			}
			else
			{
				passport.Append(string.Format("область счёта {0:F0}×{1:F0}×{2:F0} мм: рама ", clip.hu * 2f * 1000f, clip.hv * 2f * 1000f, clip.hn * 2f * 1000f) + string.Format("{0:F1} мм ({1} слоя частиц) закреплена — «вырезано из жёсткой рамы», за рамой ", SuperClip.Band * SuperPreset.SpacingM * 1000f, SuperClip.Band) + "вещество не создаётся; ");
				shown.Append(Localization.T(KeyPassportRegion, clip.hu * 2f * 1000f, clip.hv * 2f * 1000f, clip.hn * 2f * 1000f, SuperClip.Band * SuperPreset.SpacingM * 1000f)).Append("; ");
				passport.Append("за рамой — декор: коробки объектов и части модели нарисованы как стоят, частиц и счёта у них нет, но вещество сквозь них не проходит — липкая стенка, урона декору нет; ");
				shown.Append(Localization.T(KeyPassportDecor, clip.hu * 2f * 1000f, clip.hv * 2f * 1000f, clip.hn * 2f * 1000f, SuperClip.Band * SuperPreset.SpacingM * 1000f)).Append("; ");
				foreach ((string tName2, string tShown2, float tZ2) in tuples)
				{
					passport.Append("«" + tName2 + "» целиком вне области счёта — не считается; ");
					shown.Append(Localization.T(KeyPassportOutside, tShown2)).Append("; ");
				}
			}
		}
		foreach ((SandboxModel sm4, SuperMeshRef meshRef2, SuperMeshVoxels vx2) in meshTuples)
		{
			passport.Append(string.Format("модель «{0}»: частей {1}, отпечаток {2}; ", sm4.DisplayName, vx2.parts.Length, SuperMeshStore.Short(sm4.source.contentSha)));
			shown.Append(Localization.T(KeyPassportModel, sm4.DisplayName, vx2.parts.Length, SuperMeshStore.Short(sm4.source.contentSha))).Append("; ");
			int liveParts = 0;
			for (int pk2 = 0; pk2 < vx2.parts.Length; pk2++)
			{
				SuperMeshVoxels.Part vp = vx2.parts[pk2];
				string partName = sm4.DisplayName + "/" + sm4.prepared.parts[pk2].name;
				if (vp.Count == 0)
				{
					passport.Append("часть «" + partName + "» вне области счёта — не считается; ");
					shown.Append(Localization.T(KeyPassportOutside, partName)).Append("; ");
					continue;
				}
				liveParts++;
				ArmorPlate.ArmorType mat4 = sm4.MaterialOf(pk2);
				ModelPart pp = sm4.prepared.parts[pk2];
				string closedRu = pp.closed ? "замкнута" : string.Format("не замкнута ({0} кромок без пары) — объём и срез приблизительны", pp.boundaryEdges);
				string closedT = pp.closed ? Localization.T(KeyPassportPartClosed) : Localization.T(KeyPassportPartClosed, pp.boundaryEdges);
				passport.Append(string.Format("часть «{0}» — {1}: {2}, неоднозначных {3:P2}; ", partName, MapMaterial(mat4), closedRu, vp.AmbiguousFraction));
				shown.Append(Localization.T(KeyPassportPart, partName, Catalog.MaterialName(mat4), closedT, vp.AmbiguousFraction)).Append("; ");
				if (pp.closed && Math.Abs(pp.volumeM3) > 0.0)
				{
					double share = Math.Max(0.0, 1.0 - (double)vp.Count * meshRef2.spacing * meshRef2.spacing * meshRef2.spacing / (Math.Abs(pp.volumeM3) * sm4.Scale * sm4.Scale * sm4.Scale));
					passport.Append(string.Format("«{0}»: в области счёта {1} частиц, в раме {2}, за областью {3:P1} объёма — не считается; ", partName, vp.live, vp.band, share));
					shown.Append(Localization.T(KeyPassportClip, partName, vp.live, vp.band, share)).Append("; ");
				}
				else
				{
					passport.Append(string.Format("«{0}»: в области счёта {1} частиц, в раме {2}; доля вне области не определена — часть не замкнута; ", partName, vp.live, vp.band));
					shown.Append(Localization.T(KeyPassportClipOpen, partName, vp.live, vp.band)).Append("; ");
				}
				for (int pj2 = 0; pj2 < vp.lostTo.Length; pj2++)
				{
					if (vp.lostTo[pj2] <= 0)
					{
						continue;
					}
					string otherName = sm4.DisplayName + "/" + sm4.prepared.parts[pj2].name;
					passport.Append(string.Format("части «{0}» и «{1}» пересекаются: {2} ячеек отдано «{3}» (первой в списке); ", otherName, partName, vp.lostTo[pj2], otherName));
					shown.Append(Localization.T(KeyPassportOverlap, otherName, partName, vp.lostTo[pj2])).Append("; ");
				}
			}
			if (liveParts >= 2)
			{
				passport.Append("части модели, касающиеся в общей сетке, сцеплены — без проскальзывания и отрыва, пока вещество не разрушится (контакт с трением между телами мишени не моделируется); ");
				shown.Append(Localization.T(KeyPassportMeshBonded)).Append("; ");
			}
		}
		int emitted = 0;
		for (int i2 = 0; i2 < posed.Count; i2++)
		{
			PosedObject o2 = posed[i2];
			for (int j2 = 0; j2 < o2.specs.Count; j2++)
			{
				SuperPlateSpec sp3 = o2.specs[j2];
				SuperScenePose.ShiftZ(ref sp3, dz);
				o2.specs[j2] = sp3;
			}
			if (o2.isMesh)
			{
				plates.Add(o2.specs[0]);
				labels.Add("часть модели «" + o2.Name + "»");
				labelsShown.Add(Localization.T(KeyBodyMeshPart, o2.Shown));
				if (!o2.onLine)
				{
					passport.Append("«" + o2.Name + "» не на линии выстрела: запечено и может поймать обломки, но это не первое касание, не мишень и не судья; ");
					shown.Append(Localization.T("super.passport.bystander", o2.Shown)).Append("; ");
				}
				while (objOf.Count < plates.Count)
				{
					objOf.Add(i2);
					kindOf.Add(ArmorPlate.ObjectKind.Plate);
				}
				if (o2.onLine)
				{
					emitted++;
				}
				continue;
			}
			if (clip.present)
			{
				AppendClip(o2, passport, shown);
			}
			if (o2.plate.kind == ArmorPlate.ObjectKind.Nera)
			{
				EmitNera(o2.np, o2.npName, o2.pack, o2.specs, o2.packTilt, o2.plate, plates, labels, labelsShown, passport, shown);
			}
			else if (o2.plate.kind == ArmorPlate.ObjectKind.Rod)
			{
				if (o2.onLine && hollowTargetSpec < 0)
				{
					hollowTargetSpec = plates.Count;
					hollowTargetPlate = o2.plate;
					hollowAheadCount = emitted;
				}
				plates.Add(o2.specs[0]);
				labels.Add("стержень «" + o2.plate.name + "»");
				labelsShown.Add(Localization.T(KeyBodyRod, ShownObjectName(o2.plate)));
				passport.Append(string.Format("«{0}» — стержень ⌀{1:F0} × {2:F0} мм, ", o2.plate.name, o2.specs[0].thicknessM * 1000f, o2.specs[0].widthM * 1000f) + string.Format("запечён круглым телом, снос {0:F0} мм от линии выстрела; ", o2.lateralM * 1000f));
				shown.Append(Localization.T(KeyPassportRod, ShownObjectName(o2.plate), o2.specs[0].thicknessM * 1000f, o2.specs[0].widthM * 1000f, o2.lateralM * 1000f)).Append("; ");
				AppendEdgesFree(o2.plate, passport, shown);
			}
			else if (o2.plate.kind == ArmorPlate.ObjectKind.Plate)
			{
				if (o2.onLine && hollowTargetSpec < 0)
				{
					hollowTargetSpec = plates.Count;
					hollowTargetPlate = o2.plate;
					hollowAheadCount = emitted;
				}
				plates.Add(o2.specs[0]);
				labels.Add("плита «" + o2.plate.name + "»");
				labelsShown.Add(Localization.T("super.body.plate", ShownObjectName(o2.plate)));
				passport.Append(string.Format("«{0}» запечена как построена: наклон {1:F1}°, ", o2.plate.name, o2.obliquityDeg) + string.Format("снос {0:F0} мм от линии выстрела, ", o2.lateralM * 1000f) + string.Format("{0:F0}×{1:F0} мм; ", o2.specs[0].widthM * 1000f, o2.specs[0].heightM * 1000f));
				shown.Append(Localization.T("super.passport.pose", ShownObjectName(o2.plate), o2.obliquityDeg, o2.lateralM * 1000f, o2.specs[0].widthM * 1000f, o2.specs[0].heightM * 1000f)).Append("; ");
				AppendEdgesFree(o2.plate, passport, shown);
			}
			else if (o2.plate.kind == ArmorPlate.ObjectKind.Era)
			{
				EmitEra(o2.ep, o2.epName, o2.cassette, o2.specs[0], o2.specs[1], o2.sideM, o2.plate, plates, labels, labelsShown, passport, shown, ref chain, projBodies, triggerBodyIndex, o2.onLine);
				if (!o2.onLine)
				{
					passport.Append("кассета «" + o2.ep.displayName + "» в стороне от линии выстрела — привод не взводится, метаемые пластины инертны; ");
					shown.Append(Localization.T("super.passport.bystander_inert", o2.epName)).Append("; ");
				}
			}
			if (!o2.onLine)
			{
				passport.Append("«" + o2.plate.name + "» не на линии выстрела: запечено и может поймать обломки, но это не первое касание, не мишень и не судья; ");
				shown.Append(Localization.T("super.passport.bystander", ShownObjectName(o2.plate))).Append("; ");
			}
			while (objOf.Count < plates.Count)
			{
				objOf.Add(i2);
				kindOf.Add(o2.plate.kind);
			}
			if (o2.onLine)
			{
				emitted += o2.specs.Count;
			}
		}
		if (meshTuples.Count > 0)
		{
			for (int i3 = 0; i3 < plates.Count; i3++)
			{
				SuperPlateSpec spec4 = plates[i3];
				if (spec4.mesh != null)
				{
					continue;
				}
				foreach ((SandboxModel sm5, SuperMeshRef meshRef3, SuperMeshVoxels vx3) in meshTuples)
				{
					float spacing2 = SuperBaker.ResolveSpacing(spec4.spacingM, SuperPreset.SpacingM, labels[i3]);
					int count = SuperClip.CountInMesh(in spec4, spacing2, vx3, out int ownerPart);
					if (count > 0)
					{
						string overlapName = sm5.DisplayName + "/" + sm5.prepared.parts[ownerPart].name;
						reasonKey = KeyPlateInModel;
						reasonArgs = new object[3] { labelsShown[i3], overlapName, count };
						if (!quiet)
						{
							Debug.LogWarning("[Super] " + labels[i3] + " заходит в вещество части «" + overlapName + "» " + string.Format("модели «{0}» ({1} частиц) — два вещества в одном объёме не сцена.", sm5.DisplayName, count));
						}
						return false;
					}
				}
			}
		}
		return true;
	}

	private static Matrix4x4 BakerMatrix(SuperScenePose.BakerFrame f)
	{
		Matrix4x4 m = Matrix4x4.identity;
		m[0, 0] = f.x.x;
		m[0, 1] = f.x.y;
		m[0, 2] = f.x.z;
		m[0, 3] = -Vector3.Dot(f.x, f.origin);
		m[1, 0] = f.y.x;
		m[1, 1] = f.y.y;
		m[1, 2] = f.y.z;
		m[1, 3] = -Vector3.Dot(f.y, f.origin);
		m[2, 0] = f.z.x;
		m[2, 1] = f.z.y;
		m[2, 2] = f.z.z;
		m[2, 3] = -Vector3.Dot(f.z, f.origin) + f.zShift;
		m[3, 0] = 0f;
		m[3, 1] = 0f;
		m[3, 2] = 0f;
		m[3, 3] = 1f;
		return m;
	}

	public static SuperMeshRef MeshRefFor(SandboxModel sm, ComputeRegion region, SuperScenePose.BakerFrame frame)
	{
		RegionBox box = RegionBox.Of(region);
		return MeshRefFor(sm, box, frame);
	}

	public static SuperMeshRef MeshRefFor(SandboxModel sm, RegionBox region, SuperScenePose.BakerFrame frame)
	{
		region.WorldAxes(frame, out Vector3 u, out Vector3 v, out Vector3 n);
		Matrix4x4 toRegion = Matrix4x4.identity;
		toRegion[0, 0] = u.x;
		toRegion[0, 1] = u.y;
		toRegion[0, 2] = u.z;
		toRegion[0, 3] = -Vector3.Dot(u, region.centre);
		toRegion[1, 0] = v.x;
		toRegion[1, 1] = v.y;
		toRegion[1, 2] = v.z;
		toRegion[1, 3] = -Vector3.Dot(v, region.centre);
		toRegion[2, 0] = n.x;
		toRegion[2, 1] = n.y;
		toRegion[2, 2] = n.z;
		toRegion[2, 3] = -Vector3.Dot(n, region.centre);
		toRegion[3, 0] = 0f;
		toRegion[3, 1] = 0f;
		toRegion[3, 2] = 0f;
		toRegion[3, 3] = 1f;
		SuperMeshRef r = new SuperMeshRef();
		r.source = sm.source;
		r.spacing = SuperPreset.SpacingM;
		r.hu = region.widthM * 0.5f;
		r.hv = region.heightM * 0.5f;
		r.hn = region.depthM * 0.5f;
		r.part = 0;
		r.partName = (sm.prepared != null && sm.prepared.parts.Count > 0) ? sm.prepared.parts[0].name : "";
		r.ModelToRegion = toRegion * sm.ModelToWorld;
		return r;
	}

	private static void AppendClip(PosedObject o, StringBuilder passport, StringBuilder shown)
	{
		long live = 0L;
		long band = 0L;
		long dropped = 0L;
		foreach (SuperPlateSpec pl in o.specs)
		{
			if (!pl.clip.present)
			{
				continue;
			}
			float spacing = SuperBaker.ResolveSpacing(pl.spacingM, SuperPreset.SpacingM, o.Name);
			SuperClip.Counts counts = SuperClip.CountPosedPlate(in pl, spacing);
			live += counts.live;
			band += counts.band;
			dropped += counts.dropped;
		}
		long total = dropped + band + live;
		double share = ((total > 0L) ? ((double)dropped / (double)total) : 0.0);
		passport.Append(string.Format("«{0}»: в области счёта {1} частиц, в раме {2}, за областью {3:P1} объёма — не считается; ", o.Name, live, band, share));
		shown.Append(Localization.T(KeyPassportClip, o.Shown, live, band, share)).Append("; ");
	}

	private static void AppendEdgesFree(ArmorPlate plate, StringBuilder passport, StringBuilder shown)
	{
		if (plate == null || plate.edgeClamp)
		{
			return;
		}
		passport.Append(string.Format(EdgesFreeRu, plate.name)).Append("; ");
		shown.Append(Localization.T(KeyEdgesFree, ShownObjectName(plate))).Append("; ");
	}

	private static void EmitEra(EraPreset ep, string epName, SuperEra.SceneCassette c, SuperPlateSpec front, SuperPlateSpec rear, float sideM, ArmorPlate plate, List<SuperPlateSpec> plates, List<string> labels, List<string> labelsShown, StringBuilder passport, StringBuilder shown, ref SuperEraDrive chain, int projBodies, int triggerBodyIndex, bool armed = true)
	{
		int frontBody = projBodies + plates.Count;
		plates.Add(front);
		plates.Add(rear);
		labels.Add("передняя метаемая «" + ep.displayName + "»");
		labels.Add("тыльная метаемая «" + ep.displayName + "»");
		labelsShown.Add(Localization.T("super.body.era_front", epName));
		labelsShown.Add(Localization.T("super.body.era_rear", epName));
		if (armed)
		{
			SuperEraDrive drive = new SuperEraDrive();
			drive.frontBody = frontBody;
			drive.rearBody = frontBody + 1;
			drive.triggerBody = triggerBodyIndex;
			drive.velocityMps = c.velocityMps;
			drive.initiationDelayS = c.initiationDelayS;
			drive.triggerDistanceM = SuperPreset.SpacingM;
			drive.next = chain;
			chain = drive;
		}
		string[] parts = new string[6]
		{
			string.Format("кассета «{0}»: метаемые {1:F2} мм ", ep.displayName, c.bakedPlateM * 1000f),
			string.Format("(каталог {0:F2}, ошибка толщины {1:P1}, ", c.catalogPlateM * 1000f, c.thicknessErrorFraction),
			string.Format("энергии {0:P1}, импульс точен), V_p {1:F0} м/с, ", c.energyErrorFraction, c.velocityMps),
			string.Format("просвет {0:F1} мм, задержка {1:F2} мкс, ", c.gapFlyersM * 1000f, c.initiationDelayS * 1000000f),
			string.Format("сторона {0:F0} мм (W элемента; короб ", sideM * 1000f),
			string.Format("{0:F0} мм не запекается); ", MathF.Max(plate.widthM, plate.heightM) * 1000f)
		};
		passport.Append(string.Concat(parts));
		shown.Append(Localization.T("super.passport.era", epName, c.bakedPlateM * 1000f, c.catalogPlateM * 1000f, c.thicknessErrorFraction, c.energyErrorFraction, c.velocityMps, c.gapFlyersM * 1000f, c.initiationDelayS * 1000000f, sideM * 1000f, MathF.Max(plate.widthM, plate.heightM) * 1000f)).Append("; ");
	}

	private static void EmitNera(NeraPreset np, string npName, SuperNera.Pack pk, IList<SuperPlateSpec> layers, float packTilt, ArmorPlate plate, List<SuperPlateSpec> plates, List<string> labels, List<string> labelsShown, StringBuilder passport, StringBuilder shown)
	{
		plates.AddRange(layers);
		labels.Add("лицевая пластина пакета «" + np.displayName + "»");
		labels.Add("прослойка пакета «" + np.displayName + "»");
		labels.Add("тыльная пластина пакета «" + np.displayName + "»");
		labelsShown.Add(Localization.T("super.body.nera_front", npName));
		labelsShown.Add(Localization.T("super.body.nera_inter", npName));
		labelsShown.Add(Localization.T("super.body.nera_rear", npName));
		string mount = (plate.neraMountAngleDeg != 0f) ? string.Format(" угол установки {0:F0}° добавлен к наклону объекта ({1:F0}°);", plate.neraMountAngleDeg, packTilt) : "";
		passport.Append(pk.passport + mount + " ");
		shown.Append(pk.passportShown);
		if (plate.neraMountAngleDeg != 0f)
		{
			shown.Append(' ');
			shown.Append(Localization.T("super.passport.nera_mount", plate.neraMountAngleDeg, packTilt)).Append(';');
		}
		shown.Append(' ');
	}

	private static HollowBurst MeasureHollowBurst(in SuperRodSpec rod, in SuperPlateSpec target, float axialGapM, float triggerTimeS, bool aheadBodies, string targetShown)
	{
		HollowBurst burst = default(HollowBurst);
		burst.plateThickM = target.thicknessM;
		burst.targetShown = targetShown ?? "";
		burst.aheadBodies = aheadBodies;
		if (SuperCavity.TryResolve(in rod, SuperPreset.SpacingM, out SuperCavity cavity, out _, out _, out _) && cavity.present)
		{
			float c = MathF.Max(0.05f, MathF.Abs(MathF.Cos(target.angleXDeg * 0.017453292f)));
			float gap = axialGapM - SuperPreset.SpacingM / c;
			float cavityCentre = (cavity.spanFromM + cavity.spanToM) * 0.5f;
			burst.beforeFaceM = (axialGapM - MathF.Max(0f, gap) + cavityCentre - MathF.Max(0f, triggerTimeS) * rod.velocityMps) * c;
			burst.behindRearM = -burst.beforeFaceM - MathF.Max(0f, target.thicknessM);
			burst.present = true;
		}
		return burst;
	}

	public static bool ReadScene(Projectile proj, out SuperRodSpec rod, out SuperJetSpec jet, out bool isJet, out List<SuperPlateSpec> plates, out SuperEraDrive drive, out string reasonKey, out object[] reasonArgs, bool reactive = true, bool quiet = false)
	{
		return ReadScene(proj, out rod, out jet, out isJet, out plates, out drive, out reasonKey, out reasonArgs, out SceneView view, reactive, quiet);
	}

	public static bool ReadScene(Projectile proj, out SuperRodSpec rod, out SuperJetSpec jet, out bool isJet, out List<SuperPlateSpec> plates, out SuperEraDrive drive, out string reasonKey, out object[] reasonArgs, out SceneView view, bool reactive = true, bool quiet = false)
	{
		rod = default(SuperRodSpec);
		jet = default(SuperJetSpec);
		view = default(SceneView);
		plates = new List<SuperPlateSpec>();
		drive = null;
		reasonKey = null;
		reasonArgs = new object[0];
		LastPassport = "";
		LastPassportShown = "";
		LastHollowBurst = default(HollowBurst);
		ProjectileDefinition d = proj.GetDefinition();
		int projBodies = 1;
		int triggerBodyIndex = 0;
		isJet = d.kind == ProjectileClass.ShapedCharge;
		ProjectileClass kind = d.kind;
		if (kind != ProjectileClass.LongRod && kind != ProjectileClass.ArmorPiercing && kind != ProjectileClass.Kinetic && kind != ProjectileClass.ShapedCharge && kind != ProjectileClass.HollowBody)
		{
			reasonKey = KeyRound;
			return false;
		}
		if (RodPassports.Refusal(d, out reasonKey, out reasonArgs))
		{
			return false;
		}
		float threatSpeed;
		Vector3 origin = proj.transform.position;
		Vector3 dir = proj.direction;
		if (dir.sqrMagnitude > 9.99999972e-10f)
		{
			dir = dir.normalized;
		}
		else
		{
			dir = Vector3.forward;
		}
		if (isJet)
		{
			if (d.isTandem)
			{
				reasonKey = KeyTandem;
				return false;
			}
			jet = default(SuperJetSpec);
			if (d.kind == ProjectileClass.ShapedCharge)
			{
				ShapedChargeSpec charge = d.GetShapedChargeSpec();
				jet = SuperJet.FromCharge(in charge, MathF.Max(d.fuzeStandoffMM * 0.001f, 0.001f), 0f);
			}
			if (jet.diameterM * 0.125f < SuperPreset.SpacingM)
			{
				bool expressibleByHand = jet.diameterM * 0.125f >= SuperPreset.SpacingMinM;
				if (!quiet)
				{
					string[] warning = new string[6]
					{
						string.Format("[Super] Струя ⌀{0:F2} мм ", jet.diameterM * 1000f),
						string.Format("(длина {0:F0} мм при подрыве {1:F0} мм) ", jet.lengthM * 1000f, d.fuzeStandoffMM),
						string.Format("при шаге {0:F2} мм не разрешается сеткой: ", SuperPreset.SpacingM * 1000f),
						string.Format("{0:F1} ", jet.diameterM / (2f * SuperPreset.SpacingM)),
						string.Format("ячеек поперёк при нужных 4. Требуется шаг ⩽ {0:F2} мм. ", jet.diameterM * 0.125f * 1000f),
						expressibleByHand ? "Он выразим ручкой: нажми «Шаг под струю» в панели Super — она ставит самый крупный допустимый шаг сама. Размер струи задан зарядом и не подменяется." : (string.Format("⛔ ЭТО НИЖЕ ПОЛА РУЧКИ ({0:F2} мм): такая ", 0.2f) + "струя этим режимом не считается вовсе — не ищи настройку, её нет. Возьми заряд крупнее или жди подобной сцены отдельной фазой.")
					};
					Debug.LogWarning(string.Concat(warning));
				}
				reasonKey = (expressibleByHand ? KeyJetGrid : KeyJetTooFine);
				return false;
			}
			threatSpeed = jet.vTipMps;
		}
		else
		{
			string coreMaterial = MapCore(proj.coreType);
			if (coreMaterial == null)
			{
				reasonKey = ((proj.coreType == Projectile.CoreType.DU) ? KeyMaterialNoRow : ((proj.coreType == Projectile.CoreType.WC) ? KeyMaterialNeedsModel : KeyMaterialNoRow));
				reasonArgs = new object[1] { CoreName(proj.coreType) };
				return false;
			}
			SuperRodSpec rodSpec = default(SuperRodSpec);
			rodSpec.diameterM = MathF.Max(proj.diameter, 0.001f) * 0.001f;
			rodSpec.lengthM = MathF.Max(rodSpec.diameterM, proj.rodLengthMM * 0.001f);
			rodSpec.velocityMps = MathF.Max(proj.combatVelocity, 1f);
			rodSpec.nose = d.nose;
			rodSpec.material = coreMaterial;
			rodSpec.tipZM = 0.002f;
			rod = rodSpec;
			if (kind == ProjectileClass.HollowBody)
			{
				rod.cavityFraction = d.cavityFrac;
			}
			else if (d.ballisticCap)
			{
				if (SuperShell.FromLive(d, proj.diameter, proj.rodLengthMM, proj.combatVelocity, coreMaterial, jacketed: false, out SuperShellSpec shell, out string reason, out string shellPassport))
				{
					shell.tipZM = rod.tipZM;
					SuperShellProfile profile = SuperShellProfile.ForShell(in shell);
					if (profile.Validate(SuperPreset.SpacingM, out string failKey, out object[] failArgs, out string prose))
					{
						rod.hasProfile = true;
						rod.profile = profile;
						if (!quiet)
						{
							Debug.Log("[Super] колпачный снаряд «" + d.name + "»: " + profile.Describe() + "; " + shellPassport);
						}
					}
					else
					{
						reasonKey = failKey;
						reasonArgs = failArgs;
						if (!quiet)
						{
							Debug.LogWarning("[Super] " + prose);
						}
						return false;
					}
				}
				else
				{
					reasonKey = KeyRound;
					if (!quiet)
					{
						Debug.LogWarning("[Super] колпачный снаряд «" + d.name + "» не выражается: " + reason);
					}
					return false;
				}
			}
			projBodies = (rod.hasProfile ? rod.profile.RegionCount : 1);
			triggerBodyIndex = (rod.hasProfile ? rod.profile.ForemostRegion : 0);
			if (HollowRowSilent(d, out reasonKey, out reasonArgs))
			{
				return false;
			}
			if (kind == ProjectileClass.HollowBody && HollowCeilingRefusal(rod.velocityMps, d.fragmentVelocityMps, out reasonKey, out reasonArgs))
			{
				if (!quiet)
				{
					Debug.LogWarning("[Super] полое тело: разлёт стенки " + string.Format("{0:F0} м/с поверх встречи ", d.fragmentVelocityMps) + string.Format("{0:F0} м/с даёт до {1:F0} м/с при потолке скорости ", rod.velocityMps, reasonArgs[2]) + string.Format("сцены {0:F0} м/с — решатель уронил бы прогон как FAIL_VELOCITY, ", reasonArgs[3]) + "то есть сломанная постановка отчиталась бы вырождением счёта.");
				}
				return false;
			}
			if (rod.cavityFraction > 0f)
			{
				float spacing = RodSpacingFor(in rod, SuperPreset.SpacingM);
				if (!SuperCavity.TryResolve(in rod, spacing, out SuperCavity cavity, out reasonKey, out reasonArgs, out string cavityProse))
				{
					if (!quiet)
					{
						Debug.LogWarning(string.Format("[Super] полое тело «{0}»: полость доли {1:P1} на шаге ", d.name, rod.cavityFraction) + string.Format("{0:F2} мм не выражается — отказ выдан ДО запекания ", SuperPreset.SpacingM * 1000f) + "тем же ключом, каким откажет решатель.");
					}
					return false;
				}
			}
			threatSpeed = rod.velocityMps;
		}
		List<ArmorPlate> inFront = new List<ArmorPlate>();
		ArmorPlate[] allPlates = UnityEngine.Object.FindObjectsByType<ArmorPlate>(FindObjectsSortMode.None);
		for (int i = 0; i < allPlates.Length; i++)
		{
			ArmorPlate plate = allPlates[i];
			Vector3 pos = plate.transform.position;
			if ((pos - origin).x * dir.x + (pos - origin).y * dir.y + (pos - origin).z * dir.z > 0f && !plate.IsModel)
			{
				inFront.Add(plate);
			}
		}
		StringBuilder passport = new StringBuilder();
		StringBuilder shown = new StringBuilder();
		List<string> labels = new List<string>();
		List<string> labelsShown = new List<string>();
		SuperEraDrive chain = null;
		int hollowTargetSpec = -1;
		int hollowAheadCount = -1;
		ArmorPlate hollowTargetPlate = null;
		List<int> objOf = new List<int>();
		List<ArmorPlate.ObjectKind> kindOf = new List<ArmorPlate.ObjectKind>();
		List<SandboxModel> models = new List<SandboxModel>();
		foreach (SandboxModel o in SandboxModel.All())
		{
			if (o != null && Vector3.Dot(o.transform.position - origin, dir) > 0f)
			{
				models.Add(o);
			}
		}
		List<ComputeRegion> regionsList = ComputeRegion.All();
		ComputeRegion singleRegion = null;
		if (regionsList.Count > 1)
		{
			reasonKey = KeyRegionTwo;
			reasonArgs = new object[1] { regionsList.Count };
			return false;
		}
		if (regionsList.Count == 1)
		{
			singleRegion = regionsList[0];
		}
		RegionBox region = ((singleRegion != null) ? RegionBox.Of(singleRegion) : null);
		if (models.Count > 0 && singleRegion == null)
		{
			List<Component> components = new List<Component>();
			foreach (SandboxModel m in models)
			{
				components.Add(m);
			}
			foreach (ArmorPlate p in inFront)
			{
				components.Add(p);
			}
			SuperScenePose.BakerFrame frame = SuperScenePose.FrameFor(origin, dir);
			region = RegionBox.Auto(frame, components, SuperClip.Band * 3f * SuperPreset.SpacingM + SuperPreset.SpacingM);
			if (region == null)
			{
				reasonKey = KeyRegionMissing;
				return false;
			}
		}
		bool hasRodInFront = inFront.Exists((ArmorPlate q) => q != null && q.IsRod);
		bool hasAny = models.Count > 0 || singleRegion != null || hasRodInFront;
		PoseRoute route = NightForceRoute ?? (hasAny ? PoseRoute.General : Choose(inFront, origin, dir, SuperPreset.SpacingM));
		LastRoute = route;
		view.route = route;
		if (route == PoseRoute.Legacy && hasAny)
		{
			reasonKey = KeyLegacyS40;
			return false;
		}
		if (route != PoseRoute.General)
		{
			Vector3 up = SceneUpAxis(inFront, -dir, out string mixedName, out float mixedDeg);
			if (mixedName != null)
			{
				reasonKey = KeyMixedTilt;
				ArmorPlate mixed = null;
				foreach (ArmorPlate q in inFront)
				{
					if (q != null && q.name == mixedName)
					{
						mixed = q;
						break;
					}
				}
				reasonArgs = new object[2]
				{
					(mixed != null) ? ShownObjectName(mixed) : mixedName,
					mixedDeg.ToString("F1", Localization.Culture)
				};
				if (!quiet)
				{
					Debug.LogWarning("[Super] Плиты наклонены вокруг разных осей: «" + mixedName + "» уходит от общей оси наклона на " + mixedDeg.ToString("F1", Localization.Culture) + "° — одним поворотом запекателя это не выразить.");
				}
				return false;
			}
			List<SceneObject> sceneObjects = new List<SceneObject>();
			foreach (ArmorPlate p in inFront)
			{
				if (p.kind != ArmorPlate.ObjectKind.Plate && !(reactive && (p.kind == ArmorPlate.ObjectKind.Era || p.kind == ArmorPlate.ObjectKind.Nera)))
				{
					reasonKey = KeyNonPlate;
					return false;
				}
				Vector3 pos = p.transform.position;
				Vector3 rel = pos - origin;
				float along = rel.x * dir.x + rel.y * dir.y + rel.z * dir.z;
				float lateral = MathF.Sqrt(MathF.Max(0f, rel.sqrMagnitude - along * along));
				float tol = MathF.Max(MathF.Min(p.widthM, p.heightM), 0.02f) * 0.5f * OffAxisTolerance;
				if (lateral > tol)
				{
					reasonKey = KeyOffAxis;
					reasonArgs = new object[3] { ShownObjectName(p), (lateral * 1000f).ToString("F0", Localization.Culture), (tol * 1000f).ToString("F0", Localization.Culture) };
					if (!quiet)
					{
						Debug.LogWarning("[Super] «" + p.name + "» стоит в " + (lateral * 1000f).ToString("F0", Localization.Culture) + " мм от линии выстрела при допуске " + (tol * 1000f).ToString("F0", Localization.Culture) + " мм — запекатель кладёт тела по оси; поставьте объект на линию.");
					}
					return false;
				}
				Vector3 fwd = p.transform.forward;
				float tilt = SignedTilt(fwd, -dir, up);
				float thickM = MathF.Max(p.thicknessMM * 0.001f, 9.99999975e-05f);
				float cosT = MathF.Cos(tilt * 0.0174532924f);
				float boxShift = (MathF.Abs(cosT) >= 0.001f) ? (thickM * 0.5f / cosT) : 0f;
				SceneObject obj = default(SceneObject);
				obj.plate = p;
				obj.faceAlong = along - boxShift;
				obj.tilt = tilt;
				obj.boxT = thickM;
				obj.sideM = MathF.Max((p.kind == ArmorPlate.ObjectKind.Era) ? MathF.Min(p.widthM, p.heightM) : MathF.Max(p.widthM, p.heightM), 0.02f);
				obj.lateralM = lateral;
				sceneObjects.Add(obj);
			}
			if (sceneObjects.Count == 0)
			{
				reasonKey = KeyNoPlates;
				return false;
			}
			sceneObjects.Sort((SceneObject a, SceneObject b) => a.faceAlong.CompareTo(b.faceAlong));
			if (sceneObjects[sceneObjects.Count - 1].plate.kind != 0)
			{
				reasonKey = KeyReactiveLast;
				return false;
			}
			float firstAlong = sceneObjects[0].faceAlong;
			int objIndex = 0;
			foreach (SceneObject so in sceneObjects)
			{
				ArmorPlate p = so.plate;
				float frontFaceZ = firstAlong - so.faceAlong;
				if (so.lateralM > LegacyLateralM)
				{
					passport.Append(string.Format("«{0}» снос {1:F0} мм от линии выстрела — тело запечено по оси; ", p.name, so.lateralM * 1000f));
					object[] offAxisArgs = new object[2] { ShownObjectName(p), so.lateralM * 1000f };
					shown.Append(Localization.T("super.passport.off_axis", offAxisArgs)).Append("; ");
				}
				if (p.kind == ArmorPlate.ObjectKind.Plate)
				{
					string mat = MapMaterial(p.armorType);
					if (mat == null)
					{
						reasonKey = MaterialRefusalKey(p.armorType);
						reasonArgs = new object[1] { Catalog.MaterialName(p.armorType) };
						return false;
					}
					if (hollowTargetSpec < 0)
					{
						hollowTargetSpec = plates.Count;
						hollowTargetPlate = p;
					}
					SuperPlateSpec spec = default(SuperPlateSpec);
					spec.thicknessM = so.boxT;
					spec.angleXDeg = so.tilt;
					spec.alongAxisM = SuperBaker.AlongAxisForFaceOnAxis(so.boxT, frontFaceZ, so.tilt);
					spec.sideM = so.sideM;
					spec.material = mat;
					spec.clampedEdges = p.edgeClamp || NightSabotageEdgeLiteral;
					plates.Add(spec);
					labels.Add("плита «" + p.name + "»");
					labelsShown.Add(Localization.T("super.body.plate", ShownObjectName(p)));
					AppendEdgesFree(p, passport, shown);
				}
				else if (p.kind == ArmorPlate.ObjectKind.Era)
				{
					EraPreset ep = EraPresetOf(p);
					string epName = PresetName(ep.id, ep.displayName);
					SuperEra.SceneCassette cassette = SuperEra.BuildInScene(ep, so.sideM, so.tilt, frontFaceZ, SuperPreset.SpacingM, threatSpeed);
					if (cassette.refuseKey != null)
					{
						reasonKey = cassette.refuseKey;
						reasonArgs = cassette.refuseArgs;
						if (!quiet)
						{
							Debug.LogWarning("[Super] Кассета «" + ep.displayName + "»: " + cassette.refuseDetail);
						}
						return false;
					}
					EmitEra(ep, epName, cassette, cassette.front, cassette.rear, so.sideM, p, plates, labels, labelsShown, passport, shown, ref chain, projBodies, triggerBodyIndex, armed: true);
				}
				else if (p.kind == ArmorPlate.ObjectKind.Nera)
				{
					NeraPreset np = NeraPresetOf(p);
					string npName = PresetName(np.id, np.displayName);
					float packTilt = so.tilt + p.neraMountAngleDeg;
					SuperNera.Pack pk = SuperNera.Build(np, so.sideM, packTilt, frontFaceZ, SuperPreset.SpacingM, p.neraFrontMaterial, p.neraRearMaterial, np.bonding, p.neraInterlayer);
					if (pk.refuseKey != null)
					{
						reasonKey = pk.refuseKey;
						reasonArgs = pk.refuseArgs;
						if (!quiet)
						{
							Debug.LogWarning("[Super] Пакет «" + np.displayName + "»: " + pk.refuseDetail);
						}
						return false;
					}
					EmitNera(np, npName, pk, pk.plates, packTilt, p, plates, labels, labelsShown, passport, shown);
				}
				while (objOf.Count < plates.Count)
				{
					objOf.Add(objIndex);
					kindOf.Add(p.kind);
				}
				objIndex++;
			}
			hollowAheadCount = hollowTargetSpec;
		}
		else
		{
			float rodRadiusM = (isJet ? jet.diameterM : rod.diameterM) * 0.5f;
			if (!ReadGeneralBodies(inFront, origin, dir, rodRadiusM, threatSpeed, reactive, quiet, plates, labels, labelsShown, objOf, kindOf, passport, shown, ref chain, ref hollowTargetSpec, ref hollowTargetPlate, ref hollowAheadCount, ref view, projBodies, triggerBodyIndex, out reasonKey, out reasonArgs, models, region))
			{
				return false;
			}
		}
		string aheadNote = "";
		if (kind == ProjectileClass.HollowBody)
		{
			float triggerTimeS = SuperPreset.TriggerTimeFor(d.fuzeDelayS);
			SuperPlateSpec target = ((hollowTargetSpec >= 0) ? plates[hollowTargetSpec] : default(SuperPlateSpec));
			float cosTarget = (hollowTargetSpec >= 0) ? MathF.Abs(MathF.Cos(target.angleXDeg * 0.0174532924f)) : 0f;
			if (hollowTargetSpec < 0 || 0.001f > cosTarget)
			{
				reasonKey = KeyHollowTarget;
				object[] hollowTargetArgs = new object[2]
				{
					(hollowTargetPlate != null) ? ShownObjectName(hollowTargetPlate) : "",
					(hollowTargetSpec >= 0) ? target.angleXDeg.ToString("F1", Localization.Culture) : "-"
				};
				reasonArgs = hollowTargetArgs;
				if (!quiet)
				{
					string hollowTargetWarning = "[Super] полое тело: опорной плоскости времени срабатывания нет — ";
					if (hollowTargetSpec < 0)
					{
						hollowTargetWarning += "в сцене нет ни одной плиты настоящей брони (только реактивная защита).";
					}
					else
					{
						hollowTargetWarning = hollowTargetWarning + "мишень «" + hollowTargetPlate.name + "» наклонена на " + target.angleXDeg.ToString("F1", Localization.Culture) + "°, " + "то есть стоит почти вдоль выстрела.";
					}
					Debug.LogWarning(hollowTargetWarning);
				}
				return false;
			}
			SuperShellDrive shellDrive = new SuperShellDrive();
			shellDrive.velocityMps = d.fragmentVelocityMps;
			shellDrive.initiationDelayS = triggerTimeS;
			shellDrive.triggerDistanceM = SuperPreset.SpacingM;
			shellDrive.targetPlateBody = hollowTargetSpec + 1;
			shellDrive.next = chain;
			chain = shellDrive;
			passport.Append(string.Concat(new string[10]
			{
				string.Format("полое тело «{0}»: доля полости {1:P1}, ", d.name, rod.cavityFraction),
				string.Format("радиальный импульс по стенке {0:F0} м/с ", d.fragmentVelocityMps),
				string.Format("(рампа {0:F2} мкс), время срабатывания ", 3f),
				string.Format("{0:F1} мкс ", triggerTimeS * 1000000f),
				"(",
				SuperPreset.TriggerTimeSet ? "ручка панели" : "значение строки",
				") от касания тела «",
				hollowTargetPlate.name,
				"» ",
				string.Format("(тело {0} из {1}); ", hollowTargetSpec + 1, plates.Count)
			}));
			object[] hollowArgs = new object[4] { rod.cavityFraction, d.fragmentVelocityMps, triggerTimeS * 1000000f, ShownObjectName(hollowTargetPlate) };
			shown.Append(Localization.T("super.passport.hollow", hollowArgs)).Append("; ");
			if (hollowAheadCount > 0)
			{
				aheadNote = Localization.T("super.passport.hollow_ahead", hollowAheadCount);
				passport.Append(string.Format("Перед мишенью стоят тела ({0}): оценка касания ", hollowAheadCount) + "в проверке окна проходит их СВОБОДНЫМ ПОЛЁТОМ (торможения модель не считает), то есть и оценка касания, и нужное окно — НИЖНЯЯ граница. ");
				shown.Append(aheadNote).Append(' ');
			}
		}
		if (!isJet && MaterialNoteKey(rod.material, asRod: true) != null)
		{
			passport.Append(UraniumRodNoteRu).Append("; ");
			shown.Append(Localization.T(MaterialNoteKey(rod.material, asRod: true))).Append("; ");
		}
		foreach (SuperPlateSpec pl in plates)
		{
			if (pl.material != null && MaterialNoteKey(pl.material, asRod: false) != null)
			{
				passport.Append(UraniumPlateNoteRu).Append("; ");
				shown.Append(Localization.T(MaterialNoteKey(pl.material, asRod: false))).Append("; ");
			}
		}
		float sepLimit = (SuperPreset.SpacingM + SuperPreset.SpacingM) * 3f;
		HashSet<int> bondedFlyers = new HashSet<int>();
		for (int ii = 0; ii < plates.Count; ii++)
		{
			for (int jj = ii + 1; jj < plates.Count; jj++)
			{
				if (objOf[ii] == objOf[jj] || plates[ii].mesh != null || plates[jj].mesh != null)
				{
					continue;
				}
				float sep = SuperScenePose.Separation(plates[ii], plates[jj]);
				if (sep > sepLimit)
				{
					continue;
				}
				if (SuperPreset.SpacingM * -0.5f > sep)
				{
					reasonKey = KeyBodiesOverlap;
					reasonArgs = new object[3]
					{
						labelsShown[ii],
						labelsShown[jj],
						(-sep * 1000f).ToString("F1", Localization.Culture)
					};
					if (!quiet)
					{
						Debug.LogWarning(string.Concat("[Super] ", labels[ii], " и ", labels[jj], " заходят друг в друга на ", string.Format("{0:F1} мм (допуск — полшага частиц, {1:F2} мм): ", -sep * 1000f, SuperPreset.SpacingM * 0.5f * 1000f), "два вещества в одном объёме не сцена. Раздвиньте тела."));
					}
					LastPassport = passport.ToString();
					LastPassportShown = shown.ToString();
					return false;
				}
				view.bonded++;
				passport.Append(string.Format("{0} и {1} сцеплены: просвет {2:F1} мм меньше трёх ", labels[ii], labels[jj], sep * 1000f) + string.Format("ячеек сетки ({0:F1} мм) — без проскальзывания и отрыва, пока ", sepLimit * 1000f) + "вещество не разрушится");
				object[] bondedArgs = new object[4] { labelsShown[ii], labelsShown[jj], sep * 1000f, sepLimit * 1000f };
				shown.Append(Localization.T("super.passport.bonded", bondedArgs));
				float gapCells = sep / 6f;
				if (sep > 0f && gapCells >= 0.0002f)
				{
					passport.Append(string.Format("; шаг частиц мельче {0:F2} мм разрешит этот зазор", gapCells * 1000f)).Append("; ");
					shown.Append(Localization.T("super.passport.bonded_hint", gapCells * 1000f));
				}
				passport.Append("; ");
				shown.Append("; ");
				for (int swap = 0; swap < 2; swap++)
				{
					int eraIndex = (swap == 0) ? ii : jj;
					int armourIndex = (swap == 0) ? jj : ii;
					if (kindOf[eraIndex] == ArmorPlate.ObjectKind.Era && kindOf[armourIndex] == ArmorPlate.ObjectKind.Plate && bondedFlyers.Add(eraIndex))
					{
						passport.Append(labels[eraIndex] + " сцеплена с бронёй и метаться не будет; ");
						shown.Append(Localization.T("super.passport.bonded_flyer", labelsShown[eraIndex])).Append("; ");
					}
				}
			}
		}
		drive = chain;
		LastPassport = passport.ToString();
		LastPassportShown = shown.ToString();
		if (!quiet && LastPassport.Length > 0)
		{
			Debug.Log("[Super] Паспорт сцены: " + LastPassport);
		}
		float tipZ;
		if (route == PoseRoute.General)
		{
			float halfDia = (isJet ? jet.diameterM : rod.diameterM) * 0.5f;
			tipZ = SuperScenePose.TipZForClearance(plates, SuperPreset.SpacingM, halfDia, 0.002f, out int bindingSpec);
			foreach (SuperPlateSpec pl in plates)
			{
				if (pl.mesh == null)
				{
					continue;
				}
				SuperMeshVoxels vx = SuperMeshCache.GetOrCompute(pl.mesh);
				float rodSpacing = (isJet ? SuperBaker.ResolveSpacing(jet.spacingM, SuperPreset.SpacingM, "струя") : RodSpacingFor(in rod, SuperPreset.SpacingM));
				double need = SuperScenePose.MeshTipNeed(vx, pl.mesh.part, pl.clip, pl.mesh.spacing, rodSpacing, halfDia, 0.002f);
				if (need != 0.0 && (tipZ == float.NegativeInfinity || need > (double)tipZ))
				{
					tipZ = (float)need;
				}
			}
			if (tipZ == float.NegativeInfinity)
			{
				tipZ = 0.002f;
			}
			if (!isJet)
			{
				rod.tipZM = tipZ;
			}
			else
			{
				jet.tipZM = tipZ;
			}
		}
		else
		{
			SuperPlateSpec front = plates[0];
			if (!isJet)
			{
				rod.tipZM = SuperBaker.RodTipForClearance(front, rod.diameterM, 0.002f);
			}
			else
			{
				jet.tipZM = SuperBaker.RodTipForClearance(front, jet.diameterM, 0.002f);
			}
		}
		if (kind == ProjectileClass.HollowBody)
		{
			SuperPlateSpec target = plates[hollowTargetSpec];
			float axialGapM = rod.tipZM - SuperBaker.FaceOnAxis(target);
			float triggerTimeS2 = SuperPreset.TriggerTimeFor(d.fuzeDelayS);
			string targetShown = ((hollowTargetSpec < labelsShown.Count) ? labelsShown[hollowTargetSpec] : "");
			bool aheadBodies = hollowAheadCount > 0;
			LastHollowBurst = MeasureHollowBurst(in rod, in target, axialGapM, triggerTimeS2, aheadBodies, targetShown);
			if (HollowWindowRefusal(axialGapM, target.angleXDeg, rod.velocityMps, SuperPreset.SpacingM, SuperPreset.TriggerTimeFor(d.fuzeDelayS), 4.99999987e-06f, SuperPreset.DurationS, aheadNote, out reasonKey, out reasonArgs))
			{
				if (!quiet)
				{
					Debug.LogWarning(string.Concat("[Super] полое тело: импульс не состоялся бы в окне. Оценка касания ", string.Format("{0:F1} мкс + время срабатывания {1:F1} мкс + ", reasonArgs[0], reasonArgs[1]), string.Format("рампа {0:F1} мкс = {1:F1} мкс при окне ", reasonArgs[2], reasonArgs[3]), string.Format("{0:F1} мкс. Привод нашёл бы касание, поставил бы пуск ", SuperPreset.DurationS * 1000000f), "за концом окна и не выдал бы НИ ОДНОГО приращения: прогон вышел бы побитово равным инертному телу, без единой строки причины (контрфакт F11 приёмки S27).", (hollowAheadCount > 0) ? (string.Format(" ⚠ Перед мишенью стоят тела ({0} шт.): их оценка проходит ", hollowAheadCount) + "свободным полётом, торможения в ней нет — нужное окно НИЖНЯЯ граница.") : ""));
				}
				return false;
			}
		}
		float thinnestM = float.MaxValue;
		foreach (SuperPlateSpec pl in plates)
		{
			if (pl.thicknessM <= thinnestM)
			{
				thinnestM = pl.thicknessM;
			}
		}
		double volume;
		if (isJet)
		{
			volume = (double)jet.diameterM * (double)(MathF.PI / 4f) * (double)jet.diameterM * (double)jet.lengthM;
		}
		else
		{
			volume = SuperCavity.BudgetVolume(in rod, SuperPreset.SpacingM);
		}
		long particles = SuperBaker.EstimateParticles(volume, plates, SuperPreset.SpacingM);
		if ((double)particles > SuperPreset.ParticleCeiling)
		{
			float spacingForBudget = SpacingForCeiling(volume, plates, SuperPreset.ParticleCeiling);
			float spacingForHardBudget = SpacingForCeiling(volume, plates, MathF.Max(1f, SuperPreset.ParticleBudget));
			if (kind == ProjectileClass.HollowBody)
			{
				float wallCeiling = SpacingCeilingForWall(in rod);
				if (rod.cavityFraction > wallCeiling || spacingForBudget > wallCeiling)
				{
					reasonKey = KeyHollowSpacing;
					reasonArgs = new object[4]
					{
						spacingForBudget * 1000f,
						(wallCeiling > rod.cavityFraction) ? (wallCeiling * 1000f) : 0.2f,
						WallLayersAt(in rod, spacingForBudget),
						4
					};
					if (!quiet)
					{
						Debug.LogWarning(string.Concat("[Super] полое тело: под потолок частиц нужен шаг не мельче ", string.Format("{0:F2} мм, ", spacingForBudget * 1000f), "а стенка полости выражается только до ", (wallCeiling > rod.cavityFraction) ? (wallCeiling * 1000f).ToString("F2") : "НИ ОДНОГО", " мм ", string.Format("(на {0:F2} мм слоёв стенки {1} при нужных ", spacingForBudget * 1000f, WallLayersAt(in rod, spacingForBudget)), string.Format("{0}). Шага, годного обоим, нет: уменьши сцену в редакторе ", 4), "либо долю полости. Размеры сцены не подменяются."));
					}
					return false;
				}
			}
			if (!quiet)
			{
				Debug.LogWarning(string.Concat("[Super] Сцена требует ", string.Format("{0:F1} млн частиц при шаге ", (double)particles / 1000000.0), string.Format("{0:F2} мм — столько не помещается в видеопамять ", SuperPreset.SpacingM * 1000f), string.Format("этой машины (потолок {0:F1} млн). Влезает при шаге ", SuperPreset.ParticleCeiling / 1000000.0), string.Format("не мельче {0:F2} мм; кнопка «Шаг под бюджет» в панели Super ставит ", spacingForBudget * 1000f), string.Format("{0:F2} мм (под {1:F1} млн частиц). Либо ", spacingForHardBudget * 1000f, SuperPreset.ParticleBudget / 1000000.0), "уменьши плиты в редакторе. Размеры сцены не подменяются."));
			}
			reasonKey = KeyTooBig;
			reasonArgs = new object[5]
			{
				(double)particles / 1000000.0,
				SuperPreset.SpacingM * 1000f,
				SuperPreset.ParticleCeiling / 1000000.0,
				spacingForBudget * 1000f,
				spacingForHardBudget * 1000f
			};
			return false;
		}
		float cellsAcross = SuperPreset.CellsAcross(thinnestM, SuperPreset.SpacingM);
		if (cellsAcross < 4f && !quiet)
		{
			Debug.LogWarning(string.Format("[Super] Самое тонкое тело ({0:F1} мм) укладывается ", thinnestM * 1000f) + string.Format("всего в {0:F1} частиц по толщине — его разрушение сетка не разрешает. ", cellsAcross) + "Для осмысленной картины нужен шаг мельче.");
		}
		float cavityOnRod = (isJet ? 0f : rod.cavityFraction);
		SuperEraDrive driveOrNull = drive;
		bool shellDriveBuilt = driveOrNull is SuperShellDrive;
		if (!HollowPairingBroken(d, cavityOnRod, shellDriveBuilt, out reasonKey, out reasonArgs))
		{
			return true;
		}
		return false;
	}

	public static float SignedTilt(Vector3 plateNormal, Vector3 shotDir)
	{
		Vector3 d = shotDir;
		float m = d.magnitude;
		if (m > 1e-05f)
		{
			d /= m;
		}
		else
		{
			d = Vector3.forward;
		}
		Vector3 z = -d;
		Vector3 up = Vector3.up - z * Vector3.Dot(Vector3.up, z);
		if (up.sqrMagnitude < 1e-08f)
		{
			return 0f;
		}
		float um = up.magnitude;
		if (um > 1e-05f)
		{
			up /= um;
		}
		else
		{
			up = Vector3.forward;
		}
		return SignedTilt(plateNormal, z, up);

	}

	public static float SignedTilt(Vector3 plateNormal, Vector3 bakerForward, Vector3 upAxis)
	{
		Vector3 fwd = bakerForward;
		float fm = fwd.magnitude;
		if (fm > 1e-05f)
		{
			fwd /= fm;
		}
		else
		{
			fwd = Vector3.forward;
		}
		Vector3 n = plateNormal;
		float nm = n.magnitude;
		if (nm > 1e-05f)
		{
			n /= nm;
		}
		else
		{
			n = Vector3.forward;
		}
		if (Vector3.Dot(n, fwd) < 0f)
		{
			n = -n;
		}
		if (upAxis.sqrMagnitude < 1e-08f)
		{
			return 0f;
		}
		return MathF.Atan2(-Vector3.Dot(upAxis, n), Vector3.Dot(upAxis, fwd)) * 57.29578f;

	}

	public static string MapMaterial(ArmorPlate.ArmorType m)
	{
		switch (m)
		{
		case ArmorPlate.ArmorType.RHA:
			return "RHA_A";
		case ArmorPlate.ArmorType.Aluminum:
			return "AL5083";
		case ArmorPlate.ArmorType.Titanium:
			return "TI6AL4V";
		case ArmorPlate.ArmorType.Tungsten:
			return "WHA";
		case ArmorPlate.ArmorType.DepletedUraniumArmour:
			return NightUraniumRowOff ? null : "DU_075TI";
		case ArmorPlate.ArmorType.RhaSetB:
			return "RHA_B";
		case ArmorPlate.ArmorType.Steel4340:
			return "STEEL4340";
		case ArmorPlate.ArmorType.Polyurethane:
			return SuperNera.RegistryNameFor(InterlayerMaterial.Polyurethane);
		case ArmorPlate.ArmorType.Neoprene:
			return SuperNera.RegistryNameFor(InterlayerMaterial.Neoprene);
		case ArmorPlate.ArmorType.NaturalRubber:
			return SuperNera.RegistryNameFor(InterlayerMaterial.NaturalRubber);
		}
		return null;

	}

	public static string MapCore(Projectile.CoreType c)
	{
		switch (c)
		{
		case Projectile.CoreType.WHA:
			return "WHA";
		case Projectile.CoreType.DU:
			return NightUraniumRowOff ? null : "DU_075TI";
		case Projectile.CoreType.Steel:
			return "HARDCORE_N12E";
		}
		return null;

	}

	public static string MaterialRefusalKey(ArmorPlate.ArmorType m)
	{
		switch (m)
		{
		case ArmorPlate.ArmorType.HHA:
			return "super.refuse.material.needs_j3";
		case ArmorPlate.ArmorType.Ceramic:
		case ArmorPlate.ArmorType.BoronCarbide:
		case ArmorPlate.ArmorType.TitaniumDiboride:
		case ArmorPlate.ArmorType.Alumina:
			return "super.refuse.material.needs_model";
		case ArmorPlate.ArmorType.Textolite:
			return "super.refuse.material.anisotropic";
		case ArmorPlate.ArmorType.CastSteel:
			return "super.refuse.material.not_accepted";
		}
		return "super.refuse.material.no_row";

	}

	public static string CoreRefusalKey(Projectile.CoreType c)
	{
		if (c == Projectile.CoreType.WC)
		{
			return "super.refuse.material.needs_model";
		}
		return "super.refuse.material.no_row";

	}

	public static string CoreName(Projectile.CoreType c)
	{
		foreach (CoreCatalogEntry e in Catalog.Cores)
		{
			if (e.type == c)
			{
				return e.meta.Name;
			}
		}
		return c.ToString();
	}

	private static void Refuse(string key, object[] args = null)
	{
		string text = ((args != null && args.Length > 0) ? Localization.T(key, args) : Localization.T(key));
		Debug.LogWarning(text);
		if (ResultDisplay.Instance != null)
		{
			ResultDisplay.Instance.Show(text, MenuTheme.Refused);
		}
	}

	public static SceneCost Cost(Projectile proj)
	{
		SceneCost cost = default(SceneCost);
		cost.reasonArgs = new object[0];
		cost.reasonText = "";
		cost.passport = "";
		if (proj == null)
		{
			cost.reasonKey = KeyNoPlates;
			cost.reasonText = Localization.T(KeyNoPlates);
			return cost;
		}
		cost.ok = ReadScene(proj, out SuperRodSpec rod, out SuperJetSpec jet, out cost.isJet, out List<SuperPlateSpec> plates, out SuperEraDrive drive, out cost.reasonKey, out cost.reasonArgs, out SceneView view, reactive: true, quiet: true);
		cost.passport = LastPassport;
		cost.windowS = SuperPreset.DurationS;
		ProjectileDefinition def = proj.GetDefinition();
		cost.isHollow = def != null && def.IsHollowBody;
		if (cost.isHollow)
		{
			cost.hollowTriggerRowS = def.fuzeDelayS;
			cost.hollowCavity = def.cavityFrac;
			cost.hollowWallVMps = def.fragmentVelocityMps;
			cost.hollowSpacingCeilingM = SpacingCeilingForWall(in rod);
			cost.hollowBurst = LastHollowBurst;
		}
		if (cost.isJet)
		{
			cost.jetDiameterM = jet.diameterM;
		}
		if (!cost.ok)
		{
			cost.reasonText = ((cost.reasonArgs != null && cost.reasonArgs.Length > 0) ? Localization.T(cost.reasonKey, cost.reasonArgs) : Localization.T(cost.reasonKey));
		}
		else if (cost.isJet)
		{
			if (plates == null)
			{
				return cost;
			}
			if (plates.Count > 0)
			{
				cost.windowS = MathF.Min(SuperPreset.DurationS, SuperSolveJob.JetWindowS(in jet, plates));
			}
		}
		if (plates == null || plates.Count < 1)
		{
			return cost;
		}
		double rodVolume = (cost.isJet ? ((double)jet.diameterM * MathF.PI * 0.25 * jet.diameterM * jet.lengthM) : SuperCavity.BudgetVolume(in rod, SuperPreset.SpacingM));
		cost.particles = SuperBaker.EstimateParticles(rodVolume, plates, SuperPreset.SpacingM);
		cost.bodies = plates.Count;
		cost.thinnestM = float.MaxValue;
		foreach (SuperPlateSpec pl in plates)
		{
			cost.thinnestM = MathF.Min(cost.thinnestM, pl.thicknessM);
		}
		if (!cost.ok)
		{
			return cost;
		}
		cost.plan = PlanFor(cost.isJet, rod, jet, plates, SuperPreset.SpacingM, out float windowS);
		if (VramRefusal(cost.plan, cost.isJet, rod, jet, plates, out string key, out object[] args))
		{
			cost.ok = false;
			cost.reasonKey = key;
			cost.reasonArgs = args;
			cost.reasonText = Localization.T(key, args);
		}
		return cost;
	}

	public static float SpacingForCeiling(double rodVolumeM3, IList<SuperPlateSpec> plates, double ceiling)
	{
		float hi = 0.008f;
		float lo = 0.0002f;
		if (SuperBaker.EstimateParticles(rodVolumeM3, plates, hi) > ceiling)
		{
			return hi;
		}
		if (ceiling >= SuperBaker.EstimateParticles(rodVolumeM3, plates, lo))
		{
			return lo;
		}
		for (int i = 0; i < 24; i++)
		{
			float mid = (hi + lo) * 0.5f;
			if (SuperBaker.EstimateParticles(rodVolumeM3, plates, mid) > ceiling)
			{
				lo = mid;
			}
			else
			{
				hi = mid;
			}
		}
		return hi;

	}

	public static string ShownObjectName(ArmorPlate p)
	{
		if (p == null)
		{
			return "";
		}
		string n = p.name ?? "";
		string prefix = ArmorPlate.KindName(p.kind) + "_";
		if (n.Length > prefix.Length && n.StartsWith(prefix, StringComparison.Ordinal))
		{
			string rest = n.Substring(prefix.Length);
			int i = 0;
			while (i < rest.Length)
			{
				if (rest[i] - '0' > 9)
				{
					return n;
				}
				i++;
			}
			return Catalog.ObjectName(p.kind) + "_" + rest;
		}
		return n;

	}

	public static string PresetName(string id, string displayName)
	{
		if (string.IsNullOrEmpty(id))
		{
			return displayName ?? "";
		}
		string key = "object." + id.ToLowerInvariant();
		if (Localization.HasKey(key))
		{
			return Localization.T(key);
		}
		return string.IsNullOrEmpty(displayName) ? id : displayName;

	}

	public static EraPreset EraPresetOf(ArmorPlate p)
	{
		string id = EraCatalog.MatchingId(p);
		if (id != null && EraCatalog.TryGet(id, out EraPreset preset))
		{
			return preset;
		}
		return new EraPreset
		{
			id = "era_custom",
			displayName = "ДЗ (ручная)",
			category = "Активная защита",
			armor = p.armorType,
			plateMM = p.eraPlateThicknessMM,
			gapMM = p.eraInterlayerGapMM,
			velocityMS = p.eraPlateVelocityMS,
			extentMM = p.eraCassetteExtentMM,
			provenance = "числа объекта, изменённые игроком"
		};
	}

	public static NeraPreset NeraPresetOf(ArmorPlate p)
	{
		string id = NeraCatalog.MatchingId(p);
		if (id != null && NeraCatalog.TryGet(id, out NeraPreset preset))
		{
			return preset;
		}
		return new NeraPreset
		{
			id = "nera_custom",
			displayName = "НДЗ (ручная)",
			category = "Выпучивающаяся броня",
			frontMM = p.neraFrontThicknessMM,
			rearMM = p.neraRearThicknessMM,
			interlayerMM = p.neraInterlayerMM,
			frontMaterial = p.neraFrontMaterial,
			rearMaterial = p.neraRearMaterial,
			interlayer = p.neraInterlayer,
			bonding = p.neraBonding,
			extentMM = p.neraExtentMM,
			mountAngleDeg = p.neraMountAngleDeg,
			provenance = "числа объекта, изменённые игроком"
		};
	}
}
