using UnityEngine;

public class RodFins
{
	public int bladeCount = 6;

	private GameObject root;

	private GameObject[] parts;

	private Mesh[] pieceMeshes;

	private Material sharedMat;

	private bool[] sheared;

	private float diameterM;

	private float rodLengthM;

	private float gunCalibreM;

	private float worldPerMeter;

	private Color color;

	private Vector3 refU;

	private Vector3 refV;

	private Vector3 refDir;

	public int BladeCount => Mathf.Max(bladeCount, 1);

	public int PieceCount => 3;

	public int PartCount => BladeCount * 3;

	public bool AllSheared { get; private set; }

	public int ShearedCount { get; private set; }

	public int PartIndex(int blade, int piece)
	{
		return blade * 3 + piece;
	}

	public void Configure(float diameterM, float rodLengthM, float worldPerMeter, Color color, int blades, Vector3 flightDir, float gunCalibreM = 0f)
	{
		this.diameterM = Mathf.Max(diameterM, 1e-05f);
		this.rodLengthM = Mathf.Max(rodLengthM, 0.0001f);
		this.gunCalibreM = Mathf.Max(gunCalibreM, 0f);
		this.worldPerMeter = Mathf.Max(worldPerMeter, 0.0001f);
		this.color = color;
		bladeCount = (blades > 0) ? Mathf.Min(blades, 12) : 6;
		SetRollBasis(flightDir);
		EnsurePool();
		ResetShear();
	}

	private void SetRollBasis(Vector3 dir)
	{
		refDir = (dir.sqrMagnitude > 1e-10f) ? dir.normalized : Vector3.forward;
		refU = Vector3.Cross(refDir, Vector3.up);
		if (refU.sqrMagnitude < 1e-06f)
		{
			refU = Vector3.Cross(refDir, Vector3.right);
		}
		refU = refU.normalized;
		refV = Vector3.Cross(refDir, refU).normalized;
	}

	public void ResetShear()
	{
		int n = PartCount;
		if (sheared == null || sheared.Length != n)
		{
			sheared = new bool[n];
		}
		for (int i = 0; i < n; i++)
		{
			sheared[i] = false;
		}
		ShearedCount = 0;
		AllSheared = false;
		if (parts != null)
		{
			for (int i = 0; i < parts.Length; i++)
			{
				if (parts[i] != null)
				{
					parts[i].SetActive(true);
				}
			}
		}
	}

	private void EnsureMeshes()
	{
		if (pieceMeshes != null && pieceMeshes.Length == 3)
		{
			return;
		}
		pieceMeshes = new Mesh[3];
		for (int piece = 0; piece < 3; piece++)
		{
			pieceMeshes[piece] = MeshShapes.FinBladePiece(0.55f, 0.18f, 0.4f, (float)piece / 3f, (float)(piece + 1) / 3f, piece, piece + 1, 3, 0.06f, 5);
		}
	}

	private void EnsurePool()
	{
		if (root == null)
		{
			root = new GameObject("RodFins");
		}
		EnsureMeshes();
		if (parts != null && parts.Length == PartCount)
		{
			return;
		}
		if (parts != null)
		{
			for (int i = 0; i < parts.Length; i++)
			{
				if (parts[i] != null)
				{
					Object.Destroy(parts[i]);
				}
			}
		}
		parts = new GameObject[PartCount];
		for (int blade = 0; blade < BladeCount; blade++)
		{
			for (int piece = 0; piece < 3; piece++)
			{
				GameObject gameObject = new GameObject(string.Format("FinBlade{0}_{1}", blade, piece), typeof(MeshFilter), typeof(MeshRenderer));
				gameObject.transform.SetParent(root.transform, false);
				gameObject.GetComponent<MeshFilter>().sharedMesh = pieceMeshes[piece];
				MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
				if (meshRenderer != null)
				{
					if (sharedMat == null)
					{
						Shader shader = Shader.Find("Standard");
						Material material = (shader != null) ? new Material(shader) : null;
						if (material != null)
						{
							material.color = color;
						}
						sharedMat = material;
					}
					if (sharedMat != null)
					{
						meshRenderer.sharedMaterial = sharedMat;
					}
				}
				parts[PartIndex(blade, piece)] = gameObject;
			}
		}
	}

