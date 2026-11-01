using UnityEngine;
using UnityEngine.EventSystems;

public sealed class AeroMenuTestInput : BaseInput
{
	public Vector2 Position;

	public Vector2 Wheel;

	public override bool mousePresent => false;

	public override Vector2 mousePosition => default;

	public override Vector2 mouseScrollDelta => default;

	public override bool touchSupported => false;

	public override int touchCount => 0;

	public override bool GetMouseButtonDown(int button)
	{
		return false;
	}

	public override bool GetMouseButtonUp(int button)
	{
		return false;
	}

	public override bool GetMouseButton(int button)
	{
		return false;
	}

	public override float GetAxisRaw(string axisName)
	{
		return 0f;
	}

	public override bool GetButtonDown(string buttonName)
	{
		return false;
	}
}
