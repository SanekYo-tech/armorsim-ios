using UnityEngine;

public sealed class AeroScenePlaceholder : MonoBehaviour
{
	private void Awake()
	{
		base.gameObject.AddComponent<AeroSceneController>();
	}
}
