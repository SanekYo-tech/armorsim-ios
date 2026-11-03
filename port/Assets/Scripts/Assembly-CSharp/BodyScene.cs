using System.Collections.Generic;

public sealed class BodyScene
{
	public BodyShotSetup Shot { get; }

	public IReadOnlyList<BodyPlacement> Placements { get; }

	public BodyScene(BodyShotSetup shot, IReadOnlyList<BodyPlacement> placements)
	{
		Shot = shot;
		Placements = placements;
	}
}
