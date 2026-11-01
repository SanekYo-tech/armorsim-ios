public interface IShotObject
{
	string Id { get; }

	float AxialHalfExtentMm(in BodyLocalRay ray);

	void Trace(in BodyLocalRay ray, ISegmentSink sink);
}
