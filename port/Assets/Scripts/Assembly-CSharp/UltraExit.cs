using UnityEngine;

public class UltraExit : MonoBehaviour
{
	public UltraShotDriver driver;

	private void Awake()
	{
		SandboxPauseMenu.FindOrCreate();
	}

	private void OnDestroy()
	{
		if (driver != null)
		{
			driver.Cancel();
		}
		UltraSolveQueue.Instance.Shutdown();
	}
}
