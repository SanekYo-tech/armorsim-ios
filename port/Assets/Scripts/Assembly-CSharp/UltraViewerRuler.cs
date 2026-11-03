using UnityEngine;

[ExecuteAlways]
public class UltraViewerRuler : MonoBehaviour
{
	public float stepM;

	public float fromM;

	public float toM;

	public float tickM;

	private void OnDrawGizmos()
	{
		Gizmos.color = new Color(0.45f, 0.85f, 1f, 0.55f);
		Gizmos.DrawLine(new Vector3(0f, 0f, fromM), new Vector3(0f, 0f, toM));
		float num = Mathf.Max(1E-06f, stepM);
		int num2 = Mathf.RoundToInt((toM - fromM) / num);
		for (int i = 0; i <= num2; i++)
		{
			float z = (float)i * stepM + fromM;
			float num3 = tickM;
			if ((i & 1) != 0)
			{
				num3 *= 0.5f;
			}
			Gizmos.DrawLine(new Vector3(0f - num3, 0f, z), new Vector3(num3, 0f, z));
		}
		Gizmos.color = new Color(1f, 0.55f, 0.2f, 0.8f);
		Gizmos.DrawLine(new Vector3(-0.08f, 0f, 0f), new Vector3(0.08f, 0f, 0f));
	}
}