using System;
using UnityEngine;

public class T8_AsbPlug : MonoBehaviour
{
	public struct Plug
	{
		public float failureRadius;

		public int ahead;

		public int loops;

		public float clearRadius;

		public int plugElems;

		public float plugMassKg;

		public float plugAdvance;
	}

	[ContextMenu("T8 — пробка через полосу сдвига")]
	public void RunOne()
	{
		int pass = 0;
		int total = 0;
		CT.Run("T8", "пробка", () => Check(), ref pass, ref total);
	}

	public static bool Check()
	{
		ContinuumScenario s = new ContinuumScenario();
		s.name = "T8: тупой стержень → RHA 10 мм";
		s.rodDiameterM = 0.02f;
		s.rodMaterial = ContinuumMaterialId.WHA;
		s.rodLengthM = 0.06f;
		s.impactVelocityMps = 800f;
		s.targetThicknessM = 0.01f;
		s.targetMaterial = ContinuumMaterials.ActiveRhaSet;
		s.elementsPerDiameter = 20f;
		s.durationS = 6e-05f;
		s.outputFrames = 60;
		Plug plug = default(Plug);
		ContinuumBackend.Run withFade = Measure(s, asb: true, ref plug);
		ContinuumScenario noFade = s.Clone();
		noFade.name += " (без спада прочности)";
		Plug plug2 = default(Plug);
		ContinuumBackend.Run withoutFade = Measure(noFade, asb: false, ref plug2);
		Debug.Log("    " + string.Format("со спадом:  мёртвых элементов преграды {0}, ", withFade.diag.elementsEroded) + string.Format("средний радиус разрушения {0:F2} мм (D/2 = {1:F2} мм), ", plug.failureRadius * 1000f, 10f) + string.Format("вытолкнуто за тыльную грань {0} элементов, контуров преграды {1}", plug.ahead, plug.loops));
		Debug.Log("    " + string.Format("без спада:  мёртвых элементов преграды {0}, ", withoutFade.diag.elementsEroded) + string.Format("средний радиус разрушения {0:F2} мм, ", plug2.failureRadius * 1000f) + string.Format("вытолкнуто {0}, контуров {1}", plug2.ahead, plug2.loops));
		Debug.Log("    " + "со спадом:  на полутолщине преграда очищена до радиуса " + string.Format("{0:F2} мм = {1:F2}·(D/2); ", plug.clearRadius * 1000f, plug.clearRadius / 0.01f) + string.Format("тело пробки {0} живых элементов, {1:F2} г, ", plug.plugElems, plug.plugMassKg * 1000f) + string.Format("ушло вперёд на {0:F2} мм за тыльную грань", plug.plugAdvance * 1000f));
		Debug.Log("    " + string.Format("без спада:  очищено до {0:F2} мм = ", plug2.clearRadius * 1000f) + string.Format("{0:F2}·(D/2); тело {1} элементов, ", plug2.clearRadius / 0.01f, plug2.plugElems) + string.Format("{0:F2} г, вперёд на {1:F2} мм", plug2.plugMassKg * 1000f, plug2.plugAdvance * 1000f));
		bool ok = CT.True("вещество преграды вытолкнуто за тыльную грань (пробка есть)", plug.ahead > 0);
		bool ratioOk = CT.InRange("средний радиус поверхности разрушения / (D/2)", plug.failureRadius / 0.01, 0.6, 1.6);
		bool contrast = CT.True("спад прочности МЕНЯЕТ картину (контрфакт различим)", withFade.diag.elementsEroded != withoutFade.diag.elementsEroded || plug.ahead != plug2.ahead);
		bool slitOk = CT.True("щель видна: на полутолщине очищено до " + string.Format("{0:F2} мм ", plug.clearRadius * 1000f) + string.Format("({0:F2}·(D/2), нужно ≥ 0.5)", plug.clearRadius / 0.01f), plug.clearRadius >= 0.005f);
		bool bodyOk = CT.True("пробка — связное тело (" + string.Format("{0} живых элементов, ", plug.plugElems) + string.Format("{0:F2} г)", plug.plugMassKg * 1000f), plug.plugElems >= 4 && plug.plugMassKg > 0f);
		bool nanOk = CT.True("NaN не появилось", !CT.AnyNaN(withFade.mesh));
		Debug.Log("    " + "⚠ это подтверждение ЗАДУМАННОЙ морфологии, а не предсказание физики: Δε_fade и m для RHA — калибровочные параметры проекта");
		withFade.ReleaseWorkingSet();
		withoutFade.ReleaseWorkingSet();
		return ok && ratioOk && contrast && slitOk && bodyOk && nanOk;
	}

	private static ContinuumBackend.Run Measure(ContinuumScenario s, bool asb, ref Plug p)
	{
		ContinuumScenario work = s.Clone();
		work.enableAsbFade = asb;
		ContinuumBackend.Run run = CT.RunToEnd(work);
		AxiMesh mesh = run.mesh;
		p.ahead = 0;
		p.plugElems = 0;
		p.plugAdvance = 0f;
		int loops = 0;
		double radiusSum = 0.0;
		float t = work.targetThicknessM;
		float minRc = float.MaxValue;
		for (int e = 0; e < mesh.elemCount; e++)
		{
			if (mesh.elemAlive[e] != 1)
			{
				continue;
			}
			if (mesh.elemBody[e] == 0)
			{
				loops++;
				radiusSum += mesh.elemRc[e];
				continue;
			}
			float rAvg = (mesh.nodeR[mesh.elemNodes[e * 4]] + mesh.nodeR[mesh.elemNodes[e * 4 + 1]] + mesh.nodeR[mesh.elemNodes[e * 4 + 2]] + mesh.nodeR[mesh.elemNodes[e * 4 + 3]]) * 0.25f;
			if (rAvg > t * 0.3f && rAvg < t * 0.7f)
			{
				if (minRc > mesh.elemRc[e])
				{
					minRc = mesh.elemRc[e];
				}
			}
			if (rAvg > t && mesh.elemRc[e] < work.rodDiameterM * 0.75f)
			{
				p.ahead++;
				p.plugElems++;
				p.plugMassKg += mesh.elemMass[e];
				if (rAvg - t > p.plugAdvance)
				{
					p.plugAdvance = rAvg - t;
				}
			}
		}
		p.clearRadius = ((minRc == float.MaxValue) ? (work.rodDiameterM * 0.5f) : minRc);
		if (loops > 0)
		{
			p.failureRadius = (float)(radiusSum / (double)loops);
		}
		else
		{
			p.failureRadius = 0f;
		}
		for (int i = 0; i < run.Timeline.frames.Count; i++)
		{
			int targetLoops = run.Timeline.frames[i].targetLoopCount;
			if (targetLoops > p.loops)
			{
				p.loops = targetLoops;
			}
		}
		return run;
	}
}
