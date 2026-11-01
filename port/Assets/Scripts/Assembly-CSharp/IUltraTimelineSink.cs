public interface IUltraTimelineSink
{
	void Accept(string key, GeometryTimeline tl);
}
