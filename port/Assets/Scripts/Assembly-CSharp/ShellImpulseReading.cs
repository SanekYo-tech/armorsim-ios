using System;
using UnityEngine;

public struct ShellImpulseReading
{
	public Vector3 injected;

	public double injectedMagnitude;

	public double scalarImpulse;

	public double ratio;

	public double predicted;

	public double tolerance;

	public ShellImpulseStatus status;

	public string why;

	public int drivenCount;

	public int onAxisDropped;

	public int zeroDirectionDropped;

	public double drivenMassKg;

	public bool resumed;

	public string Describe()
	{
		if (status == ShellImpulseStatus.NotFired)
		{
			string text = resumed ? "контроль импульса: на отрезке после продолжения импульс не вносился" : "контроль импульса: импульс не выдавался";
			if (!string.IsNullOrEmpty(why))
			{
				return text + " (" + why + ")";
			}
			return text;
		}
		string[] array = new string[5];
		array[0] = string.Format("контроль импульса: |dP| = {0:E3} кг*м/с при скалярном {1:E3}, ", injectedMagnitude, scalarImpulse);
		array[1] = string.Format("отношение {0:E3} при допуске {1:F3}; замкнутая форма {2:E3} ", ratio, tolerance, predicted);
		array[2] = string.Format("(расхождение {0:E1}); ведомых {1}, ", Math.Abs(ratio - predicted), drivenCount);
		array[3] = string.Format("на оси без радиальной части {0}, без направления {1}, ", onAxisDropped, zeroDirectionDropped);
		array[4] = string.Format("масса стенки {0:F1} г", drivenMassKg * 1000.0);
		string text2 = string.Concat(array);
		string text3 = ((status != ShellImpulseStatus.WithinTolerance) ? ((status != ShellImpulseStatus.Exceeded) ? "  [ПРИБОР МЁРТВ]" : "  [СВЕРХ ДОПУСКА]") : "");
		string text4 = string.IsNullOrEmpty(why) ? "" : ("  — " + why);
		return text2 + text3 + text4;
	}
}
