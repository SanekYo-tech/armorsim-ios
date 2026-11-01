using System;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using System.Collections.Generic;
using UnityEngine;

public class FragmentSpawner : MonoBehaviour
{
	public static FragmentSpawner Instance;

	[Header("Пробка (сноп осколков)")]
	public float plugSpreadTilt;

	public const int NormalModeActiveFragmentCap = 1600;

	private bool capBiteReported;

	[Header("Разлёт (§2 — по нормали тыла)")]
	public float coneHalfAngleDeg;

	[Range(0f, 0.5f)]
	public float backSplashFraction;

	[Header("Визуальный масштаб")]
	[Range(0.2f, 1f)]
	public float fragmentSpeedShowMult;

	public float minFragmentWorld;

	public float baseFragmentLifetime;

	public float lifeJitter;

	[Header("Осаждение (§5)")]
	[Range(0f, 1f)]
	public float settledRemovalFraction;

	[Range(0f, 1f)]
	public float ceramicRemovalFraction;

	[Range(0f, 1f)]
	public float compositeRemovalFraction;

	public float settleSpeedMps;

	[Header("Керамика")]
	[Range(0f, 1f)]
	public float ceramicFragmentFactor;

	[Range(1f, 4f)]
	public float compositeFragmentFactor;

	[Header("Стружка back-jet (§1 — сток эрозии, чистый визуал)")]
	public int maxChips;

	public int chipsPerStepCap;

	[Range(0.05f, 0.6f)]
	public float chipSpeedFactor;

	public float chipLifetime;

	[Range(0.05f, 0.4f)]
	public float chipSizeCalibers;

	[Header("Гравитация в slow-mo (§3, вариант B)")]
	public float baseGravityShowScale;

	public float referenceShowDuration;

	[Header("Вторичное пробитие (§4)")]
	public int secondaryMin;

	public int secondaryMax;

	public float secondaryConeDeg;

	public const float VisualSizeScale = 1f;

	private List<GameObject> activeFragments;

	private List<GameObject> settledFragments;

	private List<GameObject> rodDebris;

	private List<GameObject> chips;

	private Mesh chipMesh;

	private PhysicsMaterial bounceMat;

	private Mesh finBladeMesh;

	private Mesh[] splinterPool;

	private Mesh[] bumpyPool;

	public const float CeramicShownFraction = 0.8f;

	public static readonly float MinFragmentWorld;

	public int jetSprayCount;

	public float jetSprayConeDeg;

	public float jetSpraySpeedFracMin;

	public float jetSpraySpeedFracMax;

	public int FreeBudget
	{
		get
		{
			int num = Budget();
			if (num == int.MaxValue)
			{
				return -1;
			}
			return num;
		}
	}

	public int LiveActive => ActiveCount();

	public int LiveSettled
	{
		get
		{
			settledFragments.RemoveAll((GameObject f) => f == null);
			return settledFragments.Count;
		}
	}

	public static bool IsFragmentSource(ArmorPlate.ObjectKind kind)
	{
		return kind != ArmorPlate.ObjectKind.Nera;
	}

	public static bool IsFragmentSource(ArmorPlate plate)
	{
		if (plate == null)
		{
			return true;
		}
		return plate.kind != ArmorPlate.ObjectKind.Nera;
	}

	public static int CapRemaining(SimMode mode, int cap, int activeNow)
	{
		if (mode != SimMode.Normal || cap <= 0)
		{
			return int.MaxValue;
		}
		int num = cap - activeNow;
		if (num < 0)
		{
			return 0;
		}
		return num;
	}

	private int Budget()
	{
		if (AppFlow.ActiveMode != SimMode.Normal)
		{
			return int.MaxValue;
		}
		int num = NormalModeActiveFragmentCap - ActiveCount();
		if (num < 0)
		{
			return 0;
		}
		return num;
	}

	private void Awake()
	{
		Instance = this;
		bounceMat = new PhysicsMaterial("FragmentBounce");
		bounceMat.bounciness = 0.35f;
		bounceMat.dynamicFriction = 0.4f;
		bounceMat.staticFriction = 0.4f;
		bounceMat.bounceCombine = PhysicsMaterialCombine.Average;
		bounceMat.frictionCombine = PhysicsMaterialCombine.Average;
	}

