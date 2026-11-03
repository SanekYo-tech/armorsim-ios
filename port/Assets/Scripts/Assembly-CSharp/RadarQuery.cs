using System;

public readonly struct RadarQuery
{
	public readonly double FrequencyHz;

	public readonly double AzimuthDeg;

	public readonly double ElevationDeg;

	public readonly double ReceiverAzimuthDeg;

	public readonly double ReceiverElevationDeg;

	public readonly bool HasSeparateReceiver;

	public RadarDirectionBasis IncidentBasis => RadarDirection.BasisDegrees(AzimuthDeg, ElevationDeg);

	public RadarDirectionBasis ReceiverBasis => RadarDirection.BasisDegrees(ReceiverAzimuthDeg, ReceiverElevationDeg);

	public RadarQuery(double frequencyHz, double azimuthDeg, double elevationDeg, double? receiverAzimuthDeg = null, double? receiverElevationDeg = null)
	{
		if (!(frequencyHz > 0.0) || double.IsNaN(frequencyHz) || double.IsInfinity(frequencyHz))
		{
			throw new ArgumentOutOfRangeException("frequencyHz");
		}
		if (receiverAzimuthDeg.HasValue != receiverElevationDeg.HasValue)
		{
			throw new ArgumentException("Both receiver angles must be supplied together.");
		}
		FrequencyHz = frequencyHz;
		AzimuthDeg = azimuthDeg;
		ElevationDeg = elevationDeg;
		ReceiverAzimuthDeg = receiverAzimuthDeg ?? azimuthDeg;
		ReceiverElevationDeg = receiverElevationDeg ?? elevationDeg;
		HasSeparateReceiver = receiverAzimuthDeg.HasValue;
		RadarDirection.BasisDegrees(azimuthDeg, elevationDeg);
		RadarDirection.BasisDegrees(ReceiverAzimuthDeg, ReceiverElevationDeg);
	}
}
