public readonly struct RadarQuery
{
	public readonly double FrequencyHz;

	public readonly double AzimuthDeg;

	public readonly double ElevationDeg;

	public readonly double ReceiverAzimuthDeg;

	public readonly double ReceiverElevationDeg;

	public readonly bool HasSeparateReceiver;

	public RadarDirectionBasis IncidentBasis => default;

	public RadarDirectionBasis ReceiverBasis => default;

	public RadarQuery(double frequencyHz, double azimuthDeg, double elevationDeg, double? receiverAzimuthDeg = null, double? receiverElevationDeg = null)
	{
		FrequencyHz = 0.0;
		AzimuthDeg = 0.0;
		ElevationDeg = 0.0;
		ReceiverAzimuthDeg = 0.0;
		ReceiverElevationDeg = 0.0;
		HasSeparateReceiver = false;
	}
}
