using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class SuperShellDrive : SuperEraDrive
{
	public int targetPlateBody;

	public bool SabotageOpenBase;

	public const float Tolerance = 0.01f;

	public const float DeadInstrumentTolerance = 0.001f;

	public const float OnAxisFractionOfSpacing = 0.25f;

	public const int MinSideWallParticles = 6;

	public const int NoseProbeExtraLayers = 2;

	public const float SolverCeilingMultiplier = 10f;

	public const float CeilingShareWarnAt = 0.5f;

	private int _plateBody;

	private Vector3 _plateDir;

	private float _plateFaceZ;

	private float _plateRearZ;

	private float _plateThickM;

	private string _plateName;

	private float _spacingM;

	private int _nosePrefix;

	private Vector3[] _probeShell;

	private Vector3[] _velShell;

	private Vector3[] _posShell;

	private int[] _driven;

	private float[] _cRho;

	private float[] _cAx;

	private int[] _side;

	private double _drivenMassKg;

	private Vector3[] _dir;

	private int _onAxisDropped;

	private int _zeroDirDropped;

	private double _predictedRatio;

	private Vector3 _axisAtFire;

	private Vector3 _axisPointAtFire;

	private KahanSum _injX;

	private KahanSum _injY;

	private KahanSum _injZ;

	private KahanSum _scalar;

	private bool _resumed;

	public ShellImpulseReading Impulse => BuildReading();

	public Vector3 AxisAtFire => _axisAtFire;

	public Vector3 AxisPointAtFire => _axisPointAtFire;

	public int DrivenCount => (_driven != null) ? _driven.Length : 0;

	public SuperShellDrive()
	{
		targetPlateBody = -1;
		_plateBody = -1;
		_plateName = "";
	}

	public override void Prepare(SuperBaker.Baked baked)
	{
		if (next != null)
		{
			next.Prepare(baked);
		}
		if (baked == null || !baked.hasRod)
		{
			throw new InvalidOperationException("[привод корпуса] в сцене нет снаряда: тело 0 обязано быть полым корпусом. Базовый привод в такой сцене просто молчит, и молчаливо инертный разрыв неотличим от исправного прогона — здесь это отказ.");
		}
		if (baked.bodies == null)
		{
			throw new NullReferenceException();
		}
		if (baked.bodies.Count == 0)
		{
			throw new InvalidOperationException("[привод корпуса] в сцене нет снаряда: тело 0 обязано быть полым корпусом. Базовый привод в такой сцене просто молчит, и молчаливо инертный разрыв неотличим от исправного прогона — здесь это отказ.");
		}
		if (!baked.cavity.present)
		{
			throw new InvalidOperationException("[привод корпуса] у тела 0 нет полости: cavityFraction не задана либо запекание испекло корпус сплошным. Радиальный импульс по сплошному телу разрывал бы его от собственной оси наружу — это не фугас, а дефект постановки.");
		}
		if (baked.projectileBodyCount != 1)
		{
			throw new InvalidOperationException(string.Format("[привод корпуса] снаряд собран из {0} тел. Полый корпус ", baked.projectileBodyCount) + "едет ТОЛЬКО стержневым путём (SuperRodSpec): у составного снаряда полости нет по построению, и ручка на нём была бы данными без читателя.");
		}
		_plateBody = (targetPlateBody >= 0) ? targetPlateBody : baked.projectileBodyCount;
		if (_plateBody < baked.projectileBodyCount)
		{
			throw new InvalidOperationException(string.Format("[привод корпуса] мишенью названо тело {0}, а плиты сцены это ", _plateBody) + string.Format("{0}..{1}. Взрыватель, взведённый по ", baked.projectileBodyCount, baked.bodies.Count - 1) + "самому снаряду, сработал бы в нулевой момент времени.");
		}
		SuperBaker.Body b = baked.bodies[_plateBody];
		double nx = b.normal.x;
		double ny = b.normal.y;
		double nz = b.normal.z;
		double len = Math.Sqrt(ny * ny + nx * nx + nz * nz);
		if (1e-09 > len)
		{
			throw new InvalidOperationException(string.Format("[привод корпуса] у мишени (тело {0}) вырожденная нормаль лицевой грани — ", _plateBody) + "опорной плоскости срабатывания нет.");
		}
		_plateDir = new Vector3((float)(nx / len), (float)(ny / len), (float)(nz / len));
		_plateThickM = b.thicknessM;
		_plateFaceZ = b.faceCentre.y * _plateDir.y + b.faceCentre.x * _plateDir.x + b.faceCentre.z * _plateDir.z;
		_plateRearZ = _plateFaceZ - _plateThickM;
		_plateName = (b.name != null) ? b.name : "";
		SuperBaker.Body shell = baked.bodies[0];
		_spacingM = (baked.cavity.spacingM > 0f) ? baked.cavity.spacingM : baked.spacing;
		float noseZ = shell.faceCentre.z;
		float threshold = (baked.cavity.nNose + NoseProbeExtraLayers) * baked.cavity.spacingM;
		int prefix = 0;
		int misordered = 0;
		if (shell.count > 0)
		{
			for (int i = 0; i < shell.count; i++)
			{
				Vector3 pos = baked.particles.x[shell.start + i];
				if (pos.z - noseZ < threshold)
				{
					if (i == prefix)
					{
						prefix = i + 1;
					}
					else
					{
						misordered++;
					}
				}
			}
			if (prefix <= 0)
			{
				throw new InvalidOperationException(string.Format("[привод корпуса] носовой признак пуст: ни одной частицы ближе {0:F2} мм ", threshold * 1000f) + "от кончика. Свидетель срабатывания, у которого нет частиц, взвёл бы взрыватель никогда.");
			}
			if (misordered > 0)
			{
				throw new InvalidOperationException(string.Format("[привод корпуса] носовые частицы не лежат префиксом тела: {0} штук стоят ", misordered) + string.Format("после первых {0}. Признак опирается на порядок выпуска FillRod (слой за слоем ", prefix) + "от кончика); порядок изменился — опрашивать префикс больше нельзя, и молча опрашивать не тот кусок тела хуже отказа.");
			}
		}
		else
		{
			throw new InvalidOperationException(string.Format("[привод корпуса] носовой признак пуст: ни одной частицы ближе {0:F2} мм ", threshold * 1000f) + "от кончика. Свидетель срабатывания, у которого нет частиц, взвёл бы взрыватель никогда.");
		}
		List<int> drivenList = new List<int>();
		List<float> cRhoList = new List<float>();
		List<float> cAxList = new List<float>();
		List<int> sideList = new List<int>();
		KahanSum massSum = default(KahanSum);
		float prevDist = float.NegativeInfinity;
		for (int j = 0; j < shell.count; j++)
		{
			Vector3 pos2 = baked.particles.x[shell.start + j];
			if (baked.cavity.WallRoleAt(pos2, noseZ, out float axial, out float radial))
			{
				if (!SabotageOpenBase || axial <= 0f)
				{
					drivenList.Add(j);
					cRhoList.Add(radial);
					cAxList.Add(axial);
					massSum.Add(baked.particles.mass[shell.start + j]);
				}
			}
			if (baked.cavity.IsSideWallAt(pos2, noseZ))
			{
				float dist = pos2.z - noseZ;
				if (prevDist - _spacingM * 0.5f > dist)
				{
					throw new InvalidOperationException("[привод корпуса] частицы боковой стенки идут не по возрастанию расстояния " + string.Format("от кончика (допуск — полшага частиц, {0:F2} мм) — порядок ", _spacingM * 500f) + "выпуска FillRod изменился, и деление на трети при снятии оси стало бы произвольным.");
				}
				prevDist = dist;
				sideList.Add(j);
			}
		}
		if (drivenList.Count == 0)
		{
			throw new InvalidOperationException("[привод корпуса] ведомых частиц стенки ноль при заявленной полости " + string.Format("(радиус {0:F2} мм, стенка {1} слоёв). Импульс ", baked.cavity.radiusM * 1000f, baked.cavity.wallLayers) + "некому нести: разрыва не будет, а прогон выглядел бы исправным.");
		}
		if (sideList.Count < MinSideWallParticles)
		{
			throw new InvalidOperationException(string.Format("[привод корпуса] боковая стенка дала {0} частиц при нужных ", sideList.Count) + string.Format("{0}: по ним снимается текущая ОСЬ тела, и центроид трети из ", MinSideWallParticles) + "одной-двух частиц был бы шумом, а не осью.");
		}
		_driven = drivenList.ToArray();
		_cRho = cRhoList.ToArray();
		_cAx = cAxList.ToArray();
		_side = sideList.ToArray();
		_drivenMassKg = massSum.Value;
		_nosePrefix = prefix;
		_probeShell = new Vector3[prefix];
		_velShell = new Vector3[shell.count];
		_posShell = new Vector3[shell.count];
		_dir = null;
		_onAxisDropped = 0;
		_zeroDirDropped = 0;
		_predictedRatio = 0.0;
		_injX = default(KahanSum);
		_injY = default(KahanSum);
		_injZ = default(KahanSum);
		_scalar = default(KahanSum);
		_resumed = false;
		_nextProbeS = 0f;
		AnnouncePreflight(baked, b);
		_prepared = true;
	}

	private void AnnouncePreflight(SuperBaker.Baked baked, SuperBaker.Body b)
	{
		int plateCount = baked.bodies.Count - baked.projectileBodyCount;
		if (targetPlateBody < 0 && plateCount > 1)
		{
			Debug.LogWarning("[Super] корпус: мишень взрывателя НЕ НАЗВАНА (targetPlateBody = -1), взята ПЕРВАЯ " + string.Format("плита сцены: тело {0} \"{1}\" из {2} плит. Если впереди ", _plateBody, baked.bodies[_plateBody].name, plateCount) + "брони стоит экран либо кассета, взрыватель взведён по НЕЙ, а опорная плоскость снимается один раз, при подготовке, и метаемая пластина её покидает. Назови мишень числом, если имелась в виду броня.");
		}
		double maxSpeedSq = 0.0;
		for (int i = 0; i < b.count; i++)
		{
			Vector3 v = baked.particles.v[b.start + i];
			double dx = v.x;
			double dy = v.y;
			double dz = v.z;
			double sq = dy * dy + dx * dx + dz * dz;
			if (sq > maxSpeedSq)
			{
				maxSpeedSq = sq;
			}
		}
		double maxSpeed = Math.Sqrt(maxSpeedSq);
		if (maxSpeed <= 0.0)
		{
			Debug.Log(string.Format("[Super] корпус: мишень - тело {0} \"{1}\"; скорость удара ", _plateBody, baked.bodies[_plateBody].name) + "нулевая, потолок скорости решателя в такой сцене выключен (сверять не с чем).");
			return;
		}
		double ceiling = maxSpeed * (double)SolverCeilingMultiplier;
		if ((double)velocityMps + maxSpeed > ceiling)
		{
			throw new InvalidOperationException(string.Format("[привод корпуса] скорость разлёта {0:F0} м/с поверх скорости удара ", velocityMps) + string.Format("{0:F0} м/с даёт стенке до {1:F0} м/с при потолке скорости сцены ", maxSpeed, (double)velocityMps + maxSpeed) + string.Format("{0:F0} м/с (= {1:F0} x |скорость удара|, потолок ставит ", ceiling, SolverCeilingMultiplier) + "SuperShot). Решатель уронил бы такой прогон как FAIL_VELOCITY, то есть сломанная ПОСТАНОВКА отчиталась бы вырождением счёта. Отказ выдан до первого шага: уменьшай скорость разлёта либо поднимай скорость встречи.");
		}
		double share = ((double)velocityMps + maxSpeed) / ceiling;
		string[] array = new string[5];
		array[0] = string.Format("[Super] корпус: мишень - тело {0} \"{1}\" ", _plateBody, baked.bodies[_plateBody].name);
		array[1] = string.Format("(плит в сцене {0}), ведомых частиц {1}, стенка ", plateCount, _driven.Length);
		array[2] = string.Format("{0} слоёв; разлёт {1:F0} м/с поверх удара ", baked.cavity.wallLayers, velocityMps);
		array[3] = string.Format("{0:F0} м/с, то есть до {1:F0} м/с при потолке {2:F0} м/с ", maxSpeed, (double)velocityMps + maxSpeed, ceiling);
		array[4] = string.Format("({0:P1} потолка).", share);
		string text = string.Concat(array);
		if (share > (double)CeilingShareWarnAt)
		{
			Debug.LogWarning(text + string.Format(" Привод съедает больше {0:P0} потолка: если ", CeilingShareWarnAt) + "прогон упадёт по FAIL_VELOCITY, причина здесь, а не в счёте.");
		}
		else
		{
			Debug.Log(text);
		}
	}

	public override void Restore(in SuperEraDrive.State s)
	{
		base.Restore(s);
		_resumed = s.deliveredMps > 0f;
	}

	public override void AfterStep(SuperKernelGPU g, SuperBaker.Baked baked, float tS, float dtS)
	{
		if (next != null)
		{
			next.AfterStep(g, baked, tS, dtS);
		}
		if (!_prepared || SabotageNeverFires)
		{
			return;
		}
		SuperBaker.Body body = baked.bodies[0];
		if (ContactTimeS < 0f)
		{
			if (_nextProbeS > tS)
			{
				return;
			}
			_nextProbeS = tS + SuperEraDrive.ProbeEveryS;
			g.DownloadPositionRange(_probeShell, body.start, _nosePrefix);
			Probes++;
			float num = float.MaxValue;
			for (int i = 0; i < _nosePrefix; i++)
			{
				float num2 = Vector3.Dot(_plateDir, _probeShell[i]) - _plateFaceZ;
				if (num2 < num)
				{
					num = num2;
				}
			}
			if (num > triggerDistanceM)
			{
				return;
			}
			ContactTimeS = tS;
			FireTimeS = tS + Mathf.Max(0f, delayMultiplier) * initiationDelayS;
			return;
		}
		if (FireTimeS > tS)
		{
			return;
		}
		float since = tS - FireTimeS;
		if (since > rampS)
		{
			return;
		}
		if (SabotageZeroVelocity || velocityMps <= 0f)
		{
			Fired = true;
			return;
		}
		if (_dir == null)
		{
			SampleDirections(g, baked, body);
		}
		float num4 = NextIncrement(velocityMps, dtS);
		num4 = WithLastStepRemainder(num4, since, dtS, velocityMps);
		if (num4 <= 0f)
		{
			return;
		}
		g.DownloadVelocityRange(_velShell, body.start, body.count);
		KahanSum beforeX = default(KahanSum);
		KahanSum beforeY = default(KahanSum);
		KahanSum beforeZ = default(KahanSum);
		for (int j = 0; j < _driven.Length; j++)
		{
			int num5 = _driven[j];
			float mass = baked.particles.mass[body.start + num5];
			Vector3 v = _velShell[num5];
			beforeX.Add(v.x * mass);
			beforeY.Add(v.y * mass);
			beforeZ.Add(v.z * mass);
		}
		KahanSum scalarSum = default(KahanSum);
		for (int k = 0; k < _driven.Length; k++)
		{
			int num6 = _driven[k];
			Vector3 v2 = _velShell[num6];
			Vector3 nv = v2 + _dir[k] * num4;
			_velShell[num6] = nv;
			float mass2 = baked.particles.mass[body.start + num6];
			double dx = nv.x - v2.x;
			double dy = nv.y - v2.y;
			double dz = nv.z - v2.z;
			scalarSum.Add(Math.Sqrt(dy * dy + dx * dx + dz * dz) * mass2);
		}
		KahanSum afterX = default(KahanSum);
		KahanSum afterY = default(KahanSum);
		KahanSum afterZ = default(KahanSum);
		for (int l = 0; l < _driven.Length; l++)
		{
			int num7 = _driven[l];
			float mass3 = baked.particles.mass[body.start + num7];
			Vector3 v3 = _velShell[num7];
			afterX.Add(v3.x * mass3);
			afterY.Add(v3.y * mass3);
			afterZ.Add(v3.z * mass3);
		}
		g.UploadVelocityRange(_velShell, body.start, body.count);
		_injX.Add(afterX.Value - beforeX.Value);
		_injY.Add(afterY.Value - beforeY.Value);
		_injZ.Add(afterZ.Value - beforeZ.Value);
		_scalar.Add(scalarSum.Value);
		DeliveredMps += num4;
		Fired = true;
	}

	private void SampleDirections(SuperKernelGPU g, SuperBaker.Baked baked, SuperBaker.Body b)
	{
		g.DownloadPositionRange(_posShell, b.start, b.count);
		int wall = _side.Length;
		int third = Math.Max(1, wall / 3);
		Vector3 c0 = SideCentroid(baked.particles, b.start, 0, third);
		Vector3 c2 = SideCentroid(baked.particles, b.start, wall - third, third);
		Vector3 centre = SideCentroid(baked.particles, b.start, 0, wall);
		double ax = (double)c0.x - c2.x;
		double ay = (double)c0.y - c2.y;
		double az = (double)c0.z - c2.z;
		double axisLen = Math.Sqrt(ay * ay + ax * ax + az * az);
		if (axisLen < (double)_spacingM * 0.5)
		{
			throw new InvalidOperationException("[привод корпуса] ось тела вырождена: центроиды третей боковой стенки разошлись " + string.Format("на {0:F3} мм при шаге частиц {1:F2} мм. Направление разлёта ", axisLen * 1000.0, _spacingM * 1000f) + "по такой оси было бы шумом.");
		}
		_axisPointAtFire = centre;
		ax /= axisLen;
		ay /= axisLen;
		az /= axisLen;
		_axisAtFire = new Vector3((float)ax, (float)ay, (float)az);
		_dir = new Vector3[_driven.Length];
		float onAxis = _spacingM * OnAxisFractionOfSpacing;
		_onAxisDropped = 0;
		_zeroDirDropped = 0;
		KahanSum sumX = default(KahanSum);
		KahanSum sumY = default(KahanSum);
		KahanSum sumZ = default(KahanSum);
		KahanSum sumM = default(KahanSum);
		for (int i = 0; i < _driven.Length; i++)
		{
			int num = _driven[i];
			Vector3 pos = _posShell[num];
			double dx = pos.x - _axisPointAtFire.x;
			double dy = pos.y - _axisPointAtFire.y;
			double dz = pos.z - _axisPointAtFire.z;
			double axial = dy * ay + dx * ax + dz * az;
			dx -= axial * ax;
			dy -= axial * ay;
			dz -= axial * az;
			double radialLen = Math.Sqrt(dy * dy + dx * dx + dz * dz);
			float cRho = _cRho[i];
			float cAx = _cAx[i];
			double ddx;
			double ddy;
			double ddz;
			if (cRho == 0f)
			{
				ddx = 0f - cAx;
				ddy = 0f - cAx;
				ddz = 0f - cAx;
				ddx *= ax;
				ddy *= ay;
				ddz *= az;
			}
			else
			{
				if (radialLen < (double)onAxis)
				{
					_onAxisDropped++;
					ddx = 0f - cAx;
					ddy = 0f - cAx;
					ddz = 0f - cAx;
					ddx *= ax;
					ddy *= ay;
					ddz *= az;
				}
				else
				{
					ddx = dx / radialLen * cRho - cAx * ax;
					ddy = dy / radialLen * cRho - cAx * ay;
					ddz = dz / radialLen * cRho - cAx * az;
				}
			}
			double dirLen = Math.Sqrt(ddy * ddy + ddx * ddx + ddz * ddz);
			if (1e-12 > dirLen)
			{
				_zeroDirDropped++;
				_dir[i] = Vector3.zero;
			}
			else
			{
				_dir[i] = new Vector3((float)(ddx / dirLen), (float)(ddy / dirLen), (float)(ddz / dirLen));
				float mass = baked.particles.mass[b.start + num];
				sumX.Add(mass * (ddx / dirLen));
				sumY.Add(mass * (ddy / dirLen));
				sumZ.Add(mass * (ddz / dirLen));
				sumM.Add(mass);
			}
		}
		double predicted = 0.0;
		if (sumM.Value > 0.0)
		{
			predicted = Math.Sqrt(sumY.Value * sumY.Value + sumX.Value * sumX.Value + sumZ.Value * sumZ.Value) / sumM.Value;
		}
		_predictedRatio = predicted;
		_posShell = null;
	}

	private Vector3 SideCentroid(SuperParticles p, int bodyStart, int from, int count)
	{
		KahanSum sx = default(KahanSum);
		KahanSum sy = default(KahanSum);
		KahanSum sz = default(KahanSum);
		KahanSum sm = default(KahanSum);
		for (int i = from; i < from + count; i++)
		{
			float mass = p.mass[bodyStart + _side[i]];
			Vector3 pos = _posShell[_side[i]];
			sx.Add(pos.x * mass);
			sy.Add(pos.y * mass);
			sz.Add(pos.z * mass);
			sm.Add(mass);
		}
		if (1e-30 >= sm.Value)
		{
			return Vector3.zero;
		}
		return new Vector3((float)(sx.Value / sm.Value), (float)(sy.Value / sm.Value), (float)(sz.Value / sm.Value));
	}

	private ShellImpulseReading BuildReading()
	{
		ShellImpulseReading r = default(ShellImpulseReading);
		r.injected = new Vector3((float)_injX.Value, (float)_injY.Value, (float)_injZ.Value);
		r.injectedMagnitude = Math.Sqrt(_injY.Value * _injY.Value + _injX.Value * _injX.Value + _injZ.Value * _injZ.Value);
		r.scalarImpulse = _scalar.Value;
		r.predicted = _predictedRatio;
		r.tolerance = Tolerance;
		r.drivenCount = (_driven != null) ? _driven.Length : 0;
		r.onAxisDropped = _onAxisDropped;
		r.zeroDirectionDropped = _zeroDirDropped;
		r.drivenMassKg = _drivenMassKg;
		r.resumed = _resumed;
		r.why = "";
		if (r.scalarImpulse > 0.0)
		{
			r.ratio = r.injectedMagnitude / r.scalarImpulse;
			if (_resumed)
			{
				r.why = "замер с момента продолжения: часть рампы выдана до контрольной точки";
			}
			double deviation = Math.Abs(r.ratio - r.predicted);
			if (deviation > (double)DeadInstrumentTolerance)
			{
				r.status = ShellImpulseStatus.DeadInstrument;
				r.why = ((r.why.Length >= 1) ? (r.why + "; ") : "") + string.Format("замер и замкнутая форма разошлись на {0:E2} при допуске сверки ", deviation) + string.Format("{0:E0}: скобка мерит не добавку привода", DeadInstrumentTolerance);
			}
			else if (r.ratio <= (double)Tolerance)
			{
				r.status = ShellImpulseStatus.WithinTolerance;
			}
			else
			{
				r.status = ShellImpulseStatus.Exceeded;
				r.why = ((r.why.Length >= 1) ? (r.why + "; ") : "") + (SabotageOpenBase ? "диверсия: полость открыта с донца, суммарный импульс обязан быть ненулевым" : "стенка разгоняется несимметрично: полость смещена, открыта либо корпус деформирован сильнее, чем допускает контроль");
			}
		}
		else
		{
			r.status = ShellImpulseStatus.NotFired;
			if (SabotageNeverFires)
			{
				r.why = "диверсия E2: инициации нет";
			}
			else if (SabotageZeroVelocity)
			{
				r.why = "диверсия E1: скорость разлёта нулевая";
			}
			else if (_resumed)
			{
				r.why = string.Format("рампа выдала {0:F0} из {1:F0} м/с ДО контрольной ", DeliveredMps, velocityMps) + "точки; после продолжения приращений не было — мерить на этом отрезке нечего, а число контроля за отрезок ДО точки НЕ СОХРАНЕНО (паспорт печатается только у завершённого прогона)";
			}
			else if (ContactTimeS < 0f)
			{
				r.why = "касание не наступило";
			}
			else
			{
				r.why = "рампа ещё не выдала ни одного приращения";
			}
		}
		return r;
	}

	public string BurstPlace()
	{
		if (!Fired)
		{
			return "место импульса: ИМПУЛЬС НЕ СОСТОЯЛСЯ В ОКНЕ" + ((ContactTimeS < 0f) ? " (касания не было)" : " (касание было, пуск за концом окна)");
		}
		if (_axisPointAtFire.sqrMagnitude <= 0f)
		{
			return "место импульса: НЕ СНЯТО — направления не выбирались (" + (SabotageZeroVelocity ? "диверсия E1: скорость разлёта нулевая" : "рампа не дала ни одного приращения на этом отрезке") + ")";
		}
		float dot = _plateDir.x * _axisPointAtFire.x + _plateDir.y * _axisPointAtFire.y + _plateDir.z * _axisPointAtFire.z;
		float face = dot - _plateFaceZ;
		float rear = _plateRearZ - dot;
		string place;
		if (face > 0f)
		{
			place = string.Format("ПЕРЕД лицевой гранью, {0:F2} мм", face * 1000f);
		}
		else if (rear > 0f)
		{
			place = string.Format("ЗА тыльной плоскостью, {0:F2} мм", rear * 1000f);
		}
		else
		{
			place = string.Format("ВНУТРИ плиты, {0:F2} мм от лицевой грани", -face * 1000f);
		}
		return string.Format("место импульса: {0} (мишень — тело {1} \"{2}\", ", place, _plateBody, _plateName) + string.Format("толщина {0:F1} мм)", _plateThickM * 1000f) + (_resumed ? " ⚠ замер с момента ПРОДОЛЖЕНИЯ: точка снята заново, а не в момент импульса" : "");
	}

	public override string Passport()
	{
		string[] array = new string[18];
		array[0] = "корпус: взрыватель ";
		array[1] = ((initiationDelayS > 0f) ? "донный (каморник)" : "головной (фугас)");
		array[2] = ", касание ";
		array[3] = ((ContactTimeS < 0f) ? "НЕ НАСТУПИЛО" : ((ContactTimeS * 1000000f).ToString("F2") + " мкс"));
		array[4] = ", пуск ";
		array[5] = ((FireTimeS < 0f) ? "—" : ((FireTimeS * 1000000f).ToString("F2") + " мкс"));
		array[6] = ", ";
		array[7] = string.Format("задержка {0:F2} мкс x {1:F2}, ", initiationDelayS * 1000000f, delayMultiplier * 1000000f);
		array[8] = string.Format("рампа {0:F2} мкс, выдано {1:F0} из {2:F0} м/с, ", rampS * 1000000f, DeliveredMps, velocityMps);
		array[9] = string.Format("опросов {0}; ", Probes);
		array[10] = BurstPlace();
		array[11] = "; ";
		array[12] = Impulse.Describe();
		array[13] = (SabotageZeroVelocity ? "  [ДИВЕРСИЯ E1: V = 0]" : "");
		array[14] = (SabotageNeverFires ? "  [ДИВЕРСИЯ E2: инициации нет]" : "");
		array[15] = (SabotageNoRemainder ? "  [ДИВЕРСИЯ S28: последний шаг окна НЕ довыдаёт остаток]" : "");
		array[16] = (SabotageOpenBase ? "  [ДИВЕРСИЯ: ПОЛОСТЬ ОТКРЫТА С ДОНЦА]" : "");
		array[17] = ((next != null) ? ("  ‖ следующий привод: " + next.Passport()) : "");
		return string.Concat(array);
	}
}
