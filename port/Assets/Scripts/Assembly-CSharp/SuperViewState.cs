using UnityEngine;

public sealed class SuperViewState
{
	public struct Framing
	{
		public float orthoSize;

		public float depthHalf;

		public float capSideMin;
	}

	public float yawDeg;

	public float pitchDeg;

	public float azimuthDeg;

	public int zoomSteps;

	public float zoomOutLimit;

	public bool AxesCanonical => yawDeg == 0f && pitchDeg == 0f && azimuthDeg == 0f;

	public bool IsCanonical => yawDeg == 0f && pitchDeg == 0f && azimuthDeg == 0f && zoomSteps == 0;

	public float ZoomFactor => Mathf.Pow(1.15f, zoomSteps);

	public float Magnification => 1f / ZoomFactor;

	public void Rotate(float dYawDeg, float dPitchDeg)
	{
		yawDeg = SuperTapeRender.WrapYawDeg(yawDeg + dYawDeg);
		pitchDeg = Mathf.Clamp(pitchDeg + dPitchDeg, -85f, 85f);
	}

	public bool Zoom(int delta)
	{
		int num = zoomSteps + delta;
		float num2 = Mathf.Pow(1.15f, num);
		if (num2 < 0.05f || num2 > zoomOutLimit)
		{
			return false;
		}
		zoomSteps = num;
		return true;
	}

	public void StepAzimuth()
	{
		azimuthDeg = Mathf.Repeat(azimuthDeg + 90f + 30f, 180f) - 90f;
	}

	public void Reset()
	{
		yawDeg = 0f;
		pitchDeg = 0f;
		azimuthDeg = 0f;
		zoomSteps = 0;
	}

	public Framing Frame(float halfY, float halfZ, float aspect)
	{
		Framing result = default(Framing);
		float num = azimuthDeg * Mathf.Deg2Rad;
		Vector3 vector = new Vector3(Mathf.Cos(num), Mathf.Sin(num), 0f);
		if (AxesCanonical)
		{
			result.orthoSize = SuperTapeRender.OrthoForView(halfY, halfZ, aspect, vector, yawDeg, pitchDeg, out result.depthHalf);
		}
		else
		{
			result.orthoSize = SuperTapeRender.OrthoForProjected(halfY, halfZ, aspect, vector, yawDeg, pitchDeg, out result.depthHalf);
		}
		result.orthoSize *= ZoomFactor;
		result.capSideMin = SuperTapeRender.CapSideForView(result.orthoSize, halfY, halfZ);
		if (!IsCanonical)
		{
			result.depthHalf = Mathf.Max(result.depthHalf, SuperTapeRender.DepthFloorFor(halfY, halfZ));
		}
		return result;
	}

	public SuperViewState()
	{
		zoomOutLimit = 4f;
	}
}
