using UnityEngine;
using UnityEngine.Rendering;

public class AimLine : MonoBehaviour
{
	private const float WidthM = 0.012f;

	private const float CrossHalfM = 0.06f;

	private static readonly Color LineColor = new Color(1f, 0.72f, 0.2f, 1f);

	private LineRenderer line;

	private LineRenderer armA;

	private LineRenderer armB;

	private void Start()
	{
		Material m = MakeMaterial(LineColor);
		line = MakeLine("AimLine", m, 2);
		armA = MakeLine("AimCrossA", m, 2);
		armB = MakeLine("AimCrossB", m, 2);
	}

	private LineRenderer MakeLine(string name, Material m, int points)
	{
		GameObject go = new GameObject(name);
		go.transform.SetParent(transform, false);
		LineRenderer lr = go.AddComponent<LineRenderer>();
		lr.sharedMaterial = m;
		lr.startWidth = (lr.endWidth = SceneScale.FromM(WidthM));
		lr.positionCount = points;
		lr.useWorldSpace = true;
		lr.shadowCastingMode = ShadowCastingMode.Off;
		lr.receiveShadows = false;
		lr.startColor = (lr.endColor = LineColor);
		lr.enabled = false;
		return lr;
	}

	private static Material MakeMaterial(Color c)
	{
		Shader shader = Shader.Find("Unlit/Color");
		if (shader == null)
		{
			shader = Shader.Find("Standard");
		}
		Material m = new Material(shader);
		if (m.HasProperty("_Color"))
		{
			m.color = c;
		}
		if (shader != null && shader.name == "Unlit/Color")
		{
			SectionView.MarkUnlit(m);
		}
		return m;
	}

	private void LateUpdate()
	{
		if (line == null)
		{
			return;
		}
		Projectile pr = ((AimShot.Available && AimShot.Mode == AimMode.Free) ? UnityEngine.Object.FindFirstObjectByType<Projectile>() : null);
		bool show = pr != null;
		armB.enabled = show;
		armA.enabled = show;
		line.enabled = show;
		if (!show)
		{
			return;
		}
		Vector3 aim = AimShot.AimPointWorld;
		line.SetPosition(0, pr.StartPositionWorld);
		line.SetPosition(1, aim);
		Vector3 d = AimShot.Direction;
		Vector3 side = ((Mathf.Abs(Vector3.Dot(d, Vector3.up)) > 0.99f) ? Vector3.forward : Vector3.up);
		Vector3 a = Vector3.Cross(side, d).normalized;
		Vector3 b = Vector3.Cross(d, a).normalized;
		float half = SceneScale.FromM(CrossHalfM);
		armA.SetPosition(0, aim - a * half);
		armA.SetPosition(1, aim + a * half);
		armB.SetPosition(0, aim - b * half);
		armB.SetPosition(1, aim + b * half);
	}
}
