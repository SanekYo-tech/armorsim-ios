using System;

public sealed class ProjectedScenario
{
	public readonly ContinuumScenario scenario;

	public readonly bool isLossy;

	public readonly string declarationKey;

	public readonly float maxTiltDeg;

	public readonly float losThicknessMM;

	public readonly float rawThicknessMM;

	public readonly int layerCount;

	private readonly UltraProjection.ProjectedDisc[] discs;

	public int DiscCount
	{
		get
		{
			if (discs == null)
			{
				return 0;
			}
			return discs.Length;
		}
	}

	public UltraProjection.ProjectedDisc Disc(int i)
	{
		return discs[i];
	}

	public ProjectedScenario(ContinuumScenario scenario, bool isLossy, string declarationKey, float maxTiltDeg, float rawThicknessMM, float losThicknessMM, int layerCount = 1, UltraProjection.ProjectedDisc[] discs = null)
	{
		if (scenario == null)
		{
			throw new ArgumentNullException("scenario");
		}
		if ((isLossy || layerCount > 1) && string.IsNullOrEmpty(declarationKey))
		{
			throw new InvalidOperationException("ProjectedScenario: проекция с потерями (θ ≠ 0 либо пакет слоёв) построена БЕЗ ключа объявления. Показать её нельзя: снимок без метки становится утверждением о физике, которого модель не делает.");
		}
		this.scenario = scenario;
		this.isLossy = isLossy;
		this.declarationKey = declarationKey;
		this.maxTiltDeg = maxTiltDeg;
		this.losThicknessMM = losThicknessMM;
		this.rawThicknessMM = rawThicknessMM;
		this.layerCount = Math.Max(1, layerCount);
		if (discs != null && discs.Length > 0)
		{
			this.discs = new UltraProjection.ProjectedDisc[discs.Length];
			Array.Copy(discs, this.discs, discs.Length);
		}
	}
}
