internal struct RadarKahanComplex
{
	private RadarKahan real;

	private RadarKahan imaginary;

	public Complex64 Value => new Complex64(real.Value, imaginary.Value);

	public void Add(Complex64 value)
	{
		real.Add(value.Real);
		imaginary.Add(value.Imaginary);
	}
}
