public interface ISegmentSink
{
	void AddSpan(float tEnterMm, float tExitMm, IMaterialRef material, float normalThicknessMm, float obliquityDeg, EntrySurface entry);

	void Reject(BodyRejectCode code, string reason);
}
