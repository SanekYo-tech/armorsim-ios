using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Fragment : MonoBehaviour
{
	private Rigidbody rb;

	private Collider col;

	private Collider ignoredSource;

	private MeshCollider ignoredSourceMesh;

	private DeformableMesh ignoredSourceDeform;

	private Vector3 tubePoint;

	private Vector3 tubeAxis = Vector3.zero;

	private float tubeRadiusWorld;

	private int generation;

	private float realSpeedMps;

	private float velocityVisualScale;

	private float sizeM;

	private float massKg;

	private ArmorPlate.ArmorType armorType;

	private Vector3 gravityWorld;

	private float penAreaM2 = -1f;

	private bool keepOnSettle;

	private Vector3 lastDir = Vector3.forward;

	private float maxRealSpeedMps = float.PositiveInfinity;

	private float life;

	private float age;

	private float settleSpeedVisual;

	private bool settled;

	private float vanishAtAge = -1f;

	public const float ProjectContactOffsetM = 0.01f;

	public const float EarlyVanishDelayS = 1f;

	public const float CeramicEarlyVanishFraction = 0.3f;

	public static float ContactOffsetFor(float worldRadius)
	{
		return Mathf.Clamp(Mathf.Max(worldRadius, 0f) * 0.25f, 1e-05f, 0.01f);
	}

	public static bool OutsideLocalBox(Vector3 localPos, Vector3 center, Vector3 extents, Vector3 marginLocal)
	{
		Vector3 d = localPos - center;
		if (Mathf.Abs(d.x) > extents.x + marginLocal.x)
		{
			return true;
		}
		if (Mathf.Abs(d.y) > extents.y + marginLocal.y)
		{
			return true;
		}
		return Mathf.Abs(d.z) > extents.z + marginLocal.z;
	}

	public static bool InsideTube(Vector3 pos, Vector3 tubePoint, Vector3 tubeAxis, float tubeRadiusWorld, float fragRadiusWorld)
	{
		if (tubeAxis.sqrMagnitude < 1e-12f)
		{
			return false;
		}
		if (tubeRadiusWorld <= 0f)
		{
			return false;
		}
		Vector3 n = tubeAxis.normalized;
		Vector3 d = pos - tubePoint;
		Vector3 perp = d - Vector3.Dot(d, n) * n;
		return tubeRadiusWorld >= perp.magnitude + fragRadiusWorld;
	}

	public static bool MayRestoreSourceContact(bool holeInCollider, bool insideTube, bool outsideBox)
	{
		return (holeInCollider && insideTube) || outsideBox;
	}

	public static bool SourceContactRestorable(Collider self, Collider source, MeshCollider sourceMesh, DeformableMesh sourceDeform, Vector3 selfPos, float selfRadiusWorld, Vector3 tubePoint, Vector3 tubeAxis, float tubeRadiusWorld)
	{
		if (self == null || source == null)
		{
			return false;
		}
		Transform t = source.transform;
		Vector3 ls = t.lossyScale;
		float sx = Mathf.Max(Mathf.Abs(ls.x), 1e-06f);
		float sy = Mathf.Max(Mathf.Abs(ls.y), 1e-06f);
		float sz = Mathf.Max(Mathf.Abs(ls.z), 1e-06f);
		bool hasMesh = sourceMesh != null && sourceMesh.sharedMesh != null;
		Vector3 center;
		Vector3 extents;
		if (hasMesh)
		{
			Bounds b = sourceMesh.sharedMesh.bounds;
			center = b.center;
			extents = b.extents;
		}
		else
		{
			Bounds b = source.bounds;
			center = Vector3.zero;
			extents = new Vector3(b.extents.x / sx, b.extents.y / sy, b.extents.z / sz);
		}
		float r = self.contactOffset + selfRadiusWorld;
		Vector3 margin = new Vector3(r / sx, r / sy, r / sz);
		Vector3 local = t.InverseTransformPoint(selfPos);
		bool holeInCollider = sourceDeform != null && sourceDeform.ColliderChannelThrough;
		bool insideTube = InsideTube(selfPos, tubePoint, tubeAxis, tubeRadiusWorld, selfRadiusWorld);
		bool outsideBox = OutsideLocalBox(local, center, extents, margin);
		return MayRestoreSourceContact(holeInCollider, insideTube, outsideBox);
	}

	public void Init(int generation, float realSpeedMps, float velocityVisualScale, float sizeM, float massKg, ArmorPlate.ArmorType armorType, Vector3 dir, Vector3 gravityWorld, Color color, PhysicsMaterial bounceMat, float baseLifetime, float lifeJitter, float settleSpeedVisual, Collider ignoreCollider, float penAreaM2 = -1f, bool keepOnSettle = false, float spinMag = 10f)
	{
		this.generation = generation;
		this.realSpeedMps = realSpeedMps;
		this.velocityVisualScale = velocityVisualScale;
		this.sizeM = sizeM;
		this.massKg = massKg;
		this.armorType = armorType;
		this.gravityWorld = gravityWorld;
		this.penAreaM2 = penAreaM2;
		this.keepOnSettle = keepOnSettle;
		this.settleSpeedVisual = settleSpeedVisual;
		rb = GetComponent<Rigidbody>();
		lastDir = ((dir.sqrMagnitude > 1e-12f) ? dir.normalized : Vector3.forward);
		vanishAtAge = ((!keepOnSettle && EarlyVanishFractionFor(armorType, AppFlow.ActiveMode) > UnityEngine.Random.value) ? EarlyVanishDelayS : (-1f));
		rb.useGravity = false;
		rb.mass = Mathf.Max(massKg, 1e-06f);
		rb.linearVelocity = lastDir * (realSpeedMps * velocityVisualScale);
		maxRealSpeedMps = Mathf.Max(realSpeedMps, 1e-06f);
		rb.angularVelocity = UnityEngine.Random.insideUnitSphere * spinMag;
		Renderer rend = GetComponent<Renderer>();
		if (rend != null)
		{
			rend.material.color = color;
		}
		col = GetComponent<Collider>();
		if (col != null)
		{
			col.material = bounceMat;
			rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
		}
		BeginSourceIgnore(ignoreCollider);
		col.contactOffset = ContactOffsetFor(Mathf.Max(transform.lossyScale.x, 1e-05f) * 0.5f);
		rb.maxDepenetrationVelocity = Mathf.Max(Mathf.Sqrt(rb.linearVelocity.sqrMagnitude), 0.001f);
		life = baseLifetime + UnityEngine.Random.Range(-lifeJitter, lifeJitter);
		age = 0f;
		settled = false;
		RenderedSpeedAudit.Register(transform, GetComponent<Renderer>(), (generation != 0) ? "вторичный осколок" : "осколок");
	}

	public void SetSourceTube(Vector3 pointWorld, Vector3 axisWorld, float radiusWorld)
	{
		tubePoint = pointWorld;
		tubeAxis = axisWorld;
		tubeRadiusWorld = radiusWorld;
	}

	private void BeginSourceIgnore(Collider source)
	{
		ignoredSource = null;
		ignoredSourceMesh = null;
		ignoredSourceDeform = null;
		if (source == null)
		{
			return;
		}
		if (col == null)
		{
			return;
		}
		Physics.IgnoreCollision(col, source, true);
		ignoredSource = source;
		ignoredSourceMesh = source as MeshCollider;
		ignoredSourceDeform = source.GetComponent<DeformableMesh>();
	}

	private void ReleaseSourceIgnore()
	{
		if (ignoredSource == null || col == null)
		{
			return;
		}
		Vector3 ls = transform.lossyScale;
		float selfRadiusWorld = Mathf.Max(ls.x, 1e-05f) * 0.5f;
		Vector3 selfPos = col.transform.position;
		if (SourceContactRestorable(col, ignoredSource, ignoredSourceMesh, ignoredSourceDeform, selfPos, selfRadiusWorld, tubePoint, tubeAxis, tubeRadiusWorld))
		{
			Physics.IgnoreCollision(col, ignoredSource, false);
			ignoredSource = null;
			ignoredSourceMesh = null;
			ignoredSourceDeform = null;
		}
	}

	private void FixedUpdate()
	{
		if (rb == null || settled)
		{
			return;
		}
		ReleaseSourceIgnore();
		SyncToShowTimeScale();
		rb.AddForce(gravityWorld, ForceMode.Acceleration);
		ClampToMaxSpeed();
		if (rb.linearVelocity.sqrMagnitude > 1e-08f)
		{
			lastDir = rb.linearVelocity.normalized;
		}
	}

	private void ClampToMaxSpeed()
	{
		if (rb == null || float.IsInfinity(maxRealSpeedMps))
		{
			return;
		}
		float limit = maxRealSpeedMps * velocityVisualScale;
		if (limit <= 0f)
		{
			return;
		}
		Vector3 v = rb.linearVelocity;
		float mag = v.magnitude;
		if (mag <= limit)
		{
			return;
		}
		if (mag <= 1e-09f)
		{
			return;
		}
		rb.linearVelocity = v * (limit / mag);
		realSpeedMps = Mathf.Min(realSpeedMps, maxRealSpeedMps);
	}

	private void SyncToShowTimeScale()
	{
		FragmentSpawner spawner = FragmentSpawner.Instance;
		if (spawner == null)
		{
			return;
		}
		if (velocityVisualScale <= 1e-12f)
		{
			return;
		}
		float eff = spawner.EffVelocityScale();
		if (Mathf.Abs(eff - velocityVisualScale) < 1e-09f)
		{
			return;
		}
		float k = Mathf.Clamp(eff / velocityVisualScale, 0.5f, 1f);
		rb.linearVelocity = rb.linearVelocity * k;
		settleSpeedVisual = k * settleSpeedVisual;
		gravityWorld = spawner.GravityWorldPublic();
		velocityVisualScale = k * velocityVisualScale;
	}

	public static float EarlyVanishFractionFor(ArmorPlate.ArmorType t, SimMode mode)
	{
		if (CraterModel.RimClassOf(t) == CraterModel.RimClass.Ceramic && (mode == SimMode.Normal || mode == SimMode.Advanced))
		{
			return CeramicEarlyVanishFraction;
		}
		return 0f;
	}

	private void Update()
	{
		age += Time.deltaTime;
		if (vanishAtAge > 0f && age >= vanishAtAge)
		{
			Vanish();
			return;
		}
		if (settled)
		{
			return;
		}
		if (age > 0.5f && rb != null && settleSpeedVisual * settleSpeedVisual > rb.linearVelocity.sqrMagnitude)
		{
			Settle();
		}
		else if (age >= life)
		{
			Settle();
		}
	}

	private void Vanish()
	{
		FragmentSpawner spawner = FragmentSpawner.Instance;
		if (spawner != null)
		{
			GameObject go = gameObject;
			spawner.OnFragmentVanished(go);
		}
		Destroy(gameObject);
	}

	public static float RemovalFractionFor(ArmorPlate.ArmorType t)
	{
		if (CraterModel.RimClassOf(t) == CraterModel.RimClass.Ceramic)
		{
			FragmentSpawner spawner = FragmentSpawner.Instance;
			if (spawner != null)
			{
				return spawner.ceramicRemovalFraction;
			}
			return 0.22f;
		}
		if (CraterModel.RimClassOf(t) == CraterModel.RimClass.Composite)
		{
			FragmentSpawner spawner = FragmentSpawner.Instance;
			if (spawner != null)
			{
				return spawner.compositeRemovalFraction;
			}
			return 0.3f;
		}
		return 0f;
	}

	private void Settle()
	{
		settled = true;
		if (!keepOnSettle)
		{
			float frac = RemovalFractionFor(armorType);
			if (frac > 0f && frac > UnityEngine.Random.value)
			{
				Destroy(gameObject);
				return;
			}
		}
		if (rb != null)
		{
			rb.linearVelocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;
			rb.Sleep();
		}
		if (!keepOnSettle)
		{
			FragmentSpawner spawner = FragmentSpawner.Instance;
			if (spawner != null)
			{
				GameObject go = gameObject;
				spawner.OnFragmentSettled(go);
			}
		}
	}

	private void OnCollisionEnter(Collision c)
	{
		if (settled || col == null)
		{
			return;
		}
		Collider hit = c.collider;
		ArmorPlate plate = ((hit != null) ? hit.GetComponent<ArmorPlate>() : null);
		if (plate == null)
		{
			return;
		}
		ContactPoint contact = c.GetContact(0);
		Vector3 dir = lastDir;
		Vector3 dirN = lastDir.normalized;
		Vector3 normal = contact.normal.normalized;
		float obliqRad = Mathf.Acos(Mathf.Clamp(Mathf.Abs(Vector3.Dot(normal, dirN)), 0.001f, 1f));
		float area = ((penAreaM2 > 0f) ? penAreaM2 : (sizeM * sizeM));
		float rFrag = ((area > 0f) ? Mathf.Sqrt(area * 4f / Mathf.PI) : 0f);
		if (ShotRouting.Fires(ShotBranch.SpallLinerCatch, plate.kind, ThreatClass.Fragment))
		{
			HitSpallLiner(plate, c, contact, dir, rFrag, obliqRad);
			return;
		}
		if (ShotRouting.Fires(ShotBranch.CrewRegistration, plate.kind, ThreatClass.Fragment))
		{
			HitCrew(plate, c, contact, dir, area);
			return;
		}
		HitBarrier(plate, c, contact, dir, rFrag, obliqRad);
	}

	private void HitSpallLiner(ArmorPlate liner, Collision c, ContactPoint contact, Vector3 dir, float dFrag, float obliqRad)
	{
		SpallLinerCatch cath = SpallLinerModel.Resolve(liner.linerMaterial, liner.LinerThicknessM, massKg, realSpeedMps, dFrag, obliqRad);
		if (!cath.caught)
		{
			Physics.IgnoreCollision(col, c.collider, true);
			float dia = Mathf.Max(transform.localScale.x, SceneScale.RodDiameterWorld(2f));
			transform.position = contact.point + dir * dia;
			realSpeedMps = cath.residualVelocityMS;
			if (rb != null)
			{
				rb.linearVelocity = dir * (velocityVisualScale * realSpeedMps);
			}
		}
		else
		{
			realSpeedMps = 0f;
			if (rb != null)
			{
				rb.linearVelocity = Vector3.zero;
			}
			transform.position = contact.point;
			Settle();
		}
	}

	private void HitCrew(ArmorPlate figure, Collision c, ContactPoint contact, Vector3 dir, float areaM2)
	{
		Vector3 localPoint = figure.transform.InverseTransformPoint(contact.point);
		Vector3 localDir = figure.transform.InverseTransformVector(dir);
		CrewTraceResult r = CrewAnatomy.Trace(localPoint, localDir, figure.widthM, figure.heightM, figure.thicknessMM, massKg, realSpeedMps, areaM2);
		figure.RegisterCrewTrace(r);
		if (!r.hitBody)
		{
			if (c != null)
			{
				Physics.IgnoreCollision(col, c.collider, true);
			}
			return;
		}
		if (c != null)
		{
			Physics.IgnoreCollision(col, c.collider, true);
		}
		Vector3 exit = figure.transform.TransformPoint(r.endLocal);
		if (r.stopped)
		{
			realSpeedMps = 0f;
			if (rb != null)
			{
				rb.linearVelocity = Vector3.zero;
			}
			transform.position = exit;
			Settle();
		}
		else
		{
			float dia = Mathf.Max(transform.localScale.x, SceneScale.RodDiameterWorld(2f));
			transform.position = exit + dir * dia;
			realSpeedMps = r.exitSpeedMps;
			if (rb != null)
			{
				rb.linearVelocity = dir * (velocityVisualScale * realSpeedMps);
			}
		}
	}

	private void HitBarrier(ArmorPlate plate, Collision c, ContactPoint contact, Vector3 dir, float dFrag, float obliqRad)
	{
		float tM = plate.SubstanceThicknessMM(plate.thicknessMM / Mathf.Max(Mathf.Cos(obliqRad), 0.001f)) / 1000f;
		if (generation != 0)
		{
			if (ShotRouting.Fires(ShotBranch.EngineDamage, plate.kind, ThreatClass.Fragment))
			{
				plate.RegisterEngineFragment(massKg * 0.5f * realSpeedMps * realSpeedMps);
			}
			realSpeedMps *= 0.6f;
			return;
		}
		FragmentPenetration pen = FragmentBallistics.PlugShear(massKg, realSpeedMps, dFrag, tM, FragmentBallistics.YieldStrengthPa(plate.armorType), outOfDomain: !FragmentBallistics.HasYieldRow(plate.armorType));
		pen.model = 0;
		pen.thorOutOfRange = FragmentBallistics.ThorHasConstants(plate.armorType);
		bool engine = ShotRouting.Fires(ShotBranch.EngineDamage, plate.kind, ThreatClass.Fragment);
		if (!pen.penetrates)
		{
			if (engine)
			{
				plate.RegisterEngineFragment(pen.energyJ);
			}
			realSpeedMps *= 0.6f;
			return;
		}
		if (engine)
		{
			plate.RegisterEngineFragment(EngineDamage.DepositedEnergyJ(pen.energyJ, massKg * 0.5f * pen.residualVelocityMS * pen.residualVelocityMS));
		}
		Physics.IgnoreCollision(col, c.collider, true);
		float dia = Mathf.Max(transform.localScale.x, SceneScale.RodDiameterWorld(2f));
		transform.position = contact.point + dir * dia;
		realSpeedMps = pen.residualVelocityMS;
		if (rb != null)
		{
			rb.linearVelocity = dir * (realSpeedMps * velocityVisualScale);
		}
		FragmentSpawner spawner = FragmentSpawner.Instance;
		if (spawner != null && FragmentSpawner.IsFragmentSource(plate))
		{
			float area = ((penAreaM2 > 0f) ? penAreaM2 : (sizeM * sizeM));
			spawner.SpawnSecondary(contact.point, dir, realSpeedMps, plate.armorType, c.collider, area * tM);
		}
		DeformableMesh deform = plate.GetComponent<DeformableMesh>();
		if (deform != null)
		{
			deform.Dent(contact.point, dir, SceneScale.RodDiameterWorld(8f), dia * 0.5f, updateCollider: false);
		}
	}
}
