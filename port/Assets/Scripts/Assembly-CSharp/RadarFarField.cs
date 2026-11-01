using System;
using Unity.Mathematics;

public static class RadarFarField
{
	public static RadarCVector Scatter(RadarCVector electricG, RadarCVector magneticL, double3 receiverDirection, double wavenumber)
	{
		if (!(wavenumber > 0.0) || double.IsNaN(wavenumber) || double.IsInfinity(wavenumber))
		{
			throw new ArgumentOutOfRangeException("wavenumber");
		}
		Complex64 g = electricG.Dot(receiverDirection);
		RadarCVector value = electricG - RadarCVector.FromReal(receiverDirection) * g - RadarCVector.Cross(receiverDirection, magneticL);
		return value * new Complex64(0.0, -wavenumber / (4.0 * Math.PI));
	}

	public static Complex64 Project(RadarCVector electricG, RadarCVector magneticL, double3 receiverDirection, double3 receiverPolarization, double wavenumber)
	{
		return Scatter(electricG, magneticL, receiverDirection, wavenumber).Dot(receiverPolarization);
	}

	public static double SigmaSquareMeters(Complex64 amplitude)
	{
		return (amplitude.Real * amplitude.Real + amplitude.Imaginary * amplitude.Imaginary) * (4.0 * Math.PI);
	}

	public static double SigmaDbSquareMeters(Complex64 amplitude)
	{
		double sigma = SigmaSquareMeters(amplitude);
		if (sigma > 0.0)
		{
			return 10.0 * Math.Log10(sigma);
		}
		return double.NegativeInfinity;
	}
}
