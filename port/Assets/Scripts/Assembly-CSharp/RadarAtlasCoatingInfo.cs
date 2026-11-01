using System;

[Serializable]
public sealed class RadarAtlasCoatingInfo
{
	public string id;

	public string label;

	public double f0Hz;

	public string layers;

	public string tag;

	public RadarAtlasCoatingInfo()
	{
		id = "metal";
		label = "metal";
		layers = "";
		tag = "";
	}

	public static RadarAtlasCoatingInfo Of(RadarCoating coating)
	{
		if (coating == null || coating.IsMetal)
		{
			return new RadarAtlasCoatingInfo();
		}
		RadarAtlasCoatingInfo radarAtlasCoatingInfo = new RadarAtlasCoatingInfo();
		radarAtlasCoatingInfo.id = coating.ScenarioId;
		radarAtlasCoatingInfo.label = coating.Describe();
		radarAtlasCoatingInfo.f0Hz = coating.F0Hz;
		radarAtlasCoatingInfo.layers = coating.Describe();
		if (coating.Id == RadarCoating.Kind.Custom)
		{
			radarAtlasCoatingInfo.tag = "ASSUMED (player input)";
			return radarAtlasCoatingInfo;
		}
		radarAtlasCoatingInfo.tag = ((coating.Id == RadarCoating.Kind.Jaumann) ? "ASSUMED (architect's design)" : "DERIVED (construction)");
		return radarAtlasCoatingInfo;
	}
}
