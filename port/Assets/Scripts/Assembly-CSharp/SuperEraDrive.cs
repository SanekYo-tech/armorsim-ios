using UnityEngine;

public class SuperEraDrive
{
	public struct State
	{
		public bool fired;

		public float contactTimeS;

		public float fireTimeS;

		public float deliveredMps;

		public float nextProbeS;

		public int probes;
	}

	public int frontBody;

	public int rearBody;

	public int triggerBody;

	public float velocityMps;

	public float initiationDelayS;

	public float rampS;

	public float triggerDistanceM;

	public bool SabotageZeroVelocity;

	public bool SabotageNeverFires;

	public float delayMultiplier;

	public bool SabotageNoRemainder;

	public bool driveFront;

	public bool driveRear;

	public SuperEraDrive next;

	public static float ProbeEveryS = 2E-07f;

	protected float _nextProbeS;

	private Vector3[] _probe;

	private Vector3[] _vel;

	private Vector3 _frontDir;

	private Vector3 _rearDir;

	private float _frontFaceZ;

	protected bool _prepared;

	public bool Fired { get; protected set; }

	public float ContactTimeS { get; protected set; }

	public float FireTimeS { get; protected set; }

	public float DeliveredMps { get; protected set; }

	public int Probes { get; protected set; }

	public SuperEraDrive()
	{
		frontBody = -1;
		rearBody = -1;
		rampS = 5E-06f;
		delayMultiplier = 1f;
		driveFront = true;
		driveRear = true;
		ContactTimeS = -1f;
		FireTimeS = -1f;
	}

	public virtual State Snapshot()
	{
		State result = default(State);
		result.fired = Fired;
		result.contactTimeS = ContactTimeS;
		result.fireTimeS = FireTimeS;
		result.deliveredMps = DeliveredMps;
		result.nextProbeS = _nextProbeS;
		result.probes = Probes;
		return result;
	}

	public virtual void Restore(in State s)
	{
		Fired = s.fired;
		ContactTimeS = s.contactTimeS;
		FireTimeS = s.fireTimeS;
		DeliveredMps = s.deliveredMps;
		_nextProbeS = s.nextProbeS;
		Probes = s.probes;
	}

	public virtual void Prepare(SuperBaker.Baked baked)
	{
		if (next != null)
		{
			next.Prepare(baked);
		}
		if (frontBody < 0 || rearBody < 0)
		{
			return;
		}
		SuperBaker.Body front = baked.bodies[frontBody];
		_frontDir = front.normal.normalized;
		_rearDir = -_frontDir;
		_frontFaceZ = Vector3.Dot(front.faceCentre, _frontDir);
		_probe = new Vector3[baked.bodies[triggerBody].count];
		_vel = new Vector3[Mathf.Max(baked.bodies[frontBody].count, baked.bodies[rearBody].count)];
		_nextProbeS = 0f;
		_prepared = true;
	}

	public virtual void AfterStep(SuperKernelGPU g, SuperBaker.Baked baked, float tS, float dtS)
	{
		if (next != null)
		{
			next.AfterStep(g, baked, tS, dtS);
		}
		if (!_prepared || SabotageNeverFires)
		{
			return;
		}
		if (ContactTimeS < 0f)
		{
			if (_nextProbeS > tS)
			{
				return;
			}
			_nextProbeS = tS + ProbeEveryS;
			SuperBaker.Body body = baked.bodies[triggerBody];
			g.DownloadPositionRange(_probe, body.start, body.count);
			Probes++;
			float num = float.MaxValue;
			for (int i = 0; i < body.count; i++)
			{
				float num2 = Vector3.Dot(_probe[i], _frontDir) - _frontFaceZ;
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
		float num3 = tS - FireTimeS;
		if (num3 > rampS)
		{
			return;
		}
		if (SabotageZeroVelocity || velocityMps <= 0f)
		{
			Fired = true;
			return;
		}
		float num4 = NextIncrement(velocityMps, dtS);
		num4 = WithLastStepRemainder(num4, num3, dtS, velocityMps);
		if (num4 <= 0f)
		{
			return;
		}
		if (driveFront)
		{
			AddVelocity(g, baked, frontBody, _frontDir * num4);
		}
		if (driveRear)
		{
			AddVelocity(g, baked, rearBody, _rearDir * num4);
		}
		Fired = true;
		DeliveredMps += num4;
	}

	protected float NextIncrement(float target, float dtS)
	{
		return Mathf.Min(target * dtS / Mathf.Max(1E-09f, rampS), target - DeliveredMps);
	}

	protected float WithLastStepRemainder(float dv, float since, float dtS, float target)
	{
		if (!SabotageNoRemainder && since + dtS > rampS)
		{
			return target - DeliveredMps;
		}
		return dv;
	}

	private void AddVelocity(SuperKernelGPU g, SuperBaker.Baked baked, int body, Vector3 dv)
	{
		SuperBaker.Body b = baked.bodies[body];
		g.DownloadVelocityRange(_vel, b.start, b.count);
		for (int i = 0; i < b.count; i++)
		{
			_vel[i] += dv;
		}
		g.UploadVelocityRange(_vel, b.start, b.count);
	}

	public virtual string Passport()
	{
		string[] array = new string[13];
		array[0] = "инициация: касание ";
		array[1] = ((ContactTimeS < 0f) ? "НЕ НАСТУПИЛО" : (ContactTimeS * 1000000f).ToString("F2") + " мкс");
		array[2] = ", пуск ";
		array[3] = ((FireTimeS < 0f) ? "—" : (FireTimeS * 1000000f).ToString("F2") + " мкс");
		array[4] = ", ";
		array[5] = string.Format("задержка {0:F2} мкс × {1:F2}, ", initiationDelayS * 1000000f, delayMultiplier);
		array[6] = string.Format("рампа {0:F2} мкс, выдано {1:F0} из {2:F0} м/с, ", rampS * 1000000f, DeliveredMps, velocityMps);
		array[7] = string.Format("опросов {0}", Probes);
		array[8] = (SabotageZeroVelocity ? "  [ДИВЕРСИЯ E1: V_p = 0]" : "");
		array[9] = (SabotageNeverFires ? "  [ДИВЕРСИЯ E2: инициации нет]" : "");
		array[10] = (SabotageNoRemainder ? "  [ДИВЕРСИЯ S28: последний шаг окна НЕ довыдаёт остаток]" : "");
		array[11] = (driveFront ? (driveRear ? "" : "  [ТОЛЬКО ПЕРЕДНЯЯ]") : (driveRear ? "  [ТОЛЬКО ЗАДНЯЯ]" : "  [НИ ОДНОЙ]"));
		array[12] = ((next != null) ? ("  ‖ следующая кассета: " + next.Passport()) : "");
		return string.Concat(array);
	}
}
