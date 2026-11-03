using System.Collections.Generic;
using UnityEngine;

public class WarheadDetonation : MonoBehaviour
{
	public float casingRadialSpeedMS;

	[Header("Куски корпуса")]
	public bool spawnCasingPieces;

	public int casingPieces;

	public float pieceLifetimeS;

	public float pieceSpinDegS;

	public bool piecesHitEachOther;

	private static readonly List<GameObject> spawned;

	private static readonly List<Renderer> hiddenRenderers;

	private static readonly List<Collider> disabledColliders;

	private static float pieceSpinDegSStatic;

	private static bool mutualCollision;

	static WarheadDetonation()
	{
		spawned = new List<GameObject>();
		hiddenRenderers = new List<Renderer>();
		disabledColliders = new List<Collider>();
	}

	public static void RestoreBody()
	{
		foreach (GameObject item in spawned)
		{
			if (item != null)
			{
				item.SetActive(true);
			}
		}
		spawned.Clear();
		foreach (Renderer item2 in hiddenRenderers)
		{
			if (item2 != null)
			{
				item2.enabled = true;
			}
		}
		hiddenRenderers.Clear();
	}

	public static void Detonate(Transform body, Vector3 forward, float forwardVelocityMS, float bodyDiameterMM, float bodyLengthMM, float radialSpeedMS, int pieces, float lifetimeS, float worldPerMeter, float timeDilation, Color casingColor, bool spawnPieces)
	{
	}

	public void DetonateFrom(Transform body, Vector3 forward, float forwardVelocityMS, float bodyDiameterMM, float bodyLengthMM, float worldPerMeter, float timeDilation, Color casingColor)
	{
		pieceSpinDegSStatic = pieceSpinDegS;
		mutualCollision = piecesHitEachOther;
		Detonate(body, forward, forwardVelocityMS, bodyDiameterMM, bodyLengthMM, casingRadialSpeedMS, casingPieces, pieceLifetimeS, worldPerMeter, timeDilation, casingColor, spawnCasingPieces);
	}

	public static void ClearAll()
	{
		foreach (GameObject item in spawned)
		{
			if (item != null)
			{
				Object.Destroy(item);
			}
		}
		spawned.Clear();
	}
}