	public void ClearAll()
	{
		foreach (GameObject activeFragment in activeFragments)
		{
			if (activeFragment != null)
			{
				Object.Destroy(activeFragment);
			}
		}
		activeFragments.Clear();
		foreach (GameObject settledFragment in settledFragments)
		{
			if (settledFragment != null)
			{
				Object.Destroy(settledFragment);
			}
		}
		settledFragments.Clear();
		foreach (GameObject rodDebrum in rodDebris)
		{
			if (rodDebrum != null)
			{
				Object.Destroy(rodDebrum);
			}
		}
		rodDebris.Clear();
		foreach (GameObject chip in chips)
		{
			if (chip != null)
			{
				Object.Destroy(chip);
			}
		}
		chips.Clear();
		capBiteReported = false;
	}

	private int ChipCount()
	{
		chips.RemoveAll((GameObject c) => c == null);
		return chips.Count;
	}

	public void SpawnBackjetChips(Vector3 edgeCenter, Vector3 rodForward, Vector3 surfaceTangent, float rodVmps, float erodedMassKg, Projectile.CoreType coreType, float caliberM)
	{
		if (erodedMassKg <= 1E-09f)
		{
			return;
		}
		if (caliberM <= 1E-06f)
		{
			return;
		}
		Vector3 rodDir = ((rodForward.sqrMagnitude > 1E-10f) ? rodForward.normalized : Vector3.forward);
		float density = CoreDensity(coreType);
		caliberM *= chipSizeCalibers;
		float chipMass = caliberM * 0.4f * caliberM * (caliberM * 0.25f) * density;
		if (chipMass <= 1E-09f)
		{
			chipMass = 1E-09f;
		}
		int count = (int)Math.Floor(erodedMassKg / chipMass);
		int room = maxChips - ChipCount();
		if (count > chipsPerStepCap)
		{
			count = chipsPerStepCap;
		}
		if (count > room)
		{
			count = room;
		}
		if (count <= 0)
		{
			return;
		}
		Vector3 gravity = GravityWorld();
		float velocityScale = EffVelocityScale();
		Material mat = MaterialLibrary.CoreFreshCut(coreType);
		Vector3 back = -rodDir;
		Vector3 side = Vector3.Cross(rodDir, Vector3.up);
		if (side.sqrMagnitude < 1E-06f)
		{
			side = Vector3.Cross(rodDir, Vector3.right);
		}
		side = side.normalized;
		Vector3 tangent = Vector3.zero;
		if (surfaceTangent.sqrMagnitude > 1E-10f)
		{
			tangent = Vector3.ProjectOnPlane(surfaceTangent, rodDir).normalized;
		}
		Vector3 fwd = Vector3.Cross(rodDir, side).normalized;
		float offset = caliberM * 0.4f;
		for (int i = 0; i < count; i++)
		{
			GameObject chip = MakeChipObject(mat, caliberM);
			float angle = Random.Range(0f, Mathf.PI * 2f);
			Vector3 ringDir = side * Mathf.Cos(angle) + fwd * Mathf.Sin(angle);
			Vector3 chipDir = back * Random.Range(0.5f, 1f) + ringDir * Random.Range(0.3f, 0.9f) + tangent * Random.Range(0f, 0.8f);
			chip.transform.position = edgeCenter + chipDir * offset;
			chip.transform.rotation = Quaternion.LookRotation(chipDir.normalized, Vector3.up);
			Chip chip2 = chip.AddComponent<Chip>();
			float speed = CapSpeed(Random.Range(0.6f, 1.2f) * (chipSpeedFactor * rodVmps));
			chip2.Init(chipDir.normalized * (speed * velocityScale), gravity, Random.Range(-0.3f, 0.3f) + chipLifetime);
			chips.Add(chip);
		}
	}

