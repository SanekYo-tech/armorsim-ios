using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class PenetrationPlayer : MonoBehaviour
{
	private class JetLayer
	{
		public int layerIndex;

		public float standoffM;

		public int elementsBefore;

		public int elementsRetired;

		public JetChannel channel;

		public DeformableMesh.ChannelProfile profile;

		public DeformableMesh mesh;

		public float fullThickWU;

		public int budget;

		public int spawned;

		public bool through;

		public bool stopped;

		public float exitBulge;

		public int loggedIndex;

		public Vector3 spallAxis;

		public float spallSpeed;

		public float spallRadiusM;

		public float spallPrevDepthM;

		public bool ignoreSource;
	}

	private class EraTrigger
	{
		public int layerIndex;

		public float arrivalS;

		public int elementsBefore;

		public int elementsRetired;

		public bool fired;
	}

	[CompilerGenerated]
	private sealed class _003CPrecomputeThenPlay_003Ed__177 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public PenetrationPlayer _003C_003E4__this;

		public int evLayer;

		private ProgressScreen _003Cpr_003E5__2;

		private Stopwatch _003Csw_003E5__3;

		private LayerPlaybackData _003Cl_003E5__4;

		private int _003Ctotal_003E5__5;

		private int _003CsaveEvery_003E5__6;

		object IEnumerator<object>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003CPrecomputeThenPlay_003Ed__177(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
		}

		private bool MoveNext()
		{
			switch (_003C_003E1__state)
			{
				default:
					return false;
				case 0:
				{
					_003C_003E1__state = -1;
					_003Cpr_003E5__2 = ProgressScreen.FindOrCreate();
					_003Cpr_003E5__2.Show();
					_003Cpr_003E5__2.Set(0f);
					_003Csw_003E5__3 = Stopwatch.StartNew();
					_003Cl_003E5__4 = _003C_003E4__this.plan.layers[evLayer];
					_003C_003E4__this.dyn = new RodDynamicsSolver(_003C_003E4__this.plan.projectile.coreType, _003C_003E4__this.plan.diameterMM, _003C_003E4__this.plan.projectile.rodLengthMM);
					float betaDeg = Vector3.Angle(-_003Cl_003E5__4.direction, _003Cl_003E5__4.surfaceNormal);
					if (_003Cl_003E5__4.eventKind == LayerEventKind.Penetration || _003Cl_003E5__4.eventKind == LayerEventKind.Stopped)
					{
						TateModel.Target target = ((_003Cl_003E5__4.plate != null) ? _003Cl_003E5__4.plate.GetTarget() : TateModel.RHA());
						_003Ctotal_003E5__5 = _003C_003E4__this.dyn.ConfigureSteps(_003Cl_003E5__4.outcome.steps, target, betaDeg, _003Cl_003E5__4.entryVelocity);
					}
					else
					{
						float vOut = ((_003Cl_003E5__4.entryVelocity > 1f) ? _003Cl_003E5__4.exitVelocity : (_003Cl_003E5__4.entryVelocity * 0.7f));
						float erodeFrac = Mathf.Clamp01((_003Cl_003E5__4.entryLengthM - _003Cl_003E5__4.exitLengthM) / Mathf.Max(_003C_003E4__this.plan.L0M, 1E-06f));
						if (erodeFrac < 0.05f)
						{
							erodeFrac = ((_003Cl_003E5__4.eventKind == LayerEventKind.Slide) ? 0.3f : 0.15f);
						}
						TateModel.Target target = ((_003Cl_003E5__4.plate != null) ? _003Cl_003E5__4.plate.GetTarget() : TateModel.RHA());
						_003Ctotal_003E5__5 = _003C_003E4__this.dyn.ConfigureSynthetic(betaDeg, _003Cl_003E5__4.entryVelocity, vOut, erodeFrac, target, _003Cl_003E5__4.eventKind != LayerEventKind.Slide);
					}
					_003CsaveEvery_003E5__6 = _003Ctotal_003E5__5 / 120;
					if (_003CsaveEvery_003E5__6 < 1)
					{
						_003CsaveEvery_003E5__6 = 1;
					}
					break;
				}
				case 1:
					_003C_003E1__state = -1;
					break;
			}
			while (_003C_003E4__this.dyn.doneSteps < _003Ctotal_003E5__5 && !_003C_003E4__this.dyn.exploded)
			{
				_003C_003E4__this.dyn.RunBatch(2000, _003CsaveEvery_003E5__6);
				_003Cpr_003E5__2.Set(_003C_003E4__this.dyn.Progress);
				_003C_003E2__current = null;
				_003C_003E1__state = 1;
				return true;
			}
			_003Csw_003E5__3.Stop();
			_003Cpr_003E5__2.Hide();
			_003C_003E4__this.dynLayerIndex = evLayer;
			_003C_003E4__this.dynCenters = new Vector3[_003C_003E4__this.dyn.RingCount];
			_003C_003E4__this.dynAlive = new bool[_003C_003E4__this.dyn.RingCount];
			_003C_003E4__this.dynDrop = new float[_003C_003E4__this.dyn.RingCount];
			_003C_003E4__this.BindDynFrame(_003Cl_003E5__4);
			if (_003C_003E4__this.hybrid == null)
			{
				_003C_003E4__this.hybrid = new HybridRodRenderer();
			}
			Debug.Log(string.Format("[Динамика 3E-2] предрасчёт слоя {0} ({1}): {2} суб-шагов, ", evLayer, _003Cl_003E5__4.eventKind, _003Ctotal_003E5__5) + string.Format("{0} кадров, {1} обломков, {2} разрывов волокон | ", _003C_003E4__this.dyn.frames.Count, _003C_003E4__this.dyn.chunkEvents.Count, _003C_003E4__this.dyn.fiberBreaks) + string.Format("{0:F0} мс{1}", _003Csw_003E5__3.Elapsed.TotalMilliseconds, _003C_003E4__this.dyn.exploded ? " ⚠ РАЗНОС (складывание >150°, самоконтакт паркован)" : ""));
			_003C_003E4__this.playing = true;
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}
	}

	public static PenetrationPlayer Instance;

	[Header("Показ")]
	[Range(1f, 60f)]
	public float playbackDuration;

	[Header("Рикошет/скольжение")]
	public float ricochetContactSeconds;

	[Header("Изгиб-поза (кинематика от событий ядра; PBD закрыт стендами 2A–2D)")]
	public float slideBendMaxDeg;

	public float ricochetBendCoeff;

	public float ricochetBendMinDeg;

	[Header("Разрушение (этап 3)")]
	public int brittleSplitPieces;

	private float lastChipLenM;

	private float bendPermDeg;

	private Vector3 bendDirWorld;

	private bool bendAlignNose;

	private Vector3 deformDirOverride;

	private bool deformDirOverrideActive;

	[Header("Раскол обломков вдоль (§5)")]
	public int debrisSlivers;

	public float debrisSliverSpread;

	[Header("Отладка канала струи (§2.5)")]
	public int jetDebugEveryNElements;

	public float minLayerSeconds;

	public const float VisualDepthScale = 999.99994f;

	private static readonly Color SlideColor = ResultPalette.Slide;

	public ShotPlan plan;

	public List<PlaybackSegment> segments;

	public int segIndex;

	private float segElapsed;

	private bool playing;

	private Vector3 baseScale;

	private Quaternion baseRotation;

	private int scaleAxis;

	private float baseVisualLen;

	private int sampleCursor;

	private Vector3 currentDir;

	private int spallLayerIndex;

	private int spallTotalN;

	private int spallSpawned;

	private Vector3 spallAxis;

	private float spallRefSpeed;

	private float spallRadiusM;

	private float finalDepthMM;

	private float holeRadiusWorld;

	private float fullThickWorld;

	private bool holeIsCeramic;

	private bool holeAllowPetaling;

	private bool holeExtraThin;

	private float holeRaggedAmp;

	private bool spallIgnoreSource;

	private GameObject mushroomHead;

	private Renderer mushroomRenderer;

	private float mushroomRampMax;

	private MeshRenderer projectileMeshRenderer;

	private float rodCrossWorld;

	private Color coreColor;

	private Projectile previewProjectile;

	private bool lodgedInArmour;

	private Vector3 lodgedNosePos;

	private Vector3 lodgedDir;

	private RodFins fins;

	private bool finsLogged;

	[Header("Оперение (шаг 21 §2)")]
	[Range(0f, 1f)]
	public float finStripAxialFraction;

	private ProjectileDeformation deform;

	private ProjectileBodyRenderer bodyModel;

	private bool advanced;

	private RodDynamicsSolver dyn;

	private HybridRodRenderer hybrid;

	private int dynLayerIndex;

	private Vector3 dynAnchor;

	private Quaternion dynRot;

	private readonly HashSet<int> spawnedChunks;

	private Vector3[] dynCenters;

	private bool[] dynAlive;

	private Vector3 dynAnchorLive;

	private bool dynAnchorLiveValid;

	public const float WorldPerMeter = 1f;

	public const float WorldPerMeterLateral = 1f;

	private float lastTimeDilation;

	private float[] dynDrop;

	private JetRenderer jetRenderer;

	private JetRenderer precursorRenderer;

	private bool jetDetonated;

	private float detonationDistanceWU;

	private float fuzeContactDistanceWU;

	private float jetSincePhysS;

	private List<JetLayer> jetLayers;

	private List<EraTrigger> eraTriggers;

	private JetShotState.Stage[] jetStages;

	private int finMaxLayer;

	private int spallReportedLayer;

	private float spallPrevDepthMM;

	private int spallLedgerCreated;

	private int spallLedgerAsked;

	private float spallLedgerBirthRmm;

	private float spallLedgerChannelRmm;

	private float spallLedgerCarvedMM;

	private float spallLedgerNoseMM;

	private float[] dynRestCache;

	public const float WorldPerMeterHybrid = 1f;

	public float CurrentRodSpeedMps { get; private set; }

	public float CurrentModelRodLengthM { get; private set; }

	public Renderer DrawnRodRenderer
	{
		get
		{
			Renderer r = ((hybrid != null) ? hybrid.DrawnRenderer : null);
			if (r == null && deform != null)
			{
				r = deform.DrawnRenderer;
			}
			if (r != null && bodyModel != null)
			{
				r = bodyModel.DrawnRenderer;
			}
			return r;
		}
	}

	public float CurrentTimeDilation
	{
		get
		{
			if (segments != null && segIndex >= 0 && segIndex < segments.Count)
			{
				PlaybackSegment seg = segments[segIndex];
				if (seg.naturalDuration > 1E-12f)
				{
					lastTimeDilation = Mathf.Max(seg.duration / seg.naturalDuration, 1E-06f);
				}
			}
			return lastTimeDilation;
		}
	}

	public float CurrentRodVisualSpeed => CurrentRodSpeedMps / CurrentTimeDilation;

	private float MainJetTimeS => jetSincePhysS - ((plan != null) ? plan.MainChargeDelayS : 0f);

	private bool HasFins => plan != null && !plan.isShapedCharge && !plan.projectile.GetDefinition().noFins;

	private void EnsureBodyModel()
	{
		if (bodyModel == null)
		{
			bodyModel = new ProjectileBodyRenderer();
		}
	}

	public bool TryGetImpactPoint(out Vector3 point)
	{
		point = Vector3.zero;
		if (plan == null || plan.layers == null || plan.layers.Count == 0)
		{
			return false;
		}
		int i = 0;
		if (segments != null && segIndex >= 0 && segIndex < segments.Count && segments[segIndex].layerIndex >= 0)
		{
			i = segments[segIndex].layerIndex;
		}
		i = Mathf.Clamp(i, 0, plan.layers.Count - 1);
		point = plan.layers[i].entryPoint;
		return true;
	}

	private float SegVisualSpeedOut(int i)
	{
		return PlaybackTrack.SpeedOut(plan, segments, i, baseVisualLen);
	}

	private float SegVisualSpeedIn(int i)
	{
		return PlaybackTrack.SpeedIn(plan, segments, i, baseVisualLen);
	}

	private static float EaseFlight(float k, float m0, float m1)
	{
		float mag = m0 * m0 + m1 * m1;
		if (mag > 9f)
		{
			float scale = 3f / Mathf.Sqrt(mag);
			m0 *= scale;
			m1 *= scale;
		}
		float k2 = k * k;
		float k3 = k2 * k;
		return (k3 - 2f * k2 + k) * m0 + k2 * (3f - 2f * k) + (k3 - k2) * m1;
	}

	private void Awake()
	{
		Instance = this;
	}

	public static PenetrationPlayer FindOrCreate()
	{
		if (Instance != null)
		{
			return Instance;
		}
		PenetrationPlayer player = UnityEngine.Object.FindFirstObjectByType<PenetrationPlayer>();
		if (player == null)
		{
			player = new GameObject("PenetrationPlayer").AddComponent<PenetrationPlayer>();
			Debug.Log("PenetrationPlayer: объекта не было в сцене — создан автоматически.");
		}
		Instance = player;
		return player;
	}

	public void StopPlayback()
	{
		playing = false;
		ReleaseLodged();
		if (RenderedSpeedAudit.Instance != null)
		{
			RenderedSpeedAudit.Instance.ResetShot();
		}
		plan = null;
		segments = null;
		StopAllCoroutines();
		if (ProgressScreen.Instance != null)
		{
			ProgressScreen.Instance.Hide();
		}
		if (hybrid != null)
		{
			hybrid.SetActive(false);
		}
		dyn = null;
		dynRestCache = null;
		dynLayerIndex = -1;
		if (spawnedChunks != null)
		{
			spawnedChunks.Clear();
		}
		jetLayers = null;
		eraTriggers = null;
		jetStages = null;
		if (jetRenderer != null)
		{
			jetRenderer.SetVisible(false);
		}
		if (precursorRenderer != null)
		{
			precursorRenderer.SetVisible(false);
		}
	}

	private void EnsureJetRenderer()
	{
		if (jetRenderer != null)
		{
			return;
		}
		jetRenderer = new GameObject("JetRenderer").AddComponent<JetRenderer>();
	}

	private void EnsurePrecursorRenderer()
	{
		if (precursorRenderer != null)
		{
			return;
		}
		precursorRenderer = new GameObject("JetRenderer_Precursor").AddComponent<JetRenderer>();
	}

	private void DetonateWarhead()
	{
		if (jetDetonated || plan == null || !plan.isShapedCharge)
		{
			return;
		}
		jetDetonated = true;
		ProjectileDefinition def = plan.projectile.GetDefinition();
		WarheadDetonation warhead = plan.projectile.GetComponent<WarheadDetonation>();
		if (warhead == null)
		{
			warhead = plan.projectile.gameObject.AddComponent<WarheadDetonation>();
		}
		Transform body = plan.projectile.transform;
		warhead.DetonateFrom(body, plan.direction, plan.projectile.combatVelocity, def.diameterMM, def.rodLengthMM, 1f, CurrentTimeDilation, Projectile.CoreColor(plan.projectile.coreType));
		string casing;
		if (warhead.spawnCasingPieces)
		{
			casing = string.Format("корпус — {0} кусков, вперёд {1:F0} м/с ", warhead.casingPieces, plan.projectile.combatVelocity) + string.Format("+ радиально {0:F0} м/с (визуальный placeholder)", warhead.casingRadialSpeedMS);
		}
		else
		{
			casing = "корпус — тело погашено, кусков НЕТ (spawnCasingPieces выключен)";
		}
		Debug.Log(string.Format("◈ ПОДРЫВ на {0:F0} мм от брони: {1}; ", def.fuzeStandoffMM, casing) + string.Format("струя уходит на {0:F0} м/с", ShapedChargeCore.DemoPlaceholder().vTip));
	}

	private void EnsureJetLayers()
	{
		if (jetLayers != null || plan == null || !plan.isShapedCharge)
		{
			return;
		}
		jetLayers = new List<JetLayer>();
		eraTriggers = new List<EraTrigger>();
		float jetTipVel = plan.jetTipVelocityMS;
		if (jetTipVel <= 0f)
		{
			if (plan.jetTimeline != null && plan.jetTimeline.Length != 0)
			{
				jetTipVel = plan.jetTimeline[0].velocity;
			}
			else
			{
				jetTipVel = 0f;
			}
		}
		if (jetRenderer != null)
		{
			jetRenderer.ClearDispersion();
		}
		float dispersionScale = 1f;
		float dispersionTargetM = -1f;
		float prevExitRadiusM = -1f;
		for (int i = 0; i < plan.layers.Count; i++)
		{
			LayerPlaybackData l = plan.layers[i];
			if (l.plate == null)
			{
				continue;
			}
			if (l.plate.IsEraPlate || l.plate.IsNeraPlate)
			{
				if (jetTipVel > 0f)
				{
					float arrivalS;
					if (plan.precursor != null && plan.precursorArmedPlates.Contains(l.plate) && plan.precursor.tipVelocityMS > 0f)
					{
						arrivalS = Vector3.Distance(plan.precursor.detonationPoint, l.entryPoint) / plan.precursor.tipVelocityMS;
					}
					else
					{
						arrivalS = Vector3.Distance(plan.detonationPoint, l.entryPoint) / jetTipVel + plan.MainChargeDelayS;
					}
					EraTrigger trigger = new EraTrigger();
					trigger.arrivalS = arrivalS;
					trigger.layerIndex = i;
					trigger.elementsBefore = l.jetElementsBefore;
					trigger.elementsRetired = l.jetElementsRetired;
					eraTriggers.Add(trigger);
					continue;
				}
			}
			if (l.jetTimeline == null)
			{
				continue;
			}
			JetChannel channel = new JetChannel(l.jetTimeline, l.tEffMM / 1000f);
			if (!channel.Valid)
			{
				continue;
			}
			float flareDeg = CraterModel.JetChannelFlareHalfAngleDeg(l.plate.armorType);
			if (flareDeg > 0f)
			{
				float entryRadiusM = ((prevExitRadiusM >= 0f) ? prevExitRadiusM : channel.EntryRadiusM);
				float exitRadiusM = channel.ExitRadiusM;
				channel.ReprofileComposite(entryRadiusM, flareDeg * Mathf.Deg2Rad, 2f);
				Debug.Log(string.Concat(
					string.Format("◈ СЛОИСТЫЙ (слой {0}, {1}, {2:F0} мм по LOS): ", i, l.plate.armorType, l.tEffMM),
					string.Format("канал раструбом, показатель p = {0:F1} ", 2f),
					string.Format("(у входа наклон ноль, к тылу разгоняется до {0:F0}°, ASSUMED) — ", flareDeg),
					string.Format("вход ⌀{0:F1} мм ", entryRadiusM * 2000f),
					"(",
					(prevExitRadiusM >= 0f) ? "стык с выходом предыдущей преграды" : "первая преграда — своя струя",
					"), ",
					string.Format("середина ⌀{0:F1} мм, ", channel.RadiusAtDepthM(channel.FinalDepthM * 0.5f) * 2000f),
					string.Format("выход ⌀{0:F1} мм — нигде не сужается. ", channel.ExitRadiusM * 2000f),
					string.Format("Профиль от струи заменён целиком (он давал выход ⌀{0:F1} мм).", exitRadiusM * 2000f)));
				dispersionScale = Mathf.Min(dispersionScale * 1.175f, 2f);
				dispersionTargetM = channel.ExitRadiusM * dispersionScale;
				float standoffM = Vector3.Distance(plan.detonationPoint, l.entryPoint);
				if (jetRenderer != null)
				{
					jetRenderer.SetDispersion(l.tEffMM / 1000f + standoffM, l.tEffMM / 1000f, dispersionTargetM);
				}
			}
			else if (dispersionTargetM > 0f)
			{
				float ownEntryM = channel.EntryRadiusM;
				float ownExitM = channel.ExitRadiusM;
				channel.FlattenTo(dispersionTargetM);
				Debug.Log(string.Concat(
					string.Format("◈ ЗА СЛОИСТЫМ (слой {0}, {1}): бьёт рассеянный пучок — ", i, l.plate.armorType),
					string.Format("дыра РОВНАЯ ⌀{0:F1} мм по всей толщине ", channel.ExitRadiusM * 2000f),
					string.Format("(выход пакета ×{0:F3}, сила рассеяния +{1:F1} %). ", dispersionScale, (dispersionScale - 1f) * 100f),
					string.Format("Своя струя дала бы вход ⌀{0:F1} → выход ⌀{1:F1} мм ", ownEntryM * 2000f, ownExitM * 2000f),
					"— то есть уступ внутрь на стыке."));
			}
			prevExitRadiusM = channel.ExitRadiusM;
			JetLayer jl = new JetLayer();
			jl.layerIndex = i;
			jl.standoffM = Vector3.Distance(plan.detonationPoint, l.entryPoint);
			jl.elementsBefore = l.jetElementsBefore;
			jl.elementsRetired = l.jetElementsRetired;
			jl.channel = channel;
			jl.profile = channel.ToWorldProfile(WorldPerMeter, WorldPerMeterLateral);
			jl.mesh = l.plate.GetComponent<DeformableMesh>();
			jl.fullThickWU = l.tEffMM / VisualDepthScale;
			jl.budget = ((FragmentSpawner.Instance != null) ? FragmentSpawner.Instance.SpallTotalCountForVolume(l.plate, channel.DisplacedVolumeM3, fromJet: true) : 0);
			jl.spallRadiusM = channel.RadiusAtDepthM(channel.FinalDepthM * 0.5f);
			jl.ignoreSource = l.eventKind == LayerEventKind.Penetration;
			jl.exitBulge = ((l.eventKind == LayerEventKind.Penetration) ? CraterModel.JetExitBulge(l.jetDepthMargin) : 0f);
			if (l.eventKind == LayerEventKind.Penetration)
			{
				Vector3 axis = l.exitDirection * 0.3f - l.surfaceNormal * 0.7f;
				float mag = axis.magnitude;
				if (mag > 1E-05f)
				{
					jl.spallAxis = axis / mag;
				}
				else
				{
					jl.spallAxis = Vector3.zero;
				}
				jl.spallSpeed = l.outcome.residualVelocity;
			}
			else
			{
				jl.spallAxis = l.surfaceNormal;
				jl.spallSpeed = l.entryVelocity * 0.1f;
			}
			jetLayers.Add(jl);
			Debug.Log(string.Format("◈ КАНАЛ слой {0} [{1} {2:F0} мм]: ", i + 1, l.plate.GetTarget().name, l.plate.thicknessMM) + channel.ProfileSummary(plan.coneDiameterM));
			Debug.Log(string.Format("    осколки по вытесненному объёму: N = {0} ", jl.budget) + "(модель кратера — Szendrei-Held; расширение полости Luk & Piekutowski и лепестки Вирцбицки к струе НЕ применяются)");
			string bulgeVerdict = ((jl.exitBulge <= 0.01f) ? "чистое отверстие — прошла с избытком" : ((jl.exitBulge >= 0.99f) ? "пробитие на грани — выпучивание и толстый откол" : "промежуточное"));
			Debug.Log(string.Format("    тыл: запас по глубине ×{0:F2} ⇒ выпучивание {1:F2} ", l.jetDepthMargin, jl.exitBulge) + "(" + bulgeVerdict + ")");
		}
	}

	private void UpdateJetChannel()
	{
		EnsureJetLayers();
		if (jetLayers == null)
		{
			return;
		}
		float tS = MainJetTimeS;
		if (eraTriggers != null)
		{
			for (int i = 0; i < eraTriggers.Count; i++)
			{
				EraTrigger et = eraTriggers[i];
				if (!et.fired && et.arrivalS <= jetSincePhysS)
				{
					et.fired = true;
					LayerPlaybackData l = plan.layers[et.layerIndex];
					if (l.plate != null && l.plate.IsNeraPlate)
					{
						NeraBlock.Fire(l.plate, l.direction, WorldPerMeter, CurrentTimeDilation, l.entryPoint, NeraHoleRadiusWorld());
					}
					else
					{
						EraCassette.Detonate(l.plate, l.direction, WorldPerMeter, CurrentTimeDilation, plan.precursorArmedPlates.Contains(l.plate), l.entryPoint);
					}
				}
			}
		}
		if (jetStages == null)
		{
			jetStages = BuildJetStages();
		}
		JetShotState.At(jetStages, tS, out var retiredFromTip, out var headLimitM);
		for (int j = 0; j < jetLayers.Count; j++)
		{
			JetLayer jl = jetLayers[j];
			JetChannel channel = jl.channel;
			int active = channel.ActiveIndex(tS);
			if (active < 0 || (jl.stopped && active >= channel.LastIndex))
			{
				continue;
			}
			JetElement[] timeline = channel.Timeline;
			float depthM = ((channel.FinalDepthM > timeline[active].depthAfter) ? timeline[active].depthAfter : channel.FinalDepthM);
			bool jetThrough = false;
			if (jl.layerIndex >= 0 && plan.layers[jl.layerIndex].eventKind == LayerEventKind.Penetration)
			{
				jetThrough = active >= channel.LastIndex;
			}
			if (jl.mesh != null)
			{
				LayerPlaybackData l = plan.layers[jl.layerIndex];
				bool updateCollider = jetThrough && !jl.through;
				jl.mesh.UpdateJetChannel(l.entryPoint, l.direction, jl.profile, depthM, jl.fullThickWU, jetThrough, updateCollider, jl.exitBulge);
			}
			SpawnJetSpall(jl, depthM);
			if (jetThrough && !jl.through)
			{
				jl.through = true;
				ArmorPlate plate = plan.layers[jl.layerIndex].plate;
				if (plate != null)
				{
					plate.SetDisplayColor(ResultPalette.Perforated);
				}
			}
			if (active >= channel.LastIndex && !jl.stopped)
			{
				jl.stopped = true;
				if (!jetThrough)
				{
					ArmorPlate plate2 = plan.layers[jl.layerIndex].plate;
					if (plate2 != null)
					{
						plate2.SetDisplayColor(ResultPalette.Stopped);
					}
				}
			}
			if (active != jl.loggedIndex)
			{
				int every = ((jetDebugEveryNElements < 1) ? 1 : jetDebugEveryNElements);
				if (active % every == 0 || active >= channel.LastIndex)
				{
					jl.loggedIndex = active;
					Debug.Log(string.Format("    слой {0} | {1} ", jl.layerIndex + 1, channel.DebugLine(tS, plan.coneDiameterM)) + string.Format("| осколков {0}/{1}", jl.spawned, jl.budget));
				}
			}
		}
		if (jetRenderer != null)
		{
			if (retiredFromTip > 0)
			{
				jetRenderer.RetireFromTip(retiredFromTip);
			}
			jetRenderer.headLimitM = headLimitM;
		}
	}

	private JetShotState.Stage[] BuildJetStages()
	{
		int jetCount = ((jetLayers != null) ? jetLayers.Count : 0) + ((eraTriggers != null) ? eraTriggers.Count : 0);
		JetShotState.Stage[] stages = new JetShotState.Stage[jetCount];
		float jetTipVel = plan.jetTipVelocityMS;
		int n = 0;
		if (eraTriggers != null)
		{
			for (int i = 0; i < eraTriggers.Count; i++)
			{
				EraTrigger et = eraTriggers[i];
				LayerPlaybackData l = plan.layers[et.layerIndex];
				float standoffM = Vector3.Distance(plan.detonationPoint, l.entryPoint);
				stages[n] = new JetShotState.Stage
				{
					standoffM = standoffM,
					elementsBefore = et.elementsBefore,
					elementsRetired = et.elementsRetired,
					channel = null,
					arrivalS = ((jetTipVel > 1E-06f) ? (standoffM / jetTipVel) : 0f)
				};
				n++;
			}
		}
		if (jetLayers != null)
		{
			for (int j = 0; j < jetLayers.Count; j++)
			{
				JetLayer jl = jetLayers[j];
				stages[n] = new JetShotState.Stage
				{
					standoffM = jl.standoffM,
					elementsBefore = jl.elementsBefore,
					elementsRetired = jl.elementsRetired,
					channel = jl.channel,
					stopsHere = plan.layers[jl.layerIndex].eventKind == LayerEventKind.Stopped
				};
				n++;
			}
		}
		return stages;
	}

	private void UpdatePrecursorJet()
	{
		if (plan == null || plan.precursor == null || precursorRenderer == null)
		{
			return;
		}
		JetElement[] timeline = plan.precursor.timeline;
		if (timeline == null || timeline.Length == 0)
		{
			return;
		}
		precursorRenderer.headLimitM = plan.precursor.reachM;
		int retired = 0;
		for (int i = 0; i < timeline.Length; i++)
		{
			if (plan.precursor.reachM > timeline[i].velocity * jetSincePhysS)
			{
				break;
			}
			retired++;
		}
		if (retired > 0)
		{
			precursorRenderer.RetireFromTip(retired);
		}
		precursorRenderer.UpdateAt(jetSincePhysS);
	}

	private void FinalizeJetChannels()
	{
		if (jetLayers == null)
		{
			return;
		}
		for (int i = 0; i < jetLayers.Count; i++)
		{
			JetLayer jl = jetLayers[i];
			if (jl.stopped)
			{
				continue;
			}
			JetChannel channel = jl.channel;
			LayerPlaybackData l = plan.layers[jl.layerIndex];
			bool through = l.eventKind == LayerEventKind.Penetration;
			if (jl.mesh != null)
			{
				jl.mesh.UpdateJetChannel(l.entryPoint, l.direction, jl.profile, channel.FinalDepthM, jl.fullThickWU, through, updateCollider: true, jl.exitBulge);
			}
			SpawnJetSpall(jl, channel.FinalDepthM);
			ArmorPlate plate = l.plate;
			if (plate != null)
			{
				plate.SetDisplayColor(through ? ResultPalette.Perforated : ResultPalette.Stopped);
			}
			jl.through = through;
			jl.stopped = true;
			Debug.Log(string.Format("    слой {0} | {1} ", jl.layerIndex + 1, channel.DebugLine(float.MaxValue, plan.coneDiameterM)) + string.Format("| осколков {0}/{1}", jl.spawned, jl.budget));
		}
	}

	private float NeraHoleRadiusWorld()
	{
		if (plan != null)
		{
			if (!plan.isShapedCharge)
			{
				return plan.diameterMM / 1000f * 0.5f;
			}
			if (plan.jetTimeline != null && plan.jetTimeline.Length != 0)
			{
				return plan.jetTimeline[0].craterRadiusM;
			}
		}
		return 0f;
	}

	private bool IsJetLayer(int layerIndex)
	{
		return FindJetLayer(layerIndex) != null;
	}

	private JetLayer FindJetLayer(int layerIndex)
	{
		if (jetLayers == null)
		{
			return null;
		}
		for (int i = 0; i < jetLayers.Count; i++)
		{
			if (jetLayers[i].layerIndex == layerIndex)
			{
				return jetLayers[i];
			}
		}
		return null;
	}

	private void SpawnJetSpall(JetLayer jl, float depthM)
	{
		if (FragmentSpawner.Instance == null)
		{
			return;
		}
		if (jl.budget <= 0)
		{
			return;
		}
		JetChannel channel = jl.channel;
		float volume = channel.DisplacedVolumeM3;
		if (volume <= 1E-12f)
		{
			volume = 1E-12f;
		}
		float fraction = channel.DisplacedVolumeToDepthM3(depthM) / volume;
		if (fraction < 0f)
		{
			fraction = 0f;
		}
		else if (fraction > 1f)
		{
			fraction = 1f;
		}
		int count = (int)Math.Round(jl.budget * fraction) - jl.spawned;
		if (count <= 0)
		{
			return;
		}
		LayerPlaybackData l = plan.layers[jl.layerIndex];
		float prevDepthMM = jl.spallPrevDepthM * 1000f;
		float deltaDepthMM = depthM * 1000f - prevDepthMM;
		for (int i = 0; i < count; i++)
		{
			float u = (i + 0.5f) / count;
			if (u < 0f)
			{
				u = 0f;
			}
			else if (u > 1f)
			{
				u = 1f;
			}
			float depthMM = u * deltaDepthMM + prevDepthMM;
			float radiusM = channel.RadiusAtDepthM(depthMM / 1000f);
			if (radiusM <= 0.0001f)
			{
				radiusM = 0.0001f;
			}
			float mag = l.direction.magnitude;
			Vector3 dirN = ((mag > 1E-05f) ? (l.direction / mag) : Vector3.zero);
			Vector3 side = Vector3.Cross(dirN, Vector3.forward);
			if (side.sqrMagnitude < 1E-06f)
			{
				side = Vector3.Cross(dirN, Vector3.down);
			}
			mag = side.magnitude;
			if (mag > 1E-05f)
			{
				side /= mag;
			}
			else
			{
				side = Vector3.zero;
			}
			Vector3 up = Vector3.Cross(dirN, side);
			mag = up.magnitude;
			if (mag > 1E-05f)
			{
				up /= mag;
			}
			else
			{
				up = Vector3.zero;
			}
			float angle = UnityEngine.Random.value * 2f * Mathf.PI;
			Vector3 dir = Mathf.Cos(angle) * up + Mathf.Sin(angle) * side;
			mag = dir.magnitude;
			if (mag > 1E-05f)
			{
				dir /= mag;
			}
			else
			{
				dir = Vector3.zero;
			}
			Vector3 axisPoint = l.entryPoint + dirN * (depthMM / VisualDepthScale);
			Vector3 origin = axisPoint + dir * radiusM;
			Vector3 velocityDir;
			if (!jl.ignoreSource)
			{
				Vector3 diff = dir - dirN;
				float vmag = diff.magnitude;
				velocityDir = ((vmag > 1E-05f) ? (diff / vmag) : Vector3.zero);
			}
			else
			{
				velocityDir = jl.spallAxis;
			}
			FragmentSpawner.Instance.SpawnSpallPortion(origin, velocityDir, jl.spallSpeed, l.plate, radiusM, 1, FragmentSpawner.Instance.coneHalfAngleDeg, jl.ignoreSource, axisPoint, dirN, depthMM / VisualDepthScale);
			float dilation = CurrentTimeDilation;
			if (dilation <= 1E-06f)
			{
				dilation = 1E-06f;
			}
			MicroSpall.Burst(axisPoint, dirN, jl.spallSpeed / dilation, jl.spallSpeed, l.plate, radiusM, 1, ResultPalette.Perforated);
		}
		jl.spawned += count;
		jl.spallPrevDepthM = depthM;
	}

	public void Play(ShotPlan newPlan)
	{
		plan = newPlan;
		if (plan == null || plan.projectile == null)
		{
			return;
		}
		previewProjectile = plan.projectile;
		plan.projectile.ClearTrace();
		if (ResultDisplay.Instance != null)
		{
			ResultDisplay.Instance.Hide();
		}
		ArmorPlate[] plates = UnityEngine.Object.FindObjectsByType<ArmorPlate>(FindObjectsSortMode.None);
		int resetCount = 0;
		foreach (ArmorPlate plate in plates)
		{
			DeformableMesh dm = plate.GetComponent<DeformableMesh>();
			if (dm != null)
			{
				dm.ResetDeformation();
				resetCount++;
			}
		}
		Debug.Log(string.Format("◇ Новый показ: сброшено повреждений на {0} плитах", resetCount));
		plan.projectile.ResetTransformToStart();
		CaptureProjectileGeometry();
		BuildSegments();
		advanced = plan.projectile.quality == Projectile.QualityMode.Продвинутое;
		dynLayerIndex = -1;
		spawnedChunks.Clear();
		bool started = false;
		if (advanced && plan.layers != null && !plan.isShapedCharge)
		{
			for (int i = 0; i < plan.layers.Count; i++)
			{
				LayerEventKind kind = plan.layers[i].eventKind;
				if (kind == LayerEventKind.Penetration || (kind >= LayerEventKind.Stopped && kind != LayerEventKind.CriticalRicochet) || kind == LayerEventKind.CriticalRicochet)
				{
					StartCoroutine(PrecomputeThenPlay(i));
					started = true;
					break;
				}
			}
		}
		segIndex = 0;
		sampleCursor = 0;
		spallLayerIndex = -1;
		playing = !started;
		jetDetonated = false;
		jetSincePhysS = 0f;
		jetLayers = null;
		eraTriggers = null;
		jetStages = null;
		WarheadDetonation.ClearAll();
		EraCassette.ClearAll();
		NeraBlock.ClearAll();
		EnsureJetRenderer();
		EnsurePrecursorRenderer();
		if (plan.isShapedCharge && plan.jetTimeline != null)
		{
			jetRenderer.Configure(plan.jetTimeline, plan.detonationPoint, plan.direction, plan.breakupTimeS, plan.elementDvMS, 1f);
			detonationDistanceWU = fuzeContactDistanceWU;
		}
		else
		{
			jetRenderer.SetVisible(false);
		}
		if (plan.isShapedCharge && plan.precursor != null && plan.precursor.timeline != null)
		{
			precursorRenderer.Configure(plan.precursor.timeline, plan.precursor.detonationPoint, plan.direction, plan.precursor.breakupTimeS, plan.precursor.elementDvMS, 1f);
			float headR = ((plan.precursor.timeline.Length != 0) ? plan.precursor.timeline[0].radiusM : 0f);
			float lifeS = ((plan.precursor.tipVelocityMS > 1E-06f) ? (plan.precursor.reachM / plan.precursor.tipVelocityMS) : 0f);
			Debug.Log(string.Concat(
				string.Format("◈ ВТОРАЯ СТРУЯ (предзаряд) НАСТРОЕНА: элементов {0}, ", plan.precursor.timeline.Length),
				string.Format("CD {0:F0} мм, радиус головного {1:F2} мм ", plan.precursor.coneDiameterM * 1000f, headR * 1000f),
				string.Format("(на экране {0:F4} мир.ед в поперечнике), ", headR * 2f),
				string.Format("путь до преграды {0:F0} мм ⇒ живёт {1:F1} мкс ", plan.precursor.reachM * 1000f, lifeS * 1000000f),
				"по часам струи. Если её не видно — смотри именно эти два числа: толщину и время жизни, а не наличие."));
		}
		else
		{
			precursorRenderer.SetVisible(false);
			if (plan.isShapedCharge && plan.projectile.GetDefinition().isTandem)
			{
				Debug.LogWarning("◈ ТАНДЕМ: второй струи в плане НЕТ. Причина — выше по логу: либо предзаряд не строился (подрыв на решётке), либо на трассе не нашлось преграды, от которой считается его стандоф.");
			}
		}
		mushroomRampMax = 0f;
		EnsureMushroomHead();
		mushroomHead.SetActive(false);
		if (deform == null)
		{
			deform = new ProjectileDeformation();
		}
		deform.coreType = plan.projectile.coreType;
		deform.noseShape = plan.projectile.GetDefinition().nose;
		deform.Configure(plan.diameterMM, plan.L0M, plan.projectile.GetCorePenetrator(), coreColor, 1f);
		deform.Precompute(plan.layers);
		deform.SetActive(false);
		dynAnchorLiveValid = false;
		bendPermDeg = 0f;
		bendDirWorld = Vector3.up;
		bendAlignNose = false;
		deformDirOverrideActive = false;
		deform.SetBendPose(0f, Vector3.up, false);
		deform.raggedTail = false;
		if (fins == null)
		{
			fins = new RodFins();
		}
		finMaxLayer = -1;
		finsLogged = false;
		ProjectileDefinition def = plan.projectile.GetDefinition();
		fins.Configure(plan.diameterMM / 1000f, plan.L0M, 1f, coreColor, def.finBlades, plan.direction, def.gunCalibreMM / 1000f);
		fins.SetMaterial(MaterialLibrary.Core(plan.projectile.coreType));
		fins.SetActive(HasFins);
		if (projectileMeshRenderer != null)
		{
			projectileMeshRenderer.enabled = false;
		}
		if (segments.Count == 0)
		{
			Finish();
		}
	}

	private void Update()
	{
		if (!playing)
		{
			if (!lodgedInArmour)
			{
				UpdatePreview();
			}
			else if (deform != null)
			{
				deform.SetActive(true);
				Transform parent = base.transform;
				deform.Render(parent, lodgedNosePos, lodgedDir);
			}
			else
			{
				lodgedInArmour = false;
			}
			return;
		}
		segElapsed += Time.deltaTime;
		bool flag = false;
		while (!flag)
		{
			PlaybackSegment seg = segments[segIndex];
			if (segElapsed < seg.duration)
			{
				AnimateSegment(seg, segElapsed);
				flag = true;
			}
			else
			{
				segElapsed -= seg.duration;
				CompleteSegment(seg);
				segIndex++;
				advanced = false;
				bendPermDeg = -1f;
				if (segIndex >= segments.Count)
				{
					Finish();
					flag = true;
				}
			}
		}
		UpdateMushroomHead();
		UpdateDeformBody();
		UpdateFins();
	}

	private void CaptureProjectileGeometry()
	{
		Transform tr = plan.projectile.transform;
		baseScale = tr.localScale;
		baseRotation = tr.rotation;
		currentDir = plan.direction;
		Vector3 localDir = tr.InverseTransformDirection(plan.direction);
		scaleAxis = 0;
		if (Mathf.Abs(localDir.y) > Mathf.Abs(localDir.x))
		{
			scaleAxis = 1;
		}
		if (Mathf.Abs(localDir.z) > Mathf.Abs(localDir[scaleAxis]))
		{
			scaleAxis = 2;
		}
		baseVisualLen = SceneScale.RodLengthWorld(plan.projectile.rodLengthMM);
		rodCrossWorld = Mathf.Max(SceneScale.RodDiameterWorld(plan.diameterMM), 0.0001f);
		coreColor = Projectile.CoreColor(plan.projectile.coreType);
		projectileMeshRenderer = plan.projectile.GetComponentInChildren<MeshRenderer>(includeInactive: true);
		if (baseVisualLen < 0.0001f)
		{
			baseVisualLen = SceneScale.RodLengthWorld(ProjectileLibrary.Default().rodLengthMM);
		}
	}

	private void UpdatePreview()
	{
		if (previewProjectile == null)
		{
			previewProjectile = UnityEngine.Object.FindFirstObjectByType<Projectile>();
		}
		if (previewProjectile == null)
		{
			return;
		}
		MeshRenderer mr = previewProjectile.GetComponentInChildren<MeshRenderer>(includeInactive: true);
		if (mr != null && mr.enabled)
		{
			mr.enabled = false;
		}
		float rodLenM = Mathf.Max(previewProjectile.rodLengthMM / 1000f, 0.0001f);
		baseVisualLen = Mathf.Max(SceneScale.RodLengthWorld(previewProjectile.rodLengthMM), 0.0001f);
		coreColor = Projectile.CoreColor(previewProjectile.coreType);
		Vector3 dir = previewProjectile.direction;
		if (dir.sqrMagnitude > 1E-06f)
		{
			dir = ((dir.magnitude > 1E-05f) ? (dir / dir.magnitude) : Vector3.zero);
		}
		else
		{
			dir = Vector3.forward;
		}
		Vector3 pos = previewProjectile.transform.position;
		Vector3 nosePos = pos + dir * (baseVisualLen * 0.5f);
		EnsureBodyModel();
		if (previewProjectile.GetDefinition().IsShapedCharge && ProjectileBodyRenderer.Available)
		{
			if (deform != null)
			{
				deform.SetActive(false);
			}
			bodyModel.SetModel(previewProjectile.GetDefinition().BodyModelFor(SectionFrame.Active));
			bodyModel.Configure(previewProjectile.diameter / 1000f, previewProjectile.GetDefinition().BodyLengthMMFor(SectionFrame.Active) / 1000f, previewProjectile.coreType, 1f);
			bodyModel.SetActive(true);
			bodyModel.Render(base.transform, nosePos, dir);
			EnsureFins();
			fins.SetActive(false);
		}
		else
		{
			bodyModel.SetActive(false);
			if (deform == null)
			{
				deform = new ProjectileDeformation();
			}
			deform.coreType = previewProjectile.coreType;
			deform.noseShape = previewProjectile.GetDefinition().nose;
			deform.Configure(previewProjectile.diameter, rodLenM, previewProjectile.GetCorePenetrator(), coreColor, 1f);
			deform.SetBendPose(0f, Vector3.up, false);
			deform.SetActive(true);
			deform.Render(base.transform, nosePos, dir);
			EnsureFins();
			bool finsOn = !previewProjectile.GetDefinition().IsShapedCharge && !previewProjectile.GetDefinition().noFins;
			fins.SetActive(finsOn);
			if (finsOn)
			{
				fins.Configure(previewProjectile.diameter / 1000f, rodLenM, 1f, coreColor, previewProjectile.GetDefinition().finBlades, dir, previewProjectile.GetDefinition().gunCalibreMM / 1000f);
				fins.SetMaterial(MaterialLibrary.Core(previewProjectile.coreType));
				fins.Render(nosePos - dir * baseVisualLen, dir);
			}
		}
		if (mushroomHead != null && mushroomHead.activeSelf)
		{
			mushroomHead.SetActive(false);
		}
	}

	private void SetRodDirection(Vector3 d)
	{
		if ((d - currentDir).sqrMagnitude < 1E-10f)
		{
			return;
		}
		Transform tr = plan.projectile.transform;
		tr.rotation = Quaternion.FromToRotation(plan.direction, d) * baseRotation;
		currentDir = d;
	}

	private void EnsureMushroomHead()
	{
		if (mushroomHead != null)
		{
			return;
		}
		mushroomHead = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		mushroomHead.name = "MushroomHead";
		mushroomHead.transform.SetParent(base.transform, worldPositionStays: false);
		Collider collider = mushroomHead.GetComponent<Collider>();
		if (collider != null)
		{
			UnityEngine.Object.Destroy(collider);
		}
		mushroomRenderer = mushroomHead.GetComponent<Renderer>();
	}

	private void UpdateMushroomHead()
	{
		if (plan == null || plan.projectile == null)
		{
			return;
		}
		if (mushroomHead != null && mushroomHead.activeSelf)
		{
			mushroomHead.SetActive(false);
		}
		Transform tr = plan.projectile.transform;
		float axisScale = Mathf.Abs(baseScale[scaleAxis]);
		float ratio;
		if (axisScale <= 1E-09f)
		{
			ratio = 1f;
		}
		else
		{
			ratio = tr.localScale[scaleAxis] / baseScale[scaleAxis];
		}
		float diameterM = plan.diameterMM / 1000f;
		float squish = (1f - ratio) * plan.L0M;
		if (squish < 0f)
		{
			squish = 0f;
		}
		squish /= 2f * diameterM;
		if (squish < 0f)
		{
			squish = 0f;
		}
		else if (squish > 1f)
		{
			squish = 1f;
		}
		if (mushroomRampMax < squish)
		{
			mushroomRampMax = squish;
		}
	}

	private void UpdateDeformBody()
	{
		if (plan == null || plan.projectile == null || deform == null)
		{
			return;
		}
		if (plan.isShapedCharge && jetDetonated)
		{
			deform.SetActive(false);
			if (hybrid != null)
			{
				hybrid.SetActive(false);
			}
			if (bodyModel != null)
			{
				bodyModel.SetActive(false);
			}
			if (mushroomHead != null && mushroomHead.activeSelf)
			{
				mushroomHead.SetActive(false);
			}
			return;
		}
		Transform tr = plan.projectile.transform;
		float axisScale = Mathf.Abs(baseScale[scaleAxis]);
		float ratio;
		if (axisScale <= 1E-09f)
		{
			ratio = 1f;
		}
		else
		{
			ratio = tr.localScale[scaleAxis] / baseScale[scaleAxis];
		}
		if (plan.isShapedCharge)
		{
			EnsureBodyModel();
			if (ProjectileBodyRenderer.Available)
			{
				deform.SetActive(false);
				if (hybrid != null)
				{
					hybrid.SetActive(false);
				}
				if (mushroomHead != null && mushroomHead.activeSelf)
				{
					mushroomHead.SetActive(false);
				}
				bodyModel.SetModel(plan.projectile.GetDefinition().BodyModelFor(SectionFrame.Active));
				bodyModel.Configure(plan.diameterMM / 1000f, plan.projectile.GetDefinition().BodyLengthMMFor(SectionFrame.Active) / 1000f, plan.projectile.coreType, 1f);
				bodyModel.SetActive(true);
				Vector3 nosePos = tr.position + currentDir * (ratio * baseVisualLen * 0.5f);
				bodyModel.Render(base.transform, nosePos, currentDir);
				return;
			}
		}
		if (bodyModel != null)
		{
			bodyModel.SetActive(false);
		}
		if (advanced && segments != null && segIndex < segments.Count)
		{
			PlaybackSegment seg = segments[segIndex];
			float k = 1f;
			if (seg.duration > 1E-06f)
			{
				k = segElapsed / seg.duration;
				if (k < 0f)
				{
					k = 0f;
				}
				else if (k > 1f)
				{
					k = 1f;
				}
			}
			if (RenderHybridForSegment(seg, k))
			{
				return;
			}
		}
		if (hybrid != null)
		{
			hybrid.SetActive(false);
		}
		Vector3 dir = (deformDirOverrideActive ? deformDirOverride : currentDir);
		Vector3 nose = tr.position + currentDir * (ratio * baseVisualLen * 0.5f);
		deform.SetActive(true);
		deform.Render(base.transform, nose, dir);
		lodgedNosePos = nose;
		lodgedDir = dir;
	}

	private void EnsureFins()
	{
		if (fins == null)
		{
			fins = new RodFins();
		}
	}

	private void SetFinPlane(int layerIndex)
	{
		if (plan == null || plan.layers == null || layerIndex < 0 || layerIndex >= plan.layers.Count || layerIndex <= finMaxLayer)
		{
			return;
		}
		finMaxLayer = layerIndex;
	}

	private bool FinPlaneUsable(int i)
	{
		LayerPlaybackData l = plan.layers[i];
		return l.eventKind == LayerEventKind.Penetration || l.eventKind == LayerEventKind.Stopped;
	}

	private float HoleDiameterM(LayerPlaybackData l)
	{
		TateModel.Penetrator core = plan.projectile.GetCorePenetrator();
		TateModel.Target target = ((l.plate != null) ? l.plate.GetTarget() : TateModel.RHA());
		return CraterModel.CraterDiameterRatio(core, target, l.entryVelocity) * (plan.diameterMM / 1000f);
	}

	private FinEnvelope CurrentFinEnvelope(bool stripped)
	{
		ProjectileDefinition def = plan.projectile.GetDefinition();
		int blades = ((fins != null) ? fins.BladeCount : 1);
		FinEnvelope envelope = FinEnvelope.For(plan.diameterMM / 1000f, def.gunCalibreMM / 1000f, plan.L0M, blades, HasFins);
		envelope.stripped = stripped;
		return envelope;
	}

	private bool FinsFitHole(LayerPlaybackData l)
	{
		ProjectileDefinition def = plan.projectile.GetDefinition();
		int blades = ((fins != null) ? fins.BladeCount : 1);
		FinEnvelope envelope = FinEnvelope.For(plan.diameterMM / 1000f, def.gunCalibreMM / 1000f, plan.L0M, blades, HasFins);
		envelope.stripped = false;
		TateModel.Penetrator core = plan.projectile.GetCorePenetrator();
		TateModel.Target target = ((l.plate != null) ? l.plate.GetTarget() : TateModel.RHA());
		float holeDiameterM = CraterModel.CraterDiameterRatio(core, target, l.entryVelocity) * (plan.diameterMM / 1000f);
		return FinCollision.Resolve(envelope, holeDiameterM) == FinStrikeOutcome.PassesThrough;
	}

	private void UpdateFins()
	{
		if (fins == null || plan == null || plan.projectile == null)
		{
			return;
		}
		if (!HasFins)
		{
			fins.SetActive(false);
			return;
		}
		Transform tr = plan.projectile.transform;
		float axisScale = Mathf.Abs(baseScale[scaleAxis]);
		float ratio;
		if (axisScale <= 1E-09f)
		{
			ratio = 1f;
		}
		else
		{
			ratio = tr.localScale[scaleAxis] / baseScale[scaleAxis];
		}
		if (ratio < 0f)
		{
			ratio = 0f;
		}
		else if (ratio > 1f)
		{
			ratio = 1f;
		}
		Vector3 tailPos = tr.position - currentDir * (ratio * baseVisualLen * 0.5f);
		if (finMaxLayer >= 0 && !fins.AllSheared)
		{
			float diameterM = plan.diameterMM / 1000f;
			int blades = fins.BladeCount;
			int pieces = fins.PieceCount;
			for (int j = 0; j < blades; j++)
			{
				for (int p = 0; p < pieces; p++)
				{
					if (fins.IsSheared(j, p))
					{
						continue;
					}
					float stationM = RodGeometry.PieceShearStationM(j, blades, p, pieces, diameterM, plan.L0M);
					Vector3 stationPoint = tailPos + currentDir * stationM;
					for (int i = 0; i <= finMaxLayer; i++)
					{
						if (i >= plan.layers.Count)
						{
							break;
						}
						if (!FinPlaneUsable(i))
						{
							continue;
						}
						LayerPlaybackData l = plan.layers[i];
						if (Vector3.Dot(stationPoint - l.entryPoint, l.direction) < 0f)
						{
							continue;
						}
						if (l.plate != null && !ShotRouting.FinEnvelopeApplies(l.plate.kind))
						{
							continue;
						}
						if (!FinsFitHole(l))
						{
							ShearBlade(j, p, l);
							break;
						}
					}
				}
			}
		}
		fins.SetActive(true);
		fins.Render(tailPos, currentDir);
	}

	private void ShearBlade(int j, int p, LayerPlaybackData l)
	{
		float diameterM = plan.diameterMM / 1000f;
		float gunCalibreM = plan.projectile.GetDefinition().gunCalibreMM / 1000f;
		Vector3 radial = fins.BladeRadial(j);
		float bladeHeightM = RodGeometry.BladeHeightM(diameterM, gunCalibreM);
		Vector3 origin = l.entryPoint + radial * (diameterM * 0.5f);
		Vector3 dir = (currentDir * finStripAxialFraction + radial).normalized;
		Collider ignoreCollider = plan.projectile.GetComponent<Collider>();
		if (FragmentSpawner.Instance != null)
		{
			FragmentSpawner.Instance.SpawnFinFragment(origin, dir, CurrentRodSpeedMps, plan.projectile.CoreDensity(), RodGeometry.FinThicknessM(diameterM), bladeHeightM, RodGeometry.FinChordM(diameterM, plan.L0M), radial, coreColor, ignoreCollider, MaterialLibrary.Core(plan.projectile.coreType), fins.PieceMesh(p), fins.PieceCount);
		}
		fins.MarkSheared(j, p);
		if (finsLogged)
		{
			return;
		}
		TateModel.Penetrator core = plan.projectile.GetCorePenetrator();
		TateModel.Target target = ((l.plate != null) ? l.plate.GetTarget() : TateModel.RHA());
		float craterRatio = CraterModel.CraterDiameterRatio(core, target, l.entryVelocity);
		bool finsPass = RodGeometry.FinsPassThroughCrater(diameterM, gunCalibreM, craterRatio * diameterM);
		Debug.Log(string.Concat(
			string.Format("✂ Оперение: размах {0:F1} мм ", RodGeometry.FinSpanM(diameterM, gunCalibreM) * 1000f),
			"(",
			((gunCalibreM > 0f) ? string.Format("из калибра ствола {0:F0} мм", gunCalibreM * 1000f) : "калибр ствола не задан — от стержня"),
			") ",
			string.Format("против отверстия ⌀{0:F1} мм (D_c/D_p = {1:F2}) — ", craterRatio * diameterM * 1000f, craterRatio),
			(finsPass ? "ПРОЛЕЗАЕТ, среза быть не должно" : "ШИРЕ отверстия ⇒ срезается о кромку"),
			"; ",
			string.Format("хорда {0:F1} мм, перьев {1} ", RodGeometry.FinChordM(diameterM, plan.L0M) * 1000f, fins.BladeCount),
			string.Format("× {0} рваных частей = {1} событий по хорде", fins.PieceCount, fins.PartCount)));
		finsLogged = true;
	}

	private void BuildSegments()
	{
		segments = new List<PlaybackSegment>();
		fuzeContactDistanceWU = ((plan.isShapedCharge) ? Vector3.Distance(plan.origin, plan.detonationPoint) : 0f);
		if (plan.verdict == ShotVerdict.NoModel)
		{
			return;
		}
		float halfLen = baseVisualLen * 0.5f;
		float combatVelocity = plan.projectile.combatVelocity;
		AddApproachLeg(combatVelocity);
		if (plan.verdict != ShotVerdict.NoTarget && plan.layers != null && plan.layers.Count != 0)
		{
			LayerPlaybackData l0 = plan.layers[0];
			Vector3 noseStart = l0.entryPoint - l0.direction * halfLen;
			fuzeContactDistanceWU = Vector3.Distance(plan.origin, noseStart);
			AddFlight(plan.origin, noseStart, l0.direction, combatVelocity);
			bool isJet = false;
			if (plan.isShapedCharge)
			{
				isJet = plan.jetTimeline != null;
				float standoffMM = Vector3.Dot(l0.entryPoint - plan.detonationPoint, l0.direction) * 1000f;
				string precursorText = ((plan.precursor != null) ? string.Format(", предзаряд — в {0:F0} мм", Vector3.Dot(l0.entryPoint - plan.precursor.detonationPoint, l0.direction) * 1000f) : "");
				Debug.Log(string.Concat(
					"  ◈ ПОДЛЁТ КОНЧАЕТСЯ КАСАНИЕМ: кончик корпуса приходит НА лицевую грань, ",
					string.Format("струя основного заряда рождается в {0:F0} мм позади него ", standoffMM),
					"(= взрывательный стандоф)",
					precursorText,
					". Длина показа корпуса ",
					string.Format("{0:F0} мм.", baseVisualLen * VisualDepthScale)));
			}
			bool stop = false;
			float jetCursorS = 0f;
			for (int i = 0; i < plan.layers.Count && !stop; i++)
			{
				LayerPlaybackData l = plan.layers[i];
				float halfK = Mathf.Clamp01(l.exitLengthM / Mathf.Max(plan.L0M, 1E-06f)) * baseVisualLen * 0.5f;
				Vector3 exitPoint = l.exitPoint;
				if (l.outcome.steps != null && l.outcome.steps.Count > 0)
				{
					exitPoint = l.entryPoint + l.direction * (l.outcome.steps[l.outcome.steps.Count - 1].depthMM / VisualDepthScale);
				}
				else if (l.outcome.penetrationDepthMM > 0f)
				{
					exitPoint = l.entryPoint + l.direction * (l.outcome.penetrationDepthMM / VisualDepthScale);
				}
				Vector3 nextEntry = ((i + 1 < plan.layers.Count) ? plan.layers[i + 1].entryPoint : plan.lineEnd);
				if (l.eventKind == LayerEventKind.Penetration || l.eventKind == LayerEventKind.Stopped)
				{
					PlaybackSegment seg = new PlaybackSegment();
					seg.kind = PlaybackSegKind.Layer;
					seg.duration = (isJet ? JetLayerDuration(l, ref jetCursorS) : RealDuration(l));
					seg.layerIndex = i;
					seg.dir = l.direction;
					segments.Add(seg);
					if (l.eventKind == LayerEventKind.Stopped || (plan.verdict == ShotVerdict.Exhausted && plan.verdictLayerIndex == i))
					{
						stop = true;
					}
					else
					{
						AddFlight(exitPoint - l.exitDirection * halfK, nextEntry - l.exitDirection * halfK, l.exitDirection, l.exitVelocity);
					}
				}
				else if (l.eventKind == LayerEventKind.Ricochet || l.eventKind == LayerEventKind.CriticalRicochet)
				{
					PlaybackSegment seg = new PlaybackSegment();
					seg.kind = PlaybackSegKind.Ricochet;
					seg.duration = RicochetContactNaturalSeconds(l);
					seg.layerIndex = i;
					seg.dir = l.direction;
					segments.Add(seg);
					AddFlight(l.entryPoint - l.exitDirection * halfK, nextEntry - l.exitDirection * halfK, l.exitDirection, l.exitVelocity);
				}
				else if (l.eventKind == LayerEventKind.Slide)
				{
					PlaybackSegment seg = new PlaybackSegment();
					seg.kind = PlaybackSegKind.Slide;
					seg.duration = (l.slideTrackWorld * VisualDepthScale / 1000f) / Mathf.Max(l.exitVelocity, 1f);
					seg.fromCenter = l.entryPoint - l.exitDirection * halfK;
					seg.toCenter = exitPoint - l.exitDirection * halfK;
					seg.layerIndex = i;
					seg.dir = l.exitDirection;
					segments.Add(seg);
					AddFlight(l.exitPoint - l.exitDirection * halfK, nextEntry - l.exitDirection * halfK, l.exitDirection, l.exitVelocity);
				}
			}
		}
		else
		{
			AddFlight(plan.origin, plan.origin + plan.direction * plan.rayDistance, plan.direction, combatVelocity);
		}
		RescaleToPlaybackDuration();
	}

	private static float RicochetContactNaturalSeconds(LayerPlaybackData l)
	{
		return Mathf.Max(l.entryLengthM, 0.001f) / Mathf.Max(l.entryVelocity, 1f);
	}

	private void AddApproachLeg(float combatVelocity)
	{
		if (plan == null || !plan.hasApproach)
		{
			return;
		}
		Vector3 diff = plan.origin - plan.approachFrom;
		if (diff.sqrMagnitude < 1E-08f)
		{
			return;
		}
		float mag = diff.magnitude;
		Vector3 dir = ((mag > 1E-05f) ? (diff / mag) : Vector3.zero);
		AddFlight(plan.approachFrom, plan.origin, dir, combatVelocity);
	}

	private void AddFlight(Vector3 from, Vector3 to, Vector3 d, float speed)
	{
		float mag = (to - from).magnitude;
		PlaybackSegment seg = new PlaybackSegment();
		seg.kind = PlaybackSegKind.Flight;
		seg.duration = mag * VisualDepthScale / 1000f / Mathf.Max(speed, 1f);
		seg.fromCenter = from;
		seg.toCenter = to;
		seg.dir = d;
		seg.speedMps = speed;
		segments.Add(seg);
	}

	private float WuToMeters(float worldUnits)
	{
		return worldUnits * VisualDepthScale / 1000f;
	}

	private float LayerShownDepthMM(int layerIndex)
	{
		if (plan == null || plan.layers == null || layerIndex < 0 || layerIndex >= plan.layers.Count)
		{
			return 50f;
		}
		LayerPlaybackData l = plan.layers[layerIndex];
		float depth = ((l.outcome.penetrationDepthMM > 0f) ? l.outcome.penetrationDepthMM : l.tEffMM);
		return Mathf.Max(depth, 0f);
	}

	private void RescaleToPlaybackDuration()
	{
		float total = 0f;
		foreach (PlaybackSegment seg in segments)
		{
			seg.naturalDuration = seg.duration;
			total += seg.duration;
		}
		float scale = playbackDuration / Mathf.Max(total, 1E-09f);
		foreach (PlaybackSegment seg in segments)
		{
			seg.duration *= scale;
		}
		foreach (PlaybackSegment seg in segments)
		{
			if (seg.kind == PlaybackSegKind.Layer || seg.kind == PlaybackSegKind.Slide)
			{
				float floorS = PlaybackTrack.LayerFloorSeconds(LayerShownDepthMM(seg.layerIndex), minLayerSeconds);
				if (floorS > seg.duration)
				{
					seg.duration = floorS;
				}
			}
			if (seg.kind == PlaybackSegKind.Ricochet)
			{
				if (ricochetContactSeconds > seg.duration)
				{
					seg.duration = ricochetContactSeconds;
				}
			}
		}
		PlaybackTrack.EnforceMonotonicSpeed(plan, segments, baseVisualLen);
	}

	private float RealDuration(LayerPlaybackData l)
	{
		if (l.outcome.steps == null || l.outcome.steps.Count < 1)
		{
			return 0f;
		}
		return l.outcome.steps[l.outcome.steps.Count - 1].t;
	}

	private float JetLayerDuration(LayerPlaybackData l, ref float jetCursorS)
	{
		float startS = jetCursorS;
		float delayS = plan.MainChargeDelayS;
		if (l.jetTimeline != null && l.jetTimeline.Length != 0)
		{
			for (int i = 0; i < l.jetTimeline.Length; i++)
			{
				JetElement e = l.jetTimeline[i];
				if (e.deltaP > 0f && delayS + e.arrivalTime > jetCursorS)
				{
					jetCursorS = delayS + e.arrivalTime;
				}
			}
			float result2 = jetCursorS - startS;
			return ((result2 < 0f) ? 0f : result2);
		}
		float jetTipVel = plan.jetTipVelocityMS;
		if (jetTipVel <= 0f)
		{
			if (plan.jetTimeline != null && plan.jetTimeline.Length != 0)
			{
				jetTipVel = plan.jetTimeline[0].velocity;
			}
			else
			{
				jetTipVel = 0f;
			}
		}
		if (plan.precursor != null && plan.precursorArmedPlates.Contains(l.plate) && plan.precursor.tipVelocityMS > 1E-06f)
		{
			float t = Vector3.Distance(plan.precursor.detonationPoint, l.entryPoint) / plan.precursor.tipVelocityMS;
			if (jetCursorS < t)
			{
				jetCursorS = t;
			}
		}
		else if (jetTipVel > 1E-06f)
		{
			float t2 = Vector3.Distance(plan.detonationPoint, l.entryPoint) / jetTipVel + delayS;
			if (jetCursorS < t2)
			{
				jetCursorS = t2;
			}
		}
		float result = jetCursorS - startS;
		if (result < 0f)
		{
			result = 0f;
		}
		return result;
	}

	private void AnimateSegment(PlaybackSegment seg, float t)
	{
		SetRodDirection(seg.dir);
		deformDirOverrideActive = false;
		float k = 1f;
		if (seg.duration > 1E-06f)
		{
			k = t / seg.duration;
			if (k < 0f)
			{
				k = 0f;
			}
			else if (k > 1f)
			{
				k = 1f;
			}
		}
		if (plan != null && plan.isShapedCharge)
		{
			Vector3 pos = plan.projectile.transform.position;
			if (!jetDetonated)
			{
				if (Vector3.Distance(plan.origin, pos) >= detonationDistanceWU - 1E-04f)
				{
					DetonateWarhead();
				}
			}
			if (jetDetonated && jetRenderer != null)
			{
				jetSincePhysS += Time.deltaTime / Mathf.Max(CurrentTimeDilation, 1E-06f);
				jetRenderer.UpdateAt(MainJetTimeS);
				UpdatePrecursorJet();
				UpdateJetChannel();
			}
		}
		if (seg.kind == PlaybackSegKind.Flight)
		{
			Transform tr = plan.projectile.transform;
			float k2 = Mathf.Clamp01(PlaybackTrack.EasedParam(plan, segments, segIndex, k, baseVisualLen));
			tr.position = Vector3.Lerp(seg.fromCenter, seg.toCenter, k2);
			CurrentRodSpeedMps = seg.speedMps;
		}
		else if (seg.kind == PlaybackSegKind.Layer)
		{
			LayerPlaybackData l = plan.layers[seg.layerIndex];
			if (spallLayerIndex != seg.layerIndex)
			{
				BeginLayer(seg.layerIndex, l);
			}
			SampleLayer(l, RealDuration(l) * k, out var depthMM, out var lengthM);
			ApplyRodState(l, depthMM, lengthM);
			Vector3 nose = l.entryPoint + l.direction * (depthMM / VisualDepthScale);
			Vector3 surfT = ((l.eventKind == LayerEventKind.Stopped) ? Vector3.ProjectOnPlane(l.direction, l.surfaceNormal) : Vector3.zero);
			EmitBackjet(nose, l.direction, surfT, l.entryVelocity, lengthM);
			if (deform != null)
			{
				deform.SetSampleState(seg.layerIndex, RealDuration(l) * k);
			}
			float angle = Vector3.Angle(l.direction, l.exitDirection);
			if (angle > 0.01f && deform != null)
			{
				Vector3 v = Vector3.ProjectOnPlane(l.exitDirection, l.direction);
				if (v.sqrMagnitude > 1E-10f)
				{
					deform.SetBendPose(Mathf.Lerp(bendPermDeg, Mathf.Max(bendPermDeg, angle), k), v.normalized, false);
				}
			}
			DeformableMesh dm = null;
			if (FindJetLayer(seg.layerIndex) == null && l.plate != null)
			{
				dm = l.plate.GetComponent<DeformableMesh>();
			}
			if (dm != null)
			{
				dm.UpdateChannel(l.entryPoint, l.direction, holeRadiusWorld, depthMM / VisualDepthScale, fullThickWorld, through: false, holeIsCeramic, holeAllowPetaling, holeExtraThin, updateCollider: false, holeRaggedAmp);
			}
			if (FindJetLayer(seg.layerIndex) == null)
			{
				ProgressiveSpall(l, depthMM);
			}
		}
		else if (seg.kind == PlaybackSegKind.Ricochet)
		{
			LayerPlaybackData l = plan.layers[seg.layerIndex];
			float lengthM = Mathf.Lerp(l.entryLengthM, l.exitLengthM, k);
			CurrentRodSpeedMps = Mathf.Lerp(l.entryVelocity, l.exitVelocity, k);
			ApplyRodState(l, 0f, lengthM);
			if (deform != null)
			{
				deform.SetSampleState(seg.layerIndex, k);
			}
			UpdateBendForContact(l, k, ricochet: true);
		}
		else if (seg.kind == PlaybackSegKind.Slide)
		{
			LayerPlaybackData l = plan.layers[seg.layerIndex];
			float k2 = Mathf.Clamp01(PlaybackTrack.EasedParam(plan, segments, segIndex, k, baseVisualLen));
			Vector3 pos = Vector3.Lerp(l.entryPoint, l.exitPoint, k2);
			float lengthM = Mathf.Lerp(l.entryLengthM, l.exitLengthM, k);
			CurrentRodSpeedMps = Mathf.Lerp(l.entryVelocity, l.exitVelocity, k);
			Transform tr = plan.projectile.transform;
			float k4 = Mathf.Clamp01(lengthM / Mathf.Max(plan.L0M, 1E-06f));
			Vector3 newScale = baseScale;
			newScale[scaleAxis] = baseScale[scaleAxis] * Mathf.Max(k4, 0.001f);
			tr.localScale = newScale;
			tr.position = pos - l.exitDirection * (k4 * baseVisualLen * 0.5f);
			EmitBackjet(pos, l.direction, l.exitDirection, l.entryVelocity, lengthM);
			if (deform != null)
			{
				deform.SetSampleState(seg.layerIndex, k);
			}
			UpdateBendForContact(l, k, ricochet: false);
			deformDirOverride = l.direction;
			deformDirOverrideActive = true;
			DeformableMesh dm = ((l.plate != null) ? l.plate.GetComponent<DeformableMesh>() : null);
			if (dm != null)
			{
				dm.DentPath(l.entryPoint, pos, -l.surfaceNormal, 0.18f, l.grooveDepthMM / VisualDepthScale, updateCollider: false);
			}
		}
	}

	private bool ContactBendTarget(LayerPlaybackData l, bool ricochet, out float thetaDeg, out Vector3 bendDir)
	{
		float angle = Vector3.Angle(-l.direction, l.surfaceNormal);
		Vector3 dir;
		float theta;
		if (ricochet)
		{
			dir = Vector3.ProjectOnPlane(-l.surfaceNormal, l.direction);
			theta = Mathf.Max(angle * ricochetBendCoeff, ricochetBendMinDeg);
		}
		else
		{
			dir = Vector3.ProjectOnPlane(l.surfaceNormal, l.direction);
			theta = Mathf.Min(90f - angle, slideBendMaxDeg);
		}
		thetaDeg = Mathf.Max(theta, bendPermDeg);
		bendDir = Vector3.up;
		if (dir.sqrMagnitude < 1E-10f)
		{
			return false;
		}
		bendDir = dir.normalized;
		return true;
	}

	private void UpdateBendForContact(LayerPlaybackData l, float k, bool ricochet)
	{
		if (deform == null)
		{
			return;
		}
		if (Projectile.IsBrittleCore(plan.projectile.coreType))
		{
			return;
		}
		if (!ContactBendTarget(l, ricochet, out var thetaDeg, out var bendDir))
		{
			return;
		}
		if (k < 0f)
		{
			k = 0f;
		}
		else if (k > 1f)
		{
			k = 1f;
		}
		deform.SetBendPose((thetaDeg - bendPermDeg) * k + bendPermDeg, bendDir, false);
	}

	private void CommitContactBend(LayerPlaybackData l, bool ricochet, Vector3 breakPoint)
	{
		Vector3 tangent = Vector3.ProjectOnPlane(l.direction, l.surfaceNormal);
		if (tangent.sqrMagnitude < 1E-08f)
		{
			tangent = l.exitDirection;
		}
		tangent = tangent.normalized;
		string text = (ricochet ? "рикошет" : "скольжение");
		if (Projectile.IsBrittleCore(plan.projectile.coreType))
		{
			Debug.Log(string.Concat(
				string.Format("  ✸ ХРУПКИЙ сердечник ({0}) на {1}: изгиба нет ", plan.projectile.coreType, text),
				string.Format("(ε_f не применяется) → РАСКОЛ носовой зоны на {0} обломка", brittleSplitPieces)));
			BreakOffFront(l, breakPoint, tangent, l.exitVelocity, brittleSplitPieces);
			return;
		}
		if (!ContactBendTarget(l, ricochet, out var thetaDeg, out var bendDir))
		{
			return;
		}
		bendDirWorld = bendDir;
		bendPermDeg = thetaDeg;
		bendAlignNose = !ricochet;
		if (deform != null)
		{
			deform.SetBendPose(thetaDeg, bendDir, !ricochet);
		}
		float hingeM = ((deform != null) ? deform.HingeLengthM() : (plan.diameterMM * 1.5f / 1000f));
		float rM = plan.diameterMM * 0.5f / 1000f;
		float eps = rM * (thetaDeg * Mathf.Deg2Rad) / Mathf.Max(hingeM, 1E-06f);
		float strain = Projectile.CoreFractureStrain(plan.projectile.coreType);
		bool fracture = eps > strain;
		Debug.Log(string.Concat(
			string.Format("  ⌒ изгиб на {0}: θ_b={1:F1}° | ε_внешн = r₀·θ_b/ℓ_h = ", text, thetaDeg),
			string.Format("{0:F1}мм·{1:F3}/{2:F1}мм = {3:F3} ", rM * 1000f, thetaDeg * Mathf.Deg2Rad, hingeM * 1000f, eps),
			string.Format("vs ε_f({0})={1:F2} [ASSUMED] → ", plan.projectile.coreType, strain),
			(fracture ? "ПЕРЕЛОМ в шарнире" : "шарнир держит — стержень уходит согнутым")));
		if (!fracture)
		{
			return;
		}
		BreakOffFront(l, breakPoint, tangent, l.exitVelocity);
		bendPermDeg = 0f;
		if (deform != null)
		{
			deform.SetBendPose(0f, bendDirWorld, false);
			deform.raggedTail = true;
		}
	}

	private void SampleLayer(LayerPlaybackData l, float tReal, out float depthMM, out float lengthM)
	{
		List<TateStep> steps = l.outcome.steps;
		if (steps == null || steps.Count == 0)
		{
			depthMM = 0f;
			lengthM = l.entryLengthM;
			CurrentRodSpeedMps = l.entryVelocity;
			return;
		}
		while (sampleCursor < steps.Count && tReal > steps[sampleCursor].t)
		{
			sampleCursor++;
		}
		if (sampleCursor >= steps.Count)
		{
			TateStep last = steps[steps.Count - 1];
			depthMM = last.depthMM;
			lengthM = last.L;
			CurrentRodSpeedMps = last.v;
			return;
		}
		TateStep cur = steps[sampleCursor];
		float prevT = 0f;
		float prevDepthMM = 0f;
		float prevV = l.entryVelocity;
		float prevL = l.entryLengthM;
		if (sampleCursor > 0)
		{
			TateStep prev = steps[sampleCursor - 1];
			prevT = prev.t;
			prevDepthMM = prev.depthMM;
			prevV = prev.v;
			prevL = prev.L;
		}
		float k = 1f;
		if (cur.t - prevT > 1E-12f)
		{
			k = Mathf.Clamp01((tReal - prevT) / (cur.t - prevT));
		}
		depthMM = (cur.depthMM - prevDepthMM) * k + prevDepthMM;
		lengthM = (cur.L - prevL) * k + prevL;
		CurrentRodSpeedMps = (cur.v - prevV) * k + prevV;
	}

	private void ApplyRodLength(float lengthM)
	{
		CurrentModelRodLengthM = lengthM;
		float k = lengthM / Mathf.Max(plan.L0M, 1E-06f);
		if (k < 0f)
		{
			k = 0f;
		}
		else if (k > 1f)
		{
			k = 1f;
		}
		if (k < 0.001f)
		{
			k = 0.001f;
		}
		Vector3 newScale = baseScale;
		if (scaleAxis == 0)
		{
			newScale.x = baseScale.x * k;
		}
		else if (scaleAxis == 1)
		{
			newScale.y = baseScale.y * k;
		}
		else
		{
			newScale.z = baseScale.z * k;
		}
		plan.projectile.transform.localScale = newScale;
	}

	private void ApplyRodState(LayerPlaybackData l, float depthMM, float lengthM)
	{
		CurrentModelRodLengthM = lengthM;
		float k = Mathf.Clamp01(lengthM / Mathf.Max(plan.L0M, 1E-06f));
		Transform tr = plan.projectile.transform;
		Vector3 newScale = baseScale;
		newScale[scaleAxis] = baseScale[scaleAxis] * Mathf.Max(k, 0.001f);
		tr.localScale = newScale;
		float depthWorld = depthMM / VisualDepthScale;
		tr.position = l.entryPoint + l.direction * depthWorld - l.direction * (k * baseVisualLen * 0.5f);
	}

	private void BeginLayer(int idx, LayerPlaybackData l)
	{
		EnsureJetLayers();
		spallLayerIndex = idx;
		SetFinPlane(idx);
		if (l.plate != null && l.plate.IsEraPlate && (!l.plate.cassetteExpended || plan.precursorArmedPlates.Contains(l.plate)))
		{
			EraCassette.Detonate(l.plate, l.direction, WorldPerMeter, CurrentTimeDilation, plan.precursorArmedPlates.Contains(l.plate), l.entryPoint);
		}
		if (l.plate != null && l.plate.IsNeraPlate && !l.plate.cassetteExpended)
		{
			NeraBlock.Fire(l.plate, l.direction, WorldPerMeter, CurrentTimeDilation, l.entryPoint, NeraHoleRadiusWorld());
		}
		spallSpawned = 0;
		spallPrevDepthMM = 0f;
		if (l.plate != null && (l.plate.IsEraPlate || l.plate.IsNeraPlate))
		{
			spallTotalN = 0;
			holeRadiusWorld = 0f;
			holeIsCeramic = false;
			holeExtraThin = false;
			holeRaggedAmp = 0f;
			fullThickWorld = l.tEffMM / VisualDepthScale;
			finalDepthMM = 0f;
			return;
		}
		if (l.tailBrokeFromYaw && l.tailBreakLengthM > 1E-04f)
		{
			SpawnTailBreak(l);
			if (deform != null)
			{
				deform.raggedTail = true;
			}
		}
		spallIgnoreSource = l.eventKind == LayerEventKind.Penetration;
		if (FindJetLayer(idx) != null)
		{
			JetLayer jl = FindJetLayer(idx);
			holeRadiusWorld = jl.channel.RadiusAtDepthM(0f);
			holeIsCeramic = false;
			holeExtraThin = false;
			holeRaggedAmp = 0f;
			fullThickWorld = jl.fullThickWU;
			spallAxis = jl.spallAxis;
			spallRefSpeed = jl.spallSpeed;
			spallRadiusM = jl.spallRadiusM;
			finalDepthMM = Mathf.Max(jl.channel.FinalDepthM * 1000f, 0.001f);
			spallTotalN = 0;
			return;
		}
		TateModel.Penetrator core = plan.projectile.GetCorePenetrator();
		TateModel.Target target = ((l.plate != null) ? l.plate.GetTarget() : TateModel.RHA());
		float erosion = CraterModel.ErosionIndicator(core, target, l.entryVelocity);
		float ratio = CraterModel.CraterDiameterRatio(core, target, l.entryVelocity);
		holeRadiusWorld = (plan.diameterMM / 1000f) * ratio * 0.5f;
		holeIsCeramic = l.plate != null && l.plate.armorType == ArmorPlate.ArmorType.Ceramic;
		holeAllowPetaling = l.plate != null && CraterModel.PetalingApplies(l.plate.thicknessMM, plan.diameterMM, erosion);
		holeExtraThin = l.plate != null && l.plate.thicknessMM < plan.diameterMM * 0.5f;
		fullThickWorld = l.tEffMM / VisualDepthScale;
		CraterModel.RimClass rimClass = ((l.plate != null) ? CraterModel.RimClassOf(l.plate.armorType) : CraterModel.RimClass.Other);
		holeRaggedAmp = ((l.plate != null && CraterModel.RaggedRimApplies(l.plate.armorType, l.plate.thicknessMM, plan.diameterMM)) ? CraterModel.RaggedRimAmplitude(rimClass) : 0f);
		float tOverD = ((plan.diameterMM > 0f) ? (((l.plate != null) ? l.plate.thicknessMM : 0f) / plan.diameterMM) : 0f);
		Debug.Log(string.Concat(
			string.Format("◇ Кратер слой {0}: D_c/D_p = {1:F3} (Бьерке при {2:F2} км/с", idx + 1, ratio, l.entryVelocity / 1000f),
			((l.entryVelocity < 1200f) ? ", ЗАЖАТО на границе области 1.2 км/с" : ""),
			"), ",
			string.Format("⌀{0:F1} мм | индикатор эрозии {1:F2} ", ratio * plan.diameterMM, erosion),
			"| режим: ",
			(holeIsCeramic ? "хрупкий скол" : (holeAllowPetaling ? "лепесткование (тонкая плита, жёсткий стержень)" : "вязкое раздавание отверстия + пробка"))));
		Debug.Log(string.Concat(
			string.Format("  ◇ кромка: t/D = {0:F2} (порог разрыва {1:F1} — ASSUMED), ", tOverD, 1f),
			string.Format("класс «{0}» ⇒ {1}", rimClass, ((holeRaggedAmp > 0f) ? string.Format("РВАНАЯ, амплитуда {0:F2}·R", holeRaggedAmp) : "без разрыва")),
			((rimClass == CraterModel.RimClass.Ceramic) ? " (керамике разрыв не положен: скол и радиальные трещины)" : ""),
			((rimClass == CraterModel.RimClass.Composite) ? " (слоистые расслаиваются, а не рвутся)" : "")));
		float shownDepth = ((l.eventKind == LayerEventKind.Penetration) ? l.tEffMM : l.outcome.penetrationDepthMM);
		float remainingMM = l.tEffMM - shownDepth;
		Debug.Log(string.Concat(
			string.Format("  ◇ выброс перед пробоем: зона — последние {0:F1} мм (1·D_p, MEASURED); ", CraterModel.PreBreakoutLigamentMM(plan.diameterMM)),
			string.Format("перемычка в конце слоя {0:F1} мм ⇒ ", remainingMM),
			((l.eventKind == LayerEventKind.Penetration) ? "выброс отработал, дальше ПРОБКА (отдельное событие)" : (CraterModel.PreBreakoutSpallApplies(remainingMM, plan.diameterMM) ? "ЗАСТРЯЛ ВНУТРИ ЗОНЫ: тыл выпучился и сыпал вперёд, пробки НЕТ" : "застрял НЕ ДОЙДЯ до зоны: ни выпучивания, ни пробки"))));
		if (l.eventKind == LayerEventKind.Penetration)
		{
			spallAxis = (l.exitDirection * 0.3f - l.surfaceNormal * 0.7f).normalized;
			spallRefSpeed = l.outcome.residualVelocity;
			spallRadiusM = holeRadiusWorld;
			finalDepthMM = Mathf.Max(l.tEffMM, 0.001f);
			if (FragmentSpawner.Instance != null)
			{
				spallTotalN = FragmentSpawner.Instance.SpallTotalCount(l.plate, holeRadiusWorld, ((l.plate != null) ? l.plate.thicknessMM : 0f) / 1000f);
			}
			else
			{
				spallTotalN = 0;
			}
		}
		else
		{
			spallAxis = l.surfaceNormal;
			spallRefSpeed = l.entryVelocity * 0.1f;
			finalDepthMM = Mathf.Max(l.outcome.penetrationDepthMM, 0.001f);
			spallRadiusM = plan.diameterMM / 1000f * 1.4f * 0.5f;
			if (FragmentSpawner.Instance != null)
			{
				spallTotalN = FragmentSpawner.Instance.SpallTotalCount(l.plate, spallRadiusM, Mathf.Max(l.outcome.penetrationDepthMM / 1000f, plan.diameterMM / 1000f * 0.5f));
			}
			else
			{
				spallTotalN = 0;
			}
		}
	}

	public static Vector3 EmissionTubeAxis(Vector3 shotDir, bool freeBackFace)
	{
		if (freeBackFace)
		{
			return Vector3.zero;
		}
		return shotDir;
	}

	private void CraterEmission(LayerPlaybackData l, float depthMM, float radiusM, bool behindArmor, out Vector3 origin, out Vector3 axisOut, out float speedScale, out Vector3 axisPoint, out Vector3 axisDir)
	{
		Vector3 dir = ((l.direction.sqrMagnitude > 1E-09f) ? l.direction.normalized : Vector3.forward);
		Vector3 axis1 = Vector3.Cross(dir, Vector3.up);
		if (axis1.sqrMagnitude < 1E-06f)
		{
			axis1 = Vector3.Cross(dir, Vector3.left);
		}
		axis1 = axis1.normalized;
		Vector3 axis2 = Vector3.Cross(dir, axis1).normalized;
		float angle = UnityEngine.Random.value * 2f * Mathf.PI;
		Vector3 randomDir = (Mathf.Cos(angle) * axis2 + Mathf.Sin(angle) * axis1).normalized;
		speedScale = Mathf.Clamp(CurrentRodSpeedMps / Mathf.Max(l.entryVelocity, 1f), 0.05f, 1f);
		if (!behindArmor && plan != null && l.plate != null && CraterModel.PreBreakoutSpallApplies(l.tEffMM - depthMM, plan.diameterMM))
		{
			float plugR = CraterModel.PlugDiameterMM(plan.diameterMM) * 0.5f / 1000f;
			float r = Mathf.Sqrt(UnityEngine.Random.value) * plugR;
			float depthW = Mathf.Max(l.tEffMM, depthMM) / VisualDepthScale;
			Vector3 pos = l.entryPoint + dir * depthW + randomDir * r;
			origin = pos;
			axisOut = pos;
			axisPoint = Vector3.zero;
			float coneDeg = ((FragmentSpawner.Instance != null) ? FragmentSpawner.Instance.coneHalfAngleDeg : 25f);
			axisDir = (dir + randomDir * (UnityEngine.Random.value * Mathf.Tan(Mathf.Max(coneDeg, 0f) * Mathf.Deg2Rad))).normalized;
			return;
		}
		axisOut = l.entryPoint + dir * (depthMM / VisualDepthScale);
		origin = axisOut + randomDir * Mathf.Max(radiusM, 1E-04f);
		axisPoint = dir;
		if (behindArmor)
		{
			axisDir = spallAxis;
		}
		else
		{
			axisDir = (randomDir - dir).normalized;
		}
	}

	private bool PreBreakoutActive(LayerPlaybackData l, float depthMM)
	{
		if (plan == null || l.plate == null)
		{
			return false;
		}
		return CraterModel.PreBreakoutSpallApplies(l.tEffMM - depthMM, plan.diameterMM);
	}

	private void ReportSpallStarvation(int layerIndex, LayerPlaybackData l)
	{
		if (spallReportedLayer == layerIndex)
		{
			return;
		}
		spallReportedLayer = layerIndex;
		Debug.LogWarning(string.Concat(
			string.Format("◇ ОСКОЛКОВ НЕТ на слое {0} [{1}]: ", layerIndex + 1, ((l.plate != null) ? l.plate.name : "?")),
			string.Format("бюджет слоя N = {0}, показанная глубина {1:F1} мм, ", spallTotalN, finalDepthMM),
			"спавнер ",
			((FragmentSpawner.Instance != null) ? "есть" : "ОТСУТСТВУЕТ"),
			", ",
			string.Format("свободно в общем бюджете {0}. ", ((FragmentSpawner.Instance != null) ? FragmentSpawner.Instance.FreeBudget : (-1))),
			"Если N = 0 — вопрос к SpallTotalCount (объём канала); если N > 0, а осколков нет — слою не досталось ни одного кадра показа."));
	}

	private void ProgressiveSpall(LayerPlaybackData l, float depthMM)
	{
		if (FragmentSpawner.Instance == null || spallTotalN <= 0)
		{
			ReportSpallStarvation(spallLayerIndex, l);
			return;
		}
		float k = Mathf.Clamp01(depthMM / Mathf.Max(finalDepthMM, 1E-04f));
		int count = (int)Math.Round(spallTotalN * k) - spallSpawned;
		if (count <= 0)
		{
			spallPrevDepthMM = depthMM;
			return;
		}
		DeformableMesh dm = ((l.plate != null) ? l.plate.GetComponent<DeformableMesh>() : null);
		float carvedMM = ((dm != null && dm.CarvedDepthWorld >= 0f) ? (dm.CarvedDepthWorld * VisualDepthScale) : depthMM);
		float prevMM = spallPrevDepthMM;
		for (int i = 0; i < count; i++)
		{
			float u = Mathf.Clamp01((i + 0.5f) / count);
			float depth = Mathf.Min(prevMM + (depthMM - prevMM) * u, carvedMM);
			CraterEmission(l, depth, spallRadiusM, spallIgnoreSource, out var origin, out var axisOut, out var speedScale, out var axisPoint, out var axisDir);
			spallLedgerCreated += FragmentSpawner.Instance.SpawnSpallPortion(origin, axisOut, speedScale, l.plate, spallRadiusM, 1, FragmentSpawner.Instance.coneHalfAngleDeg, spallIgnoreSource, axisPoint, axisDir, depth / VisualDepthScale);
			MicroSpall.Burst(axisPoint, l.direction, CurrentRodSpeedMps / CurrentTimeDilation, CurrentRodSpeedMps, l.plate, spallRadiusM, 1, ResultPalette.Perforated);
		}
		spallLedgerBirthRmm = spallRadiusM * 1000f;
		spallSpawned += count;
		spallLedgerAsked += count;
		spallLedgerChannelRmm = holeRadiusWorld * 1000f;
		spallLedgerCarvedMM = carvedMM;
		spallPrevDepthMM = depthMM;
		spallLedgerNoseMM = depthMM;
	}

	private void ReportSpallLedger(int layerIndex, LayerPlaybackData l)
	{
		if (layerIndex < 0)
		{
			return;
		}
		int cut = Mathf.Max(spallLedgerAsked - spallLedgerCreated, 0);
		int liveActive = ((FragmentSpawner.Instance != null) ? FragmentSpawner.Instance.LiveActive : (-1));
		int liveSettled = ((FragmentSpawner.Instance != null) ? FragmentSpawner.Instance.LiveSettled : (-1));
		string verdict;
		if (cut > 0)
		{
			verdict = " ⇒ СРЕЗАНО (потолка в коде нет — смотри вызывающего)";
		}
		else if (spallLedgerCreated <= 0)
		{
			verdict = " ⇒ НЕ СОЗДАНЫ ВОВСЕ: смотри бюджет слоя";
		}
		else if (liveActive == 0)
		{
			verdict = " ⇒ СОЗДАНЫ, НО НЕ ДОЖИЛИ: гибнут в момент рождения";
		}
		else
		{
			verdict = " ⇒ СОЗДАНЫ И ЖИВЫ: если на экране пусто, дело в показе, а не в эмиссии";
		}
		Debug.Log(string.Concat(
			string.Format("◇ ОСКОЛКИ слой {0} [{1}]: ", layerIndex + 1, ((l.plate != null) ? l.plate.name : "?")),
			string.Format("СОЗДАНО {0} из запрошенных {1} ", spallLedgerCreated, spallLedgerAsked),
			string.Format("(бюджет слоя {0}{1}) | ", spallTotalN, ((cut > 0) ? string.Format(", СРЕЗАНО {0}", cut) : "")),
			string.Format("ЖИВЫХ сейчас {0}, осевших {1} | ", liveActive, liveSettled),
			string.Format("радиус рождения {0:F1} мм = радиус канала {1:F1} мм | ", spallLedgerBirthRmm, spallLedgerChannelRmm),
			string.Format("дно {0:F1} против носика {1:F1} мм", spallLedgerCarvedMM, spallLedgerNoseMM),
			verdict));
		spallLedgerCreated = 0;
	}

	private void CompleteSegment(PlaybackSegment seg)
	{
		SetRodDirection(seg.dir);
		if (seg.kind == PlaybackSegKind.Flight)
		{
			plan.projectile.transform.position = seg.toCenter;
			return;
		}
		if (seg.kind == PlaybackSegKind.Ricochet)
		{
			LayerPlaybackData ricochetLayer = plan.layers[seg.layerIndex];
			ApplyRodLength(ricochetLayer.exitLengthM);
			if (ricochetLayer.plate != null)
			{
				ricochetLayer.plate.SetDisplayColor(ResultPalette.Ricochet);
			}
			if (ricochetLayer.grooveDepthMM > 0f)
			{
				DeformableMesh ricochetDm = ((ricochetLayer.plate != null) ? ricochetLayer.plate.GetComponent<DeformableMesh>() : null);
				if (ricochetDm != null)
				{
					ricochetDm.Dent(ricochetLayer.entryPoint, -ricochetLayer.surfaceNormal, 0.3f, ricochetLayer.grooveDepthMM / VisualDepthScale);
				}
			}
			if (deform != null)
			{
				deform.SetFinalState(seg.layerIndex);
			}
			CommitContactBend(ricochetLayer, ricochet: true, ricochetLayer.entryPoint);
			return;
		}
		if (seg.kind == PlaybackSegKind.Slide)
		{
			LayerPlaybackData slideLayer = plan.layers[seg.layerIndex];
			Transform tr = plan.projectile.transform;
			tr.position = seg.toCenter;
			ApplyRodLength(slideLayer.exitLengthM);
			if (slideLayer.plate != null)
			{
				slideLayer.plate.SetDisplayColor(SlideColor);
			}
			DeformableMesh slideDm = ((slideLayer.plate != null) ? slideLayer.plate.GetComponent<DeformableMesh>() : null);
			if (slideDm != null)
			{
				slideDm.DentPath(slideLayer.entryPoint, slideLayer.exitPoint, -slideLayer.surfaceNormal, 0.18f, slideLayer.grooveDepthMM / VisualDepthScale);
			}
			if (deform != null)
			{
				deform.SetFinalState(seg.layerIndex);
			}
			CommitContactBend(slideLayer, ricochet: false, slideLayer.entryPoint);
			return;
		}
		LayerPlaybackData l = plan.layers[seg.layerIndex];
		ApplyRodState(l, l.outcome.penetrationDepthMM, l.outcome.residualLengthM);
		if (deform != null)
		{
			deform.SetFinalState(seg.layerIndex);
		}
		float angle = Vector3.Angle(l.direction, l.exitDirection);
		if (angle > 0.01f)
		{
			Vector3 v = Vector3.ProjectOnPlane(l.exitDirection, l.direction);
			if (v.sqrMagnitude > 1E-10f)
			{
				bendPermDeg = Mathf.Max(bendPermDeg, angle);
				Vector3 bendDir = v.normalized;
				bendDirWorld = bendDir;
				bendAlignNose = true;
				if (deform != null)
				{
					deform.SetBendPose(bendPermDeg, bendDir, true);
				}
			}
		}
		if (spallLayerIndex != seg.layerIndex)
		{
			BeginLayer(seg.layerIndex, l);
		}
		if (FindJetLayer(seg.layerIndex) != null)
		{
			return;
		}
		DeformableMesh dm = ((l.plate != null) ? l.plate.GetComponent<DeformableMesh>() : null);
		if (l.outcome.result == PenetrationResult.Penetrated)
		{
			if (dm != null)
			{
				dm.UpdateChannel(l.entryPoint, l.direction, holeRadiusWorld, fullThickWorld, fullThickWorld, through: true, holeIsCeramic, holeAllowPetaling, holeExtraThin, updateCollider: false, holeRaggedAmp);
			}
			if (l.plate != null)
			{
				l.plate.SetDisplayColor(ResultPalette.Perforated);
			}
			ProgressiveSpall(l, finalDepthMM);
			ReportSpallLedger(seg.layerIndex, l);
			if (FragmentSpawner.Instance != null && spallTotalN > 0)
			{
				int backSplashN = Mathf.RoundToInt(spallTotalN * FragmentSpawner.Instance.backSplashFraction);
				if (backSplashN > 0)
				{
					FragmentSpawner.Instance.SpawnSpallPortion(l.entryPoint, l.surfaceNormal, spallRefSpeed * 0.3f, l.plate, spallRadiusM, backSplashN, 55f, ignoreSourcePlate: false);
				}
			}
			if (!holeIsCeramic && FragmentSpawner.Instance != null && l.plate != null)
			{
				float plugD = CraterModel.PlugDiameterMM(plan.diameterMM);
				float plugT = CraterModel.PlugThicknessMM(plan.diameterMM, l.plate.thicknessMM);
				float plugSpeed = l.outcome.residualVelocity * 0.8f;
				FragmentSpawner.Instance.SpawnPlugCluster(l.exitPoint, l.direction, plugD / VisualDepthScale, plugT / VisualDepthScale, l.plate, plugSpeed);
				Debug.Log(string.Concat(
					string.Format("◇ Пробка тыльной грани: ⌀{0:F1} × {1:F1} мм ", plugD, plugT),
					string.Format("({0:F0}% толщины плиты), ", plugT / Mathf.Max(l.plate.thicknessMM, 0.001f) * 100f),
					string.Format("{0:F0} м/с (< остаточной {1:F0})", plugSpeed, l.outcome.residualVelocity)));
			}
			if (dm != null)
			{
				dm.SyncCollider();
			}
		}
		else
		{
			if (dm != null)
			{
				dm.UpdateChannel(l.entryPoint, l.direction, holeRadiusWorld, l.outcome.penetrationDepthMM / VisualDepthScale, fullThickWorld, through: false, holeIsCeramic, holeAllowPetaling, holeExtraThin, updateCollider: true, holeRaggedAmp);
			}
			if (l.plate != null)
			{
				l.plate.SetDisplayColor(ResultPalette.Stopped);
			}
			ProgressiveSpall(l, finalDepthMM);
			ReportSpallLedger(seg.layerIndex, l);
		}
	}

	private void Finish()
	{
		playing = false;
		if (RenderedSpeedAudit.Instance != null)
		{
			RenderedSpeedAudit.Instance.ReportShot();
		}
		if (plan == null)
		{
			return;
		}
		FinalizeJetChannels();
		if (plan.verdictMessage != null && ResultDisplay.Instance != null)
		{
			ResultDisplay.Instance.Show(plan.verdictMessage, plan.verdictColor);
		}
		GameEvents.RaiseShotResolved(ShotReportBuilder.FromPlan(plan, AppFlow.ActiveMode.ToString()));
		plan.projectile.DrawTracePath(plan.tracePoints, plan.lineColor);
		if ((plan.verdict == ShotVerdict.Stopped || plan.verdict == ShotVerdict.Exhausted) && !plan.isShapedCharge && lodgedDir.sqrMagnitude > 1E-08f)
		{
			lodgedInArmour = true;
			return;
		}
		plan.projectile.ResetTransformToStart();
	}

	private void HoldLodged()
	{
		if (deform == null)
		{
			lodgedInArmour = false;
			return;
		}
		deform.SetActive(true);
		Transform parent = base.transform;
		deform.Render(parent, lodgedNosePos, lodgedDir);
	}

	public void ReleaseLodged()
	{
		if (!lodgedInArmour)
		{
			return;
		}
		lodgedInArmour = false;
		lodgedDir = Vector3.zero;
		if (previewProjectile != null)
		{
			previewProjectile.ResetTransformToStart();
		}
	}

	public static void ReleaseLodgedIfAny()
	{
		PenetrationPlayer player = UnityEngine.Object.FindFirstObjectByType<PenetrationPlayer>();
		if (player != null)
		{
			player.ReleaseLodged();
		}
	}

	private bool ChooseDynamicEvent(out int layerIndex)
	{
		layerIndex = -1;
		if (plan == null)
		{
			return false;
		}
		if (plan.layers == null || plan.isShapedCharge)
		{
			return false;
		}
		for (int i = 0; i < plan.layers.Count; i++)
		{
			LayerEventKind kind = plan.layers[i].eventKind;
			if (kind == LayerEventKind.Penetration || (kind >= LayerEventKind.Stopped && kind != LayerEventKind.CriticalRicochet) || kind == LayerEventKind.CriticalRicochet)
			{
				layerIndex = i;
				return true;
			}
		}
		return false;
	}

	[IteratorStateMachine(typeof(_003CPrecomputeThenPlay_003Ed__177))]
	private IEnumerator PrecomputeThenPlay(int evLayer)
	{
		_003CPrecomputeThenPlay_003Ed__177 stateMachine = new _003CPrecomputeThenPlay_003Ed__177(0);
		stateMachine._003C_003E4__this = this;
		stateMachine.evLayer = evLayer;
		return stateMachine;
	}

	private void BindDynFrame(LayerPlaybackData l)
	{
		Vector3 dir = ((l.direction.sqrMagnitude > 1E-06f) ? l.direction.normalized : Vector3.forward);
		Vector3 n = ((l.surfaceNormal.sqrMagnitude > 1E-06f) ? l.surfaceNormal.normalized : (-dir));
		if (Vector3.Dot(n, dir) > 0f)
		{
			n = -n;
		}
		Vector3 axis = n - dir * Vector3.Dot(n, dir);
		if (axis.sqrMagnitude > 1E-08f)
		{
			dynRot = Quaternion.LookRotation(dir, Vector3.Cross(dir, axis.normalized));
		}
		else
		{
			dynRot = Quaternion.LookRotation(dir, Vector3.up);
		}
		dynAnchor = l.entryPoint;
	}

	private bool RenderHybridForSegment(PlaybackSegment seg, float k)
	{
		if (advanced && dyn != null && seg != null && seg.layerIndex == dynLayerIndex && dyn.frames.Count != 0)
		{
			float k2 = Mathf.Clamp01(k);
			float t = dyn.EventDuration * k2;
			if (!dyn.SampleFrame(t, dynCenters, dynAlive, dynDrop))
			{
				return false;
			}
			for (int i = 0; i < dyn.chunkEvents.Count; i++)
			{
				if (!spawnedChunks.Contains(i) && dyn.chunkEvents[i].t <= t)
				{
					SpawnDynChunk(dyn.chunkEvents[i]);
					spawnedChunks.Add(i);
				}
			}
			deform.SetActive(false);
			hybrid.SetActive(true);
			Transform tr = plan.projectile.transform;
			float axisScale = Mathf.Abs(baseScale[scaleAxis]);
			float ratio = ((axisScale <= 1E-09f) ? 1f : (tr.localScale[scaleAxis] / baseScale[scaleAxis]));
			Vector3 nose = tr.position + currentDir * (ratio * baseVisualLen * 0.5f);
			float[] rest = DynRestCoords();
			Vector3 anchorLive = Vector3.zero;
			for (int j = 0; j < dyn.RingCount; j++)
			{
				if (dynAlive[j] && rest[j] >= dyn.LastSampledErodedM)
				{
					anchorLive = dynCenters[j];
					break;
				}
			}
			Vector3 rotated = dynRot * anchorLive;
			dynAnchorLive = nose - rotated;
			dynAnchorLiveValid = true;
			Material mat = MaterialLibrary.Core(plan.projectile.coreType);
			Func<float, float> radiusAtW = (float aM) => deform.SectionRadiusW(aM);
			Vector3 planePoint = dyn.PlanePointAt(t);
			hybrid.Render(base.transform, mat, dynAnchorLive, dynRot, ratio, dynCenters, dynAlive, DynRestCoords(), radiusAtW, planePoint, dyn.PlaneNormal, dynDrop, dyn.RadiusM, dyn.LastSampledErodedM);
			return true;
		}
		return false;
	}

	private float[] DynRestCoords()
	{
		int ringCount = dyn.RingCount;
		if (dynRestCache != null && dynRestCache.Length == ringCount)
		{
			return dynRestCache;
		}
		dynRestCache = new float[ringCount];
		for (int i = 0; i < ringCount; i++)
		{
			dynRestCache[i] = dyn.RingRestCoord(i);
		}
		return dynRestCache;
	}

	private void SpawnDynChunk(RodDynamicsSolver.ChunkEvent ce)
	{
		if (FragmentSpawner.Instance == null)
		{
			return;
		}
		Vector3 anchor = (dynAnchorLiveValid ? dynAnchorLive : dynAnchor);
		Vector3 pos = anchor + dynRot * ce.posCm;
		Vector3 vel = dynRot * ce.vCm;
		Vector3 dir = ((vel.sqrMagnitude > 1E-06f) ? vel.normalized : (dynRot * Vector3.forward));
		float dM = plan.diameterMM / 1000f;
		float penArea = dM * (Mathf.PI / 4f) * dM;
		FragmentSpawner.Instance.SpawnRodDebris(pos, dir, vel.magnitude, ce.massKg, penArea, ce.lengthM, ce.avgRadiusM * 2f, coreColor, ce.omega.magnitude, null, MaterialLibrary.Core(plan.projectile.coreType));
	}

	private void EmitBackjet(Vector3 noseW, Vector3 forward, Vector3 surfТ, float vMps, float curLenM)
	{
		if (FragmentSpawner.Instance == null)
		{
			return;
		}
		if (lastChipLenM < 0f)
		{
			lastChipLenM = vMps;
			return;
		}
		float chipLen = lastChipLenM - vMps;
		lastChipLenM = vMps;
		if (chipLen <= 1E-06f)
		{
			return;
		}
		float dM = plan.diameterMM / 1000f;
		float density = plan.projectile.CoreDensity();
		float mass = density * (dM * (Mathf.PI / 4f) * dM) * chipLen;
		FragmentSpawner.Instance.SpawnBackjetChips(noseW, forward, surfТ, vMps, mass, plan.projectile.coreType, dM);
	}

	private void SpawnRodDebris(Vector3 pos, Vector3 dir, float speedMps, float lengthM, float spinMag, int sliversOverride = -1)
	{
		if (FragmentSpawner.Instance == null || plan == null || lengthM <= 1E-04f)
		{
			return;
		}
		Vector3 dirN = ((dir.sqrMagnitude > 1E-12f) ? dir.normalized : Vector3.forward);
		int slivers = ((sliversOverride > 0) ? sliversOverride : debrisSlivers);
		if (slivers < 1)
		{
			slivers = 1;
		}
		float dM = plan.diameterMM / 1000f;
		float sliverArea = dM * (Mathf.PI / 4f) * dM / slivers;
		float sliverMass = plan.projectile.CoreDensity() * sliverArea * lengthM;
		float sliverLength = lengthM / Mathf.Max(plan.L0M, 1E-06f) * baseVisualLen;
		float sliverCross = rodCrossWorld / Mathf.Sqrt(slivers);
		Collider collider = plan.projectile.GetComponent<Collider>();
		Vector3 side = Vector3.Cross(dirN, Vector3.up);
		if (side.sqrMagnitude < 1E-06f)
		{
			side = Vector3.Cross(dirN, Vector3.left);
		}
		Vector3 sideN = side.normalized;
		Vector3 axis2 = Vector3.Cross(dirN, sideN).normalized;
		float halfCross = rodCrossWorld * 0.5f;
		for (int i = 0; i < slivers; i++)
		{
			float a;
			float b;
			if (slivers > 1)
			{
				float ang = (float)i * Mathf.PI * 2f / slivers;
				a = Mathf.Sin(ang) * 0.5f;
				b = Mathf.Cos(ang) * 0.5f;
			}
			else
			{
				a = 0f;
				b = 1f;
			}
			Vector3 offset = a * sideN + b * axis2;
			Vector3 sliverPos = pos + offset * halfCross;
			Vector3 sliverVel = (dirN + offset * debrisSliverSpread).normalized;
			FragmentSpawner.Instance.SpawnRodDebris(sliverPos, sliverVel, speedMps, sliverMass, sliverArea, sliverLength, sliverCross, coreColor, spinMag, collider, MaterialLibrary.Core(plan.projectile.coreType));
		}
	}

	private void BreakOffFront(LayerPlaybackData l, Vector3 hinge, Vector3 alongPlaneDir, float speedMps, int slivers = -1)
	{
		float erodedM = Mathf.Max(l.entryLengthM - l.exitLengthM, 0f);
		if (erodedM <= 1E-04f)
		{
			return;
		}
		Vector3 dirN = ((alongPlaneDir.sqrMagnitude > 1E-10f) ? alongPlaneDir.normalized : l.exitDirection);
		Vector3 nose = hinge + dirN * (erodedM / Mathf.Max(plan.L0M, 1E-06f) * baseVisualLen * 0.5f);
		SpawnRodDebris(nose, dirN, speedMps, erodedM, 8f, slivers);
	}

	private void SpawnTailBreak(LayerPlaybackData l)
	{
		if (l.tailBreakLengthM <= 1E-04f || plan == null)
		{
			return;
		}
		Vector3 tail = l.entryPoint - l.direction * (l.entryLengthM / Mathf.Max(plan.L0M, 1E-06f) * baseVisualLen);
		Vector3 side = Vector3.Cross(l.direction, Vector3.up);
		if (side.sqrMagnitude < 1E-06f)
		{
			side = Vector3.Cross(l.direction, Vector3.left);
		}
		Vector3 sideN = side.normalized;
		Vector3 vel = (l.direction + sideN * UnityEngine.Random.Range(-0.2f, 0.2f)).normalized;
		Vector3 pos = tail - l.direction * (l.tailBreakLengthM / Mathf.Max(plan.L0M, 1E-06f) * baseVisualLen * 0.5f);
		SpawnRodDebris(pos, vel, l.entryVelocity, l.tailBreakLengthM, 16f);
	}

	private void PaintPlate(ArmorPlate plate, Color c)
	{
		if (plate == null)
		{
			return;
		}
		plate.SetDisplayColor(c);
	}

	public PenetrationPlayer()
	{
		playbackDuration = 3f;
		ricochetContactSeconds = 0.5f;
		slideBendMaxDeg = 70f;
		ricochetBendCoeff = 0.4f;
		ricochetBendMinDeg = 5f;
		brittleSplitPieces = 3;
		lastChipLenM = -1f;
		bendDirWorld = Vector3.up;
		debrisSlivers = 4;
		debrisSliverSpread = 0.3f;
		jetDebugEveryNElements = 25;
		minLayerSeconds = 0.5f;
		spallLayerIndex = -1;
		coreColor = Color.white;
		finStripAxialFraction = 0.25f;
		dynLayerIndex = -1;
		spawnedChunks = new HashSet<int>();
		lastTimeDilation = 1f;
		finMaxLayer = -1;
		spallReportedLayer = -1;
	}
}
