using System;
using System.Globalization;
using System.Reflection;
using System.Text;

public static class UltraCacheKey
{
	public const string SolverFingerprint = "R/r0=1.279217/1128/489/S19-single-floor";

	public static readonly string[] KeyedFields = new string[36]
	{
		"rodDiameterM", "rodLengthM", "impactVelocityMps", "nose", "noseLengthCalibres", "noseHalfAngleDeg",
		"tipFlatOverD", "noseMeshMode", "rodMaterial", "targetKind", "targetThicknessM", "targetMaterial",
		"layers", "initialGapM", "elementsPerDiameter", "uniformRadiusCalibres", "outerRadiusCalibres", "gradingRatio",
		"hOverrideM", "cflSafety", "dtFloorS", "hourglassQ", "viscosityQuadratic", "viscosityLinear",
		"contactScale", "contactDampingRatio", "contactPushSpeedMps", "friction", "erodeEpsP", "erodeVolumeFraction",
		"asbDeletionPressureGate", "deleteMaxPressureFraction", "enableJcDamage", "enableAsbFade", "initialTemperatureK", "durationS"
	};

	public static readonly string[] CosmeticFields = new string[4] { "name", "outputFrames", "maxSteps", "materialOverride" };

	public static string Compute(ContinuumScenario s)
	{
		if (s == null)
		{
			throw new ArgumentNullException("s");
		}
		if (s.materialOverride != null)
		{
			return null;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(SolverFingerprint);
		stringBuilder.Append('|');
		Type typeFromHandle = typeof(ContinuumScenario);
		for (int i = 0; i < KeyedFields.Length; i++)
		{
			FieldInfo field = typeFromHandle.GetField(KeyedFields[i], BindingFlags.Instance | BindingFlags.Public);
			if (field == null)
			{
				throw new InvalidOperationException("UltraCacheKey: поля «" + KeyedFields[i] + "» в ContinuumScenario нет. Перечень ключа отстал от сценария — см. тест соответствия списков.");
			}
			object value = field.GetValue(s);
			if (KeyedFields[i] == "layers")
			{
				stringBuilder.Append("layers=");
				stringBuilder.Append(FmtLayers((ContinuumLayer[])value));
			}
			else
			{
				stringBuilder.Append(KeyedFields[i]);
				stringBuilder.Append('=');
				stringBuilder.Append(Fmt(value));
			}
			stringBuilder.Append(';');
		}
		return Hash(stringBuilder.ToString());
	}

	private static string Fmt(object v)
	{
		if (v == null)
		{
			return "null";
		}
		if (v is float)
		{
			return ((float)v).ToString("R", CultureInfo.InvariantCulture);
		}
		if (v is double)
		{
			return ((double)v).ToString("R", CultureInfo.InvariantCulture);
		}
		if (v is bool)
		{
			if (!(bool)v)
			{
				return "0";
			}
			return "1";
		}
		if (v is Enum)
		{
			return Convert.ToInt32(v).ToString(CultureInfo.InvariantCulture);
		}
		return Convert.ToString(v, CultureInfo.InvariantCulture);
	}

	private static string FmtLayers(ContinuumLayer[] layers)
	{
		if (layers == null || layers.Length == 0)
		{
			return null;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(layers.Length);
		for (int i = 0; i < layers.Length; i++)
		{
			stringBuilder.Append(':');
			stringBuilder.Append(Fmt(layers[i].gapBeforeM));
			stringBuilder.Append('/');
			stringBuilder.Append(Fmt(layers[i].thicknessM));
			stringBuilder.Append('/');
			stringBuilder.Append(Fmt(layers[i].material));
		}
		return stringBuilder.ToString();
	}

	public static string Hash(string s)
	{
		ulong num = 0x14650FB0739D0383uL;
		for (int i = 0; i < s.Length; i++)
		{
			num = (num ^ s[i]) * 0x100000001B3uL;
		}
		return num.ToString("X16");
	}

	public static string FileName(string key)
	{
		return "ultra_" + key + ".ultra.bin";
	}
}