	private GameObject MakeChipObject(Material mat, float sizeWorld)
	{
		if (chipMesh == null)
		{
			GameObject gameObject = GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Sphere);
			MeshFilter component = gameObject.GetComponent<MeshFilter>();
			chipMesh = component.sharedMesh;
			UnityEngine.Object.Destroy(gameObject);
		}
		GameObject gameObject2 = new GameObject("Chip");
		MeshFilter meshFilter = gameObject2.AddComponent<MeshFilter>();
		meshFilter.sharedMesh = chipMesh;
		MeshRenderer meshRenderer = gameObject2.AddComponent<MeshRenderer>();
		meshRenderer.sharedMaterial = mat;
		meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		gameObject2.transform.localScale = new Vector3(sizeWorld * 0.4f, sizeWorld * 0.25f, sizeWorld);
		return gameObject2;
	}

	public static bool UsesSplinter(ArmorPlate.ArmorType t)
	{
		return CraterModel.RimClassOf(t) == CraterModel.RimClass.Composite;
	}

	public static Vector3 SplinterScale(float world)
	{
		return new Vector3(world * 0.55f, world * 0.3f, world * 2.6f);
	}

	private Mesh SplinterShared(int seed)
	{
		if (splinterPool == null)
		{
			splinterPool = new Mesh[8];
			for (int i = 0; i < splinterPool.Length; i++)
			{
				splinterPool[i] = MeshShapes.Splinter(i + 2000);
			}
		}
		return splinterPool[Math.Abs(seed) % splinterPool.Length];
	}

	private Mesh BumpyChunkShared(int seed)
	{
		if (bumpyPool == null)
		{
			bumpyPool = new Mesh[8];
			for (int i = 0; i < bumpyPool.Length; i++)
			{
				bumpyPool[i] = MeshShapes.BumpyChunk(i + 1000);
			}
		}
		return bumpyPool[Math.Abs(seed) % bumpyPool.Length];
	}

	private static float CoreDensity(Projectile.CoreType t)
	{
		if (t == Projectile.CoreType.DU)
		{
			return TateModel.DU().density;
		}
		if (t == Projectile.CoreType.Steel)
		{
			return TateModel.Steel().density;
		}
		if (t == Projectile.CoreType.WC)
		{
			return TateModel.WC().density;
		}
		return TateModel.WHA().density;
	}

	private int ActiveCount()
	{
		activeFragments.RemoveAll((GameObject f) => f == null);
		return activeFragments.Count;
	}

	public void OnFragmentSettled(GameObject go)
	{
		activeFragments.Remove(go);
		settledFragments.Add(go);
	}

	public void OnFragmentVanished(GameObject go)
	{
		activeFragments.Remove(go);
		settledFragments.Remove(go);
	}

	public float EffVelocityScale()
	{
		float num = ((PenetrationPlayer.Instance != null) ? PenetrationPlayer.Instance.CurrentTimeDilation : 1f);
		if (num < 1E-06f)
		{
			num = 1E-06f;
		}
		return fragmentSpeedShowMult / num;
	}

	public float SpeedCapMps()
	{
		if (PenetrationPlayer.Instance != null && PenetrationPlayer.Instance.CurrentRodSpeedMps > 0.001f)
		{
			return PenetrationPlayer.Instance.CurrentRodSpeedMps;
		}
		return float.PositiveInfinity;
	}

	private float CapSpeed(float mps)
	{
		float num = SpeedCapMps();
		if (mps > num)
		{
			return num;
		}
		return mps;
	}

	public Vector3 GravityWorldPublic()
	{
		return GravityWorld();
	}

	private Vector3 GravityWorld()
	{
		float num = ((PenetrationPlayer.Instance != null) ? PenetrationPlayer.Instance.CurrentTimeDilation : 1f);
		if (num < 1E-06f)
		{
			num = 1E-06f;
		}
		return Physics.gravity * (baseGravityShowScale / (num * num));
	}

	private Vector3 RandomInCone(Vector3 axis, float halfDeg)
	{
		Vector3 vector = ((axis.sqrMagnitude > 1E-12f) ? axis.normalized : Vector3.forward);
		if (halfDeg <= 0.01f)
		{
			return vector;
		}
		float num = Random.Range(0f, halfDeg) * 0.0174532924f;
		float num2 = Random.Range(0f, 360f) * 0.0174532924f;
		Vector3 vector2 = Vector3.Cross(vector, Vector3.up);
		if (vector2.sqrMagnitude < 1E-06f)
		{
			vector2 = Vector3.Cross(vector, Vector3.right);
		}
		Vector3 vector3 = vector2.normalized;
		Vector3 vector4 = Vector3.Cross(vector, vector3);
		float num3 = Mathf.Cos(num2);
		float num4 = Mathf.Sin(num2);
		float num5 = Mathf.Cos(num);
		float num6 = Mathf.Sin(num);
		Vector3 vector5 = (vector4 * num4 + vector3 * num3) * num6 + vector * num5;
		return vector5.normalized;
	}

	public int SpallTotalCount(ArmorPlate plate, float channelRadiusM, float channelLengthM)
	{
		float num = ((channelLengthM >= 0f) ? channelLengthM : 0f);
		float channelVolumeM = channelRadiusM * 3.14159274f * channelRadiusM * num;
		return SpallTotalCountForVolume(plate, channelVolumeM, fromJet: false);
	}

	public static bool CeramicThinningApplies(SimMode mode)
	{
		if (mode == SimMode.Normal)
		{
			return true;
		}
		return mode == SimMode.Advanced;
	}

	public static float FragmentDisplayMultiplier(ArmorPlate.ArmorType t, SimMode mode)
	{
		if (CraterModel.RimClassOf(t) == CraterModel.RimClass.Ceramic)
		{
			if (mode == SimMode.Normal || mode == SimMode.Advanced)
			{
				return 0.8f;
			}
			return 1f;
		}
		if (t == ArmorPlate.ArmorType.Textolite)
		{
			return 0.7f;
		}
		return 1f;
	}

	public static bool JetSpallSuppressed(ArmorPlate.ArmorType t)
	{
		return CraterModel.RimClassOf(t) == CraterModel.RimClass.Composite;
	}

	public int SpallTotalCountForVolume(ArmorPlate plate, float channelVolumeM3, bool fromJet)
	{
		if (plate == null)
		{
			return 0;
		}
		if (fromJet && CraterModel.RimClassOf(plate.armorType) == CraterModel.RimClass.Composite)
		{
			Debug.Log(string.Format("    осколков нет [{0} × струя]: слоистый пакет расслаивается, ", plate.armorType) + "а не выбрасывает куски — бюджет слоя 0 (гаснут и микро-осколки). Множитель показа 0.70 остаётся в силе на стержневом пути.");
			return 0;
		}
		return SpallTotalCountForVolume(plate, channelVolumeM3);
	}

	public int SpallTotalCountForVolume(ArmorPlate plate, float channelVolumeM3)
	{
		if (plate == null)
		{
			return 0;
		}
		int grady = SpallGradyCount(plate, channelVolumeM3);
		SimMode mode = AppFlow.ActiveMode;
		float showMult = FragmentDisplayMultiplier(plate.armorType, mode);
		if (showMult >= 1f)
		{
			return grady;
		}
		int shown = (int)((float)grady * showMult);
		Debug.Log(string.Format("    показ [{0}, режим {1}]: ×{2:F2} VISUAL_TUNING ⇒ N {3} → {4}. ", plate.armorType, mode, showMult, grady, shown) + "Модельное число не тронуто — его отдаёт SpallGradyCount, множитель живёт снаружи модели Грейди.");
		return shown;
	}

	public int SpallGradyCount(ArmorPlate plate, float channelVolumeM3)
	{
		if (plate == null)
		{
			return 0;
		}
		GradyModel.FragMaterial material = GradyModel.ForArmor(plate.armorType);
		float sizeM = GradyModel.CharacteristicSize(material, GradyModel.StrainRate);
		int count = GradyModel.FragmentCountForVolume(sizeM, channelVolumeM3);
		float drawSizeM = Mathf.Max(sizeM, minFragmentWorld);
		float volumeM3 = Mathf.Max(channelVolumeM3, 0f);
		int volumeCap = Math.Max(1, (int)Math.Floor(volumeM3 / Mathf.Max(drawSizeM * drawSizeM * drawSizeM, 1E-15f)));
		int result = Math.Min(count, volumeCap);
		if (plate.armorType == ArmorPlate.ArmorType.Ceramic)
		{
			result = (int)((float)result * ceramicFragmentFactor);
		}
		if (CraterModel.RimClassOf(plate.armorType) == CraterModel.RimClass.Composite)
		{
			result = (int)((float)result * compositeFragmentFactor);
		}
		Debug.Log(string.Format("Осколки [{0}]: ветвь={1}, s={2:F1} мм | N_физ={3}, ", material.name, GradyModel.EffectiveBranch(material, GradyModel.StrainRate), sizeM * 1000f, count) + string.Format("N по объёму канала={0} (рисуемый кусок {1:F1} мм, ", volumeCap, drawSizeM * 1000f) + string.Format("канал {0:F1} см³) → N={1}{2}", volumeM3 * 1000000f, result, (volumeCap < count) ? " ← ограничен объёмом" : ""));
		return result;
	}

	public int SpawnSpallPortion(Vector3 origin, Vector3 axis, float speedMps, ArmorPlate plate, float channelRadiusM, int count, float coneHalfDeg, bool ignoreSourcePlate)
	{
		return SpawnSpallPortion(origin, axis, speedMps, plate, channelRadiusM, count, coneHalfDeg, ignoreSourcePlate, Vector3.zero, Vector3.zero, 0f);
	}

	public int SpawnSpallPortion(Vector3 origin, Vector3 axis, float speedMps, ArmorPlate plate, float channelRadiusM, int count, float coneHalfDeg, bool ignoreSourcePlate, Vector3 axisPoint, Vector3 axisDir, float backRoomWorld)
	{
		if (plate == null || count <= 0 || !IsFragmentSource(plate))
		{
			return 0;
		}
		int budget = Budget();
		if (budget <= count)
		{
			if (budget < count && !capBiteReported)
			{
				capBiteReported = true;
				Debug.LogWarning(string.Format("◈ ПОТОЛОК ОСКОЛКОВ СРАБОТАЛ (обычный режим, {0} ", NormalModeActiveFragmentCap) + string.Format("одновременно летящих): просили {0}, дали {1}, живых сейчас ", count, budget) + string.Format("{0}. Дальше по трассе слои будут получать меньше запрошенного, ", ActiveCount()) + "пока время жизни не освободит места — это ИЗВЕСТНОЕ следствие потолка (замер при 600: слои получали 513/0/1/0/171/311), а не потеря осколков. Уже летящее не удаляется намеренно.");
			}
			count = budget;
			if (budget < 1)
			{
				return 0;
			}
		}
		GradyModel.FragMaterial material = GradyModel.ForArmor(plate.armorType);
		float sizeM = GradyModel.CharacteristicSize(material, GradyModel.StrainRate);
		Color color = PlateColor(plate);
		Vector3 gravity = GravityWorld();
		Collider ignoreCollider = (!ignoreSourcePlate) ? null : plate.GetComponent<Collider>();
		float[] sizes = new float[count];
		for (int i = 0; i < count; i++)
		{
			sizes[i] = Random.Range(0.5f, 1.5f) * sizeM;
		}
		Array.Sort(sizes);
		Array.Reverse(sizes);
		for (int j = 0; j < count; j++)
		{
			float size = sizes[j];
			Vector3 fragDir = RandomInCone(axis, coneHalfDeg);
			float massFrac = Mathf.Clamp01(size / Mathf.Max(sizeM, 1E-05f) - 0.5f);
			float speedFrac = Random.Range(-0.08f, 0.08f) + massFrac * -0.6f + 0.9f;
			speedFrac = Mathf.Clamp(speedFrac, 0.2f, 0.95f);
			Vector3 spawnPos = InsideChannelPoint(origin, axisPoint, axisDir, channelRadiusM, size, Random.value, Random.value, backRoomWorld);
			MakeFragment(spawnPos, size, material.density, speedFrac * speedMps, fragDir, plate.armorType, 0, size >= sizeM, color, gravity, ignoreCollider, keepOnSettle: false, lifetimeMult: 1f, tubePoint: axisPoint, tubeAxis: axisDir, tubeRadiusWorld: channelRadiusM);
		}
		return count;
	}

	public static Vector3 InsideChannelPoint(Vector3 origin, Vector3 axisPoint, Vector3 axisDir, float channelRadiusM, float sizeM, float rFrac, float axialFrac, float backRoomWorld)
	{
		if (axisDir.sqrMagnitude < 1E-12f)
		{
			return origin;
		}
		Vector3 axis = axisDir.normalized;
		channelRadiusM = Mathf.Max(channelRadiusM, 0f);
		sizeM = Mathf.Max(sizeM, MinFragmentWorld);
		Vector3 delta = origin - axisPoint;
		Vector3 perp = delta - axis * Vector3.Dot(axis, delta);
		perp = ((perp.sqrMagnitude > 1E-14f) ? perp.normalized : Vector3.zero);
		float radialRoom = Mathf.Max(channelRadiusM - sizeM * 0.5f, 0f);
		float radialOffset = Mathf.Clamp01(rFrac) * radialRoom;
		float axialOffset = Mathf.Min(channelRadiusM * 0.5f, Mathf.Max(backRoomWorld, 0f));
		return axisPoint + perp * radialOffset - axis * (Mathf.Clamp01(Mathf.Abs(axialFrac)) * axialOffset);
	}

	public void SpawnJetSpray(Vector3 point, Vector3 dir, float jetTipSpeedMps, float jetElementRadiusM, ArmorPlate.ArmorType armorType, Collider ignoreCollider = null)
	{
		int budget = Budget();
		if (budget <= 0)
		{
			return;
		}
		GradyModel.ForArmor(armorType);
		float jetDiameterM = jetElementRadiusM + jetElementRadiusM;
		if (jetDiameterM <= 0.0001f)
		{
			jetDiameterM = 0.0001f;
		}
		int count = Math.Min(jetSprayCount, budget);
		Vector3 gravity = GravityWorld();
		if (count < 1)
		{
			return;
		}
		for (int i = 0; i < count; i++)
		{
			Vector3 sprayDir = RandomInCone(dir, jetSprayConeDeg);
			float speed = CapSpeed(Random.Range(jetSpraySpeedFracMin, jetSpraySpeedFracMax) * jetTipSpeedMps);
			float sizeWorld = Random.Range(0.5f, 1.5f) * jetDiameterM;
			MakeFragment(point, sizeWorld, 8960f, speed, sprayDir, armorType, 1, false, new Color(0.75f, 0.62f, 0.48f, 1f), gravity, ignoreCollider);
		}
	}

	public void SpawnSecondary(Vector3 point, Vector3 dir, float vOutMps, ArmorPlate.ArmorType armorType, Collider ignoreCollider = null, float punchedVolumeM3 = -1f)
	{
		int budget = Budget();
		if (budget <= 0)
		{
			return;
		}
		GradyModel.FragMaterial material = GradyModel.ForArmor(armorType);
		float sizeM = GradyModel.CharacteristicSize(material, GradyModel.StrainRate) * 0.5f;
		int count = Math.Min(budget, Random.Range(secondaryMin, secondaryMax + 1));
		if (punchedVolumeM3 > 0f)
		{
			float fragSizeM = Mathf.Max(minFragmentWorld, sizeM);
			float fragVolumeM3 = Mathf.Max(fragSizeM * fragSizeM * fragSizeM, 1E-15f);
			int volumeCap = Math.Max(0, (int)Math.Floor(punchedVolumeM3 / fragVolumeM3));
			if (volumeCap < count)
			{
				count = volumeCap;
			}
			if (count < 1)
			{
				return;
			}
		}
		Vector3 gravity = GravityWorld();
		for (int i = 0; i < count; i++)
		{
			Vector3 fragDir = RandomInCone(dir, secondaryConeDeg);
			float sizeWorld = Random.Range(0.4f, 0.9f) * sizeM;
			float speed = Random.Range(0.7f, 1.1f) * vOutMps * 0.5f;
			MakeFragment(point, sizeWorld, material.density, speed, fragDir, armorType, 1, false, new Color(0.6f, 0.6f, 0.62f, 1f), gravity, ignoreCollider);
		}
	}

	private void MakeFragment(Vector3 pos, float sizeM, float density, float realSpeedMps, Vector3 dir, ArmorPlate.ArmorType armorType, int generation, bool large, Color color, Vector3 gravity, Collider ignoreCollider = null, bool keepOnSettle = false, float lifetimeMult = 1f, Vector3 tubePoint = default(Vector3), Vector3 tubeAxis = default(Vector3), float tubeRadiusWorld = 0f)
	{
		GameObject gameObject = GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Sphere);
		gameObject.name = ((generation != 0) ? "Fragment2" : "Fragment");
		gameObject.transform.position = pos;
		gameObject.transform.rotation = Random.rotation;
		float worldSize = Mathf.Max(minFragmentWorld, sizeM);
		gameObject.transform.localScale = Vector3.one * worldSize;
		if (CraterModel.RimClassOf(armorType) == CraterModel.RimClass.Composite)
		{
			MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
			if (meshFilter != null)
			{
				int seed = Random.Range(1, 99999);
				if (splinterPool == null)
				{
					splinterPool = new Mesh[8];
					for (int i = 0; i < splinterPool.Length; i++)
					{
						splinterPool[i] = MeshShapes.Splinter(i + 2000);
					}
				}
				meshFilter.sharedMesh = splinterPool[Math.Abs(seed) % splinterPool.Length];
			}
			gameObject.transform.localScale = SplinterScale(worldSize);
		}
		else if (large)
		{
			MeshFilter meshFilter2 = gameObject.GetComponent<MeshFilter>();
			if (meshFilter2 != null)
			{
				int seed2 = Random.Range(1, 99999);
				if (bumpyPool == null)
				{
					bumpyPool = new Mesh[8];
					for (int j = 0; j < bumpyPool.Length; j++)
					{
						bumpyPool[j] = MeshShapes.BumpyChunk(j + 1000);
					}
				}
				meshFilter2.sharedMesh = bumpyPool[Math.Abs(seed2) % bumpyPool.Length];
			}
		}
		if (gameObject.GetComponent<Rigidbody>() == null)
		{
			gameObject.AddComponent<Rigidbody>();
		}
		Fragment fragment = gameObject.GetComponent<Fragment>();
		if (fragment == null)
		{
			fragment = gameObject.AddComponent<Fragment>();
		}
		float velocityScale = EffVelocityScale();
		float speed = CapSpeed(realSpeedMps);
		fragment.Init(generation, speed, velocityScale, sizeM, sizeM * density * sizeM * sizeM, armorType, dir, gravity, color, bounceMat, lifetimeMult * baseFragmentLifetime, lifeJitter, velocityScale * settleSpeedMps, ignoreCollider, -1f, keepOnSettle);
		if (tubeAxis.sqrMagnitude > 1E-12f && tubeRadiusWorld > 0f)
		{
			fragment.SetSourceTube(tubePoint, tubeAxis, tubeRadiusWorld);
		}
		activeFragments.Add(gameObject);
	}

	private Color PlateColor(ArmorPlate plate)
	{
		if (plate != null)
		{
			return ArmorPlate.CanonicalColor(plate.armorType);
		}
		return new Color(0.6f, 0.6f, 0.62f, 1f);
	}

	public void SpawnPlugCluster(Vector3 point, Vector3 direction, float diameterWorld, float thicknessWorld, ArmorPlate plate, float realSpeedMps)
	{
		if (plate == null || !IsFragmentSource(plate))
		{
			return;
		}
		Vector3 dir = ((direction.sqrMagnitude > 1E-12f) ? direction.normalized : Vector3.forward);
		float plugRadius = Mathf.Max(diameterWorld * 0.5f, 1E-04f);
		Vector3 side = Vector3.Cross(dir, Vector3.up);
		if (side.sqrMagnitude < 1E-06f)
		{
			side = Vector3.Cross(dir, Vector3.right);
		}
		side = side.normalized;
		Vector3 fwd = Vector3.Cross(dir, side).normalized;
		GradyModel.FragMaterial material = GradyModel.ForArmor(plate.armorType);
		Color color = PlateColor(plate);
		Vector3 gravity = GravityWorld();
		Collider ignoreCollider = plate.GetComponent<Collider>();
		int[] counts = new int[4] { 1, 6, 12, 18 };
		float[] rings = new float[4];
		rings[1] = plugRadius * 0.38f;
		rings[2] = plugRadius * 0.7f;
		rings[3] = plugRadius * 0.95f;
		int ringCount = ((plugRadius > minFragmentWorld * 2.5f) ? 4 : 2);
		float size = Mathf.Max(Mathf.Max(plugRadius * 0.62f, minFragmentWorld * 1.4f), 1E-04f);
		int budget = Budget();
		if (budget < 1)
		{
			return;
		}
		for (int ring = 0; ring < ringCount; ring++)
		{
			int subCount = ((ring != 0) ? 2 : 1);
			for (int sub = 0; sub < subCount; sub++)
			{
				for (int i = 0; i < counts[ring] && budget > 0; i++)
				{
					float angle = ring * 0.4f + (float)i / counts[ring] * Mathf.PI * 2f + sub * 0.25f;
					Vector3 ringDir = side * Mathf.Cos(angle) + fwd * Mathf.Sin(angle);
					float radial = Random.Range(0.9f, 1.05f) * rings[ring];
					Vector3 spawnPos = point + ringDir * radial + dir * (((sub != 0) ? 0.28f : (-0.28f)) * thicknessWorld);
					float radialFrac = ((plugRadius > 1E-06f) ? (radial / plugRadius) : 0f);
					Vector3 dirOut = (dir + ringDir * (radialFrac * plugSpreadTilt)).normalized;
					MakeFragment(spawnPos, size, material.density, Random.Range(0.85f, 1f) * realSpeedMps, dirOut, plate.armorType, 0, large: true, color, gravity, ignoreCollider, keepOnSettle: true, lifetimeMult: 3f);
					budget--;
				}
			}
		}
	}

	public void SpawnFinFragment(Vector3 pos, Vector3 dir, float speedMps, float densityKgM3, float thickM, float heightM, float chordM, Vector3 spanAxis, Color color, Collider ignoreCollider, Material bodyMat = null, Mesh pieceMesh = null, int pieces = 1)
	{
		Vector3 dirN = ((dir.sqrMagnitude > 1E-12f) ? dir.normalized : Vector3.forward);
		if (thickM <= 1E-05f)
		{
			thickM = 1E-05f;
		}
		if (heightM <= 1E-05f)
		{
			heightM = 1E-05f;
		}
		if (chordM <= 1E-05f)
		{
			chordM = 1E-05f;
		}
		int pieceCount = Mathf.Max(1, pieces);
		float pieceFrac = 1f / pieceCount;
		float mass = thickM * densityKgM3 * heightM * chordM * pieceFrac;
		if (mass <= 1E-07f)
		{
			mass = 1E-07f;
		}
		float penArea = chordM * heightM * pieceFrac;
		Mesh mesh = pieceMesh;
		if (mesh == null)
		{
			if (finBladeMesh == null)
			{
				finBladeMesh = MeshShapes.FinBlade(0.55f, 0.18f, 0.4f);
			}
			mesh = finBladeMesh;
		}
		GameObject gameObject = new GameObject("FinFragment", typeof(MeshFilter), typeof(MeshRenderer));
		gameObject.GetComponent<MeshFilter>().sharedMesh = mesh;
		gameObject.transform.position = pos;
		Vector3 spanDir = ((spanAxis.sqrMagnitude > 1E-10f) ? spanAxis.normalized : Vector3.up);
		if (Mathf.Abs(Vector3.Dot(spanDir, dirN)) > 0.999f)
		{
			spanDir = Vector3.Cross(dirN, Vector3.up).normalized;
		}
		gameObject.transform.rotation = Quaternion.LookRotation(dirN, spanDir);
		gameObject.transform.localScale = new Vector3(thickM, heightM, chordM);
		BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
		boxCollider.center = mesh.bounds.center;
		boxCollider.size = mesh.bounds.size;
		boxCollider.material = bounceMat;
		MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
		if (meshRenderer != null)
		{
			if (bodyMat == null)
			{
				Material material = new Material(Shader.Find("Standard"));
				meshRenderer.sharedMaterial = material;
				meshRenderer.sharedMaterial.color = color;
			}
			else
			{
				meshRenderer.sharedMaterial = bodyMat;
			}
		}
		gameObject.AddComponent<Rigidbody>();
		Fragment fragment = gameObject.AddComponent<Fragment>();
		float velocityScale = EffVelocityScale();
		Vector3 gravity = GravityWorld();
		float speed = CapSpeed(speedMps);
		fragment.Init(0, speed, velocityScale, Mathf.Max(chordM, heightM), mass, ArmorPlate.ArmorType.RHA, dirN, gravity, color, bounceMat, baseFragmentLifetime + baseFragmentLifetime, lifeJitter, velocityScale * settleSpeedMps, ignoreCollider, penArea, keepOnSettle: true, 12f);
		rodDebris.Add(gameObject);
	}

	public void SpawnRodDebris(Vector3 pos, Vector3 dir, float speedMps, float massKg, float penAreaM2, float lengthWorld, float crossWorld, Color color, float spinMag, Collider ignoreCollider = null, Material bodyMat = null)
	{
		Vector3 vector = ((dir.sqrMagnitude > 1E-12f) ? dir.normalized : Vector3.left);
		GameObject gameObject = new GameObject("RodDebris");
		gameObject.transform.position = pos;
		gameObject.transform.rotation = Quaternion.FromToRotation(Vector3.forward, vector);
		lengthWorld = Mathf.Max(lengthWorld, 0.001f);
		crossWorld = Mathf.Max(crossWorld, 0.001f);
		gameObject.transform.localScale = new Vector3(crossWorld, lengthWorld * 0.5f, crossWorld);
		MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
		int seed = Random.Range(1, 99999);
		meshFilter.sharedMesh = MeshShapes.RaggedRod(9, 0.25f, 0.12f, seed);
		MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
		CapsuleCollider capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
		capsuleCollider.direction = 1;
		capsuleCollider.radius = 0.5f;
		capsuleCollider.height = 1f;
		capsuleCollider.material = bounceMat;
		if (bodyMat == null)
		{
			Shader shader = Shader.Find("Standard");
			Material material = new Material(shader);
			meshRenderer.sharedMaterial = material;
			meshRenderer.sharedMaterial.color = color;
		}
		else
		{
			meshRenderer.sharedMaterial = bodyMat;
		}
		gameObject.AddComponent<Rigidbody>();
		Fragment fragment = gameObject.AddComponent<Fragment>();
		float velocityScale = EffVelocityScale();
		Vector3 gravity = GravityWorld();
		float speed = CapSpeed(speedMps);
		fragment.Init(0, speed, velocityScale, crossWorld, massKg, ArmorPlate.ArmorType.RHA, vector, gravity, color, bounceMat, baseFragmentLifetime + baseFragmentLifetime, lifeJitter, velocityScale * settleSpeedMps, ignoreCollider, penAreaM2, keepOnSettle: true, spinMag);
		rodDebris.Add(gameObject);
	}
}
