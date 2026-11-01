internal struct RadarKahanVector
{
	private RadarKahanComplex x;

	private RadarKahanComplex y;

	private RadarKahanComplex z;

	public RadarCVector Value => new RadarCVector(x.Value, y.Value, z.Value);

	public void Add(RadarCVector value)
	{
		x.Add(value.X);
		y.Add(value.Y);
		z.Add(value.Z);
	}
}