	public Mesh PieceMesh(int piece)
	{
		EnsureMeshes();
		return pieceMeshes[Mathf.Clamp(piece, 0, pieceMeshes.Length - 1)];
	}

	public void SetMaterial(Material m)
	{
		if (m == null || parts == null)
		{
			return;
		}
		for (int i = 0; i < parts.Length; i++)
		{
			if (parts[i] != null)
			{
				MeshRenderer meshRenderer = parts[i].GetComponent<MeshRenderer>();
				if (meshRenderer != null)
				{
					meshRenderer.sharedMaterial = m;
				}
			}
		}
	}

	public void SetActive(bool on)
	{
		if (root != null)
		{
			root.SetActive(on);
		}
	}

	public Vector3 BladeRadial(int j)
	{
		float angle = (float)j / (float)BladeCount * Mathf.PI * 2f;
		Vector3 radial = refU * Mathf.Cos(angle) + refV * Mathf.Sin(angle);
		return radial.normalized;
	}

	public bool IsSheared(int blade, int piece)
	{
		int num = PartIndex(blade, piece);
		if (sheared != null && num >= 0 && num < sheared.Length)
		{
			return sheared[num];
		}
		return false;
	}

	public void MarkSheared(int blade, int piece)
	{
		int num = PartIndex(blade, piece);
		if (sheared == null || num < 0 || num >= sheared.Length)
		{
			return;
		}
		if (sheared[num])
		{
			return;
		}
		sheared[num] = true;
		ShearedCount++;
		if (parts[num] != null)
		{
			parts[num].SetActive(false);
		}
		AllSheared = ShearedCount >= PartCount;
	}

	public void Render(Vector3 tailWorld, Vector3 dir)
	{
		EnsurePool();
		if (root != null && !root.activeSelf)
		{
			root.SetActive(true);
		}
		Vector3 d = (dir.sqrMagnitude > 1e-10f) ? dir.normalized : refDir;
		float diam = Mathf.Max(diameterM, 0f);
		float spanW = Mathf.Min(diam * 2f, Mathf.Max(rodLengthM, 1e-06f) * 0.4f) * worldPerMeter;
		float cal = (gunCalibreM > 0f) ? Mathf.Max(gunCalibreM * 0.5f, diam * 1.3f) : (diam * 2.5f);
		float offsetW = Mathf.Max(cal - diam, 0f) * 0.5f * worldPerMeter;
		float chordW = diam * 0.06f * worldPerMeter;
		for (int blade = 0; blade < BladeCount; blade++)
		{
			Vector3 radial = BladeRadial(blade);
			Vector3 pos = tailWorld + d * (spanW * 0.5f) + radial * (diameterM * 0.5f * worldPerMeter);
			Quaternion rot = Quaternion.LookRotation(d, radial);
			Vector3 scale = new Vector3(Mathf.Max(chordW, 0.0001f), Mathf.Max(offsetW, 0.0001f), Mathf.Max(spanW, 0.0001f));
			for (int piece = 0; piece < 3; piece++)
			{
				GameObject gameObject = parts[blade * 3 + piece];
				if (gameObject == null)
				{
					continue;
				}
				if (sheared[blade * 3 + piece])
				{
					gameObject.SetActive(false);
					continue;
				}
				gameObject.SetActive(true);
				gameObject.transform.position = pos;
				gameObject.transform.rotation = rot;
				gameObject.transform.localScale = scale;
			}
		}
	}

	public void Destroy()
	{
		if (root != null)
		{
			Object.Destroy(root);
		}
		root = null;
		parts = null;
	}
}
