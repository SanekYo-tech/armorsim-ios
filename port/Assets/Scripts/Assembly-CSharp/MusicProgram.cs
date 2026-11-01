using System;

public struct MusicProgram
{
	public string Name;

	public MusicTrack[] Tracks;

	public MusicPlan Plan;

	public bool SurvivesSceneChange;

	public bool KeepsPlaying(string sceneName, string menuSceneName)
	{
		if (!SurvivesSceneChange)
		{
			return false;
		}
		return !string.Equals(sceneName, menuSceneName, StringComparison.OrdinalIgnoreCase);
	}
}
