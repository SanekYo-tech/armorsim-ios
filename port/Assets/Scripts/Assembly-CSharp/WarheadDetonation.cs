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

	public static void RestoreBody()
	{
	}

	public static void Detonate(Transform body, Vector3 forward, float forwardVelocityMS, float bodyDiameterMM, float bodyLengthMM, float radialSpeedMS, int pieces, float lifetimeS, float worldPerMeter, float timeDilation, Color casingColor, bool spawnPieces)
	{
	}

	public void DetonateFrom(Transform body, Vector3 forward, float forwardVelocityMS, float bodyDiameterMM, float bodyLengthMM, float worldPerMeter, float timeDilation, Color casingColor)
	{
	}

	public static void ClearAll()
	{
	}
}
