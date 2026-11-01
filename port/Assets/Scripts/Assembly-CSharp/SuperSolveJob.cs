using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

public static class SuperSolveJob
{
	private sealed class Runner : MonoBehaviour
	{
	}

	public enum RunEnd
	{
		Done = 0,
		Invalid = 1,
		Failed = 2,
		Refused = 3,
		Cancelled = 4
	}

	public sealed class Outcome
	{
		public RunEnd end;

		public string tapePath;

		public int frames;

		public long tapeBytes;

		public string reason;

		public string reasonKey;

		public object[] reasonArgs;

		public bool resumed;

		public SuperRunRecord record;

		public string run;

		public string hollowKey;

		public object[] hollowArgs;
	}

	private class Cancelled : Exception
	{
	}

	private struct DisplayState
	{
		public int vsync;

		public int targetFps;

		public int renderInterval;

		public bool taken;
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass86_0
	{
		public SuperJetSpec jetForPlan;

		public IList<SuperPlateSpec> plates;

		public float durationS;

		public float margin;

		internal SuperScenePlan _003CJetRoutine_003Eb__0()
		{
			return SuperScenePlan.For(in jetForPlan, plates, SuperPreset.SpacingM, durationS, margin);
		}
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass86_1
	{
		public int seen;

		public int expected;

		internal void _003CJetRoutine_003Eb__1()
		{
			seen++;
			SuperSolveJob.Progress = Mathf.Max(SuperSolveJob.Progress, Mathf.Clamp01((float)seen / (float)expected));
			SuperSolveJob.Frames = seen;
			if (SuperSolveJob.CancelRequested)
			{
				throw new Cancelled();
			}
		}
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass95_0
	{
		public int seen;

		public int expected;

		internal void _003CResumeRoutine_003Eb__0()
		{
			seen++;
			SuperSolveJob.Progress = Mathf.Max(SuperSolveJob.Progress, Mathf.Clamp01((float)seen / (float)expected));
			SuperSolveJob.Frames = seen;
			if (SuperSolveJob.CancelRequested)
			{
				throw new Cancelled();
			}
		}
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass98_0
	{
		public SuperRodSpec rodForPlan;

		public IList<SuperPlateSpec> plates;

		public float margin;

		internal SuperScenePlan _003CRodRoutine_003Eb__0()
		{
			return SuperScenePlan.For(in rodForPlan, plates, SuperPreset.SpacingM, SuperPreset.DurationS, margin);
		}
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass98_1
	{
		public int seen;

		public int expected;

		internal void _003CRodRoutine_003Eb__1()
		{
			seen++;
			SuperSolveJob.Progress = Mathf.Max(SuperSolveJob.Progress, Mathf.Clamp01((float)seen / (float)expected));
			SuperSolveJob.Frames = seen;
			if (SuperSolveJob.CancelRequested)
			{
				throw new Cancelled();
			}
		}
	}

	[CompilerGenerated]
	private sealed class _003CJetRoutine_003Ed__86 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public IList<SuperPlateSpec> plates;

		public SuperEraDrive drive;

		public SuperJetSpec jet;

		public float cutAzimuthDeg;

		public ComputeShader cs;

		public byte[] decor;

		private _003C_003Ec__DisplayClass86_0 _003C_003E8__1;

		private float _003CsavedProbe_003E5__2;

		private string _003CtapeName_003E5__3;

		private string _003CtmpPath_003E5__4;

		private string _003CfinalPath_003E5__5;

		private DisplayState _003Cdisplay_003E5__6;

		private SuperTapeRecorder.RecBox _003Cbox_003E5__7;

		private IEnumerator _003Cit_003E5__8;

		private bool _003Ccancelled_003E5__9;

		private Exception _003Cfailure_003E5__10;

		object IEnumerator<object>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003CJetRoutine_003Ed__86(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			if (_003C_003E1__state == -3 || _003C_003E1__state == 1)
			{
				_003C_003Em__Finally1();
			}
		}

		private bool MoveNext()
		{
			bool flag = _003C_003E1__state == 0;
			if (flag)
			{
				_003C_003E1__state = -1;
				_003C_003Ec__DisplayClass86_0 @class = new _003C_003Ec__DisplayClass86_0();
				@class.plates = plates;
				SuperSolveJob.Running = true;
				SuperSolveJob.Frames = 0;
				SuperSolveJob.Expected = 0;
				SuperSolveJob._progressSw = Stopwatch.StartNew();
				SuperSolveJob.Progress = 0f;
				SuperSolveJob.CancelRequested = false;
				SuperSolveJob.LastTapePath = null;
				_003CsavedProbe_003E5__2 = SuperEraDrive.ProbeEveryS;
				if (drive != null)
				{
					SuperEraDrive.ProbeEveryS = Mathf.Min(_003CsavedProbe_003E5__2, SuperPreset.SpacingM / (Mathf.Max(1f, jet.vTipMps) * 4f));
				}
				float maxObliquityDeg = SuperScenePlan.MaxObliquityDeg(@class.plates);
				@class.durationS = Mathf.Min(SuperPreset.DurationS, JetWindowS(in jet, @class.plates));
				@class.margin = SuperPreset.MarginFor(maxObliquityDeg, jet.vTipMps, @class.durationS);
				string tapeDir = SuperTapeRecorder.TapeDir;
				_003CtapeName_003E5__3 = NewTapeName();
				_003CtmpPath_003E5__4 = Path.GetFullPath(Path.Combine(tapeDir, _003CtapeName_003E5__3 + ".writing"));
				_003CfinalPath_003E5__5 = Path.GetFullPath(Path.Combine(tapeDir, _003CtapeName_003E5__3));
				Debug.Log(string.Concat(string.Format("[Super] Расчёт СТРУИ: плит {0}, шаг {1:F2} мм, ", @class.plates.Count, SuperPreset.SpacingM * 1000f), string.Format("окно {0:F0} мкс", @class.durationS * 1000000f), (SuperPreset.DurationS > @class.durationS) ? (string.Format(" (выведено из сцены; панельные {0:F0} мкс ", SuperPreset.DurationS * 1000000f) + "струе не нужны — она тратится раньше)") : "", string.Format(", габарит {0:F0} мм; ", @class.margin * 1000f), string.Format("L = {0:F1} мм, ⌀ = {1:F2} мм, ", jet.lengthM * 1000f, jet.diameterM * 1000f), string.Format("{0:F0}…{1:F0} м/с.", jet.vTailMps, jet.vTipMps)));
				@class.jetForPlan = jet;
				_003C_003E8__1 = @class;
				RecordBegin(SafePlan(new Func<SuperScenePlan>(@class._003CJetRoutine_003Eb__0)), _003CtapeName_003E5__3, cutAzimuthDeg);
				SuperSolveJob._stepLoopMarked = false;
				SuperSolveJob._stepLoopEnded = false;
				SuperSolveJob._meter = new SuperRunMeter();
				SuperSolveJob._meter.Begin();
				_003Cdisplay_003E5__6 = EnterSolveDisplay();
			}
			else
			{
				if (_003C_003E1__state != 1)
				{
					return false;
				}
			}
			try
			{
				_003C_003E1__state = -3;
				if (flag)
				{
					_003C_003Ec__DisplayClass86_1 displayClass = new _003C_003Ec__DisplayClass86_1();
					float cadenceS = Mathf.Max(2e-07f, _003C_003E8__1.durationS / 150f);
					displayClass.expected = Mathf.Max(1, (int)(_003C_003E8__1.durationS / cadenceS));
					displayClass.seen = 0;
					SuperCheckpoint.KeepLatestRunOnly(Path.GetFileNameWithoutExtension(_003CtapeName_003E5__3));
					SuperCheckpoint.ActiveRun = Path.GetFileNameWithoutExtension(_003CtapeName_003E5__3);
					SuperSolveJob.Expected = displayClass.expected;
					_003Cbox_003E5__7 = new SuperTapeRecorder.RecBox();
					SuperSolveJob._pumpBox = _003Cbox_003E5__7;
					SuperSolveJob._progBuilt = -1;
					SuperSolveJob._progTicksAtBuilt = 0L;
					SuperSolveJob._progTicksPerFrame = 200.0;
					_003Cit_003E5__8 = SuperTapeRecorder.RecordCore(cs, default(SuperRodSpec), default(SuperShellSpec), jet, SuperTapeRecorder.Kind.Jet, _003C_003E8__1.plates, SuperPreset.SpacingM, _003C_003E8__1.durationS, _003CtapeName_003E5__3 + ".writing", _003C_003E8__1.margin, cadenceS, SuperTapeRecorder.TapeDir, new Action(displayClass._003CJetRoutine_003Eb__1), drive, checkpoint: true, null, _003Cbox_003E5__7, new Action(CancelPump), SuperTapeRecorder.GamePipeline, SuperTapeRecorder.GamePaced, gameDomain: true, decor);
					_003Ccancelled_003E5__9 = false;
					_003Cfailure_003E5__10 = null;
				}
				while (true)
				{
					bool flag2 = (SuperSolveJob._meter != null) ? SuperSolveJob._meter.Move(_003Cit_003E5__8) : _003Cit_003E5__8.MoveNext();
					if (!flag2)
					{
						break;
					}
					SuperSolveJob.MarkStepLoopFromPump();
					_003C_003E2__current = null;
					_003C_003E1__state = 1;
					return true;
				}
			}
			catch (Cancelled)
			{
				_003Ccancelled_003E5__9 = true;
			}
			catch (Exception ex)
			{
				_003Cfailure_003E5__10 = ex;
			}
			SuperSolveJob.Conclude(_003Ccancelled_003E5__9, _003Cfailure_003E5__10, _003Cbox_003E5__7.s, _003CtapeName_003E5__3, _003CtmpPath_003E5__4, _003CfinalPath_003E5__5, drive, "[Super] Расчёт струи упал: ", _003C_003E8__1.durationS);
			_003Cbox_003E5__7 = null;
			_003Cit_003E5__8 = null;
			_003Cfailure_003E5__10 = null;
			_003C_003Em__Finally1();
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		private void _003C_003Em__Finally1()
		{
			_003C_003E1__state = -1;
			SuperSolveJob.EndMeter();
			if (_003Cdisplay_003E5__6.taken)
			{
				QualitySettings.vSyncCount = _003Cdisplay_003E5__6.vsync;
				Application.targetFrameRate = _003Cdisplay_003E5__6.targetFps;
				OnDemandRendering.renderFrameInterval = _003Cdisplay_003E5__6.renderInterval;
			}
			SuperEraDrive.ProbeEveryS = _003CsavedProbe_003E5__2;
			SuperSolveJob.Running = false;
			SuperSolveJob._progressSw = null;
			SuperHud.RefreshResumeProbe();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}
	}

	[CompilerGenerated]
	private sealed class _003CResumeRoutine_003Ed__95 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public SuperCheckpoint.Data d;

		public ComputeShader cs;

		private SuperCheckpoint.Scene _003Csc_003E5__2;

		private SuperRunMeter _003CresumeMeter_003E5__3;

		private DisplayState _003Cdisplay_003E5__4;

		private float _003CsavedProbe_003E5__5;

		private SuperEraDrive _003Cdrive_003E5__6;

		private string _003CtmpPath_003E5__7;

		private string _003CfinalPath_003E5__8;

		private SuperTapeRecorder.RecBox _003Cbox_003E5__9;

		private IEnumerator _003Cit_003E5__10;

		private bool _003Ccancelled_003E5__11;

		private Exception _003Cfailure_003E5__12;

		object IEnumerator<object>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003CResumeRoutine_003Ed__95(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			if (_003C_003E1__state == -3 || _003C_003E1__state == 1)
			{
				_003C_003Em__Finally1();
			}
		}

		private bool MoveNext()
		{
			bool flag = _003C_003E1__state == 0;
			_003C_003Ec__DisplayClass95_0 @class = null;
			if (flag)
			{
				_003C_003E1__state = -1;
				@class = new _003C_003Ec__DisplayClass95_0();
				string mismatch;
				if (d.damageEnabled != SuperDamage.Enabled || d.coherentFlyers != SuperEra.CoherentFlyerPlates || d.softening != SuperDamage.SofteningCoefficient || d.coherentFloorPa != SuperDamage.CoherentTensionFloorPa || d.capN != SuperDamage.PressureCapFactorN)
				{
					mismatch = "[Super] Конфигурация решателя изменилась с момента точки — продолжение было бы другим прогоном под тем же именем. Отброшено.";
				}
				else
				{
					mismatch = XiStateMismatch(d);
					if (mismatch != null)
					{
						mismatch = "[Super] " + mismatch;
					}
				}
				if (mismatch != null)
				{
					Fail(mismatch);
					SuperCheckpoint.DiscardAll(d.run);
					SuperHud.RefreshResumeProbe();
					return false;
				}
				_003Csc_003E5__2 = d.scene;
				SuperSolveJob.Running = true;
				SuperSolveJob.Frames = d.frames;
				SuperSolveJob.Expected = 0;
				SuperSolveJob._progressSw = Stopwatch.StartNew();
				SuperSolveJob.CancelRequested = false;
				SuperSolveJob.LastTapePath = null;
				SuperSolveJob.LastRecord = null;
				_003CresumeMeter_003E5__3 = new SuperRunMeter();
				SuperSolveJob._stepLoopMarked = false;
				SuperSolveJob._stepLoopEnded = false;
				SuperSolveJob._meter = _003CresumeMeter_003E5__3;
				_003CresumeMeter_003E5__3.Begin();
				_003Cdisplay_003E5__4 = EnterSolveDisplay();
				_003CsavedProbe_003E5__5 = SuperEraDrive.ProbeEveryS;
				SuperEraDrive.ProbeEveryS = ((_003Csc_003E5__2.probeEveryS > 0f) ? _003Csc_003E5__2.probeEveryS : _003CsavedProbe_003E5__5);
				_003Cdrive_003E5__6 = SuperCheckpoint.BuildDrives(_003Csc_003E5__2);
				_003CtmpPath_003E5__7 = _003Csc_003E5__2.tapeTmpPath;
				_003CfinalPath_003E5__8 = Path.GetFullPath(Path.Combine(_003Csc_003E5__2.tapeDir, _003Csc_003E5__2.tapeName.EndsWith(".writing") ? _003Csc_003E5__2.tapeName.Substring(0, _003Csc_003E5__2.tapeName.Length - ".writing".Length) : (_003Csc_003E5__2.tapeName + ".resumed.tape")));
				SuperCheckpoint.ActiveRun = d.run;
				@class.expected = Mathf.Max(1, (int)(_003Csc_003E5__2.durationS / _003Csc_003E5__2.cadenceS));
				SuperSolveJob.Expected = @class.expected;
				@class.seen = d.frames;
				SuperSolveJob.Progress = Mathf.Clamp01((float)@class.seen / (float)@class.expected);
				Debug.Log(string.Format("[Super] Продолжение с точки: шаг {0} из {1}, кадров {2}, ", d.step, d.steps, d.frames) + string.Format("t = {0:F1} мкс из {1:F0}; лента {2} ({3} байт).", d.simTimeS * 1000000f, _003Csc_003E5__2.durationS * 1000000f, _003CtmpPath_003E5__7, d.tapeBytes));
			}
			else
			{
				if (_003C_003E1__state != 1)
				{
					return false;
				}
			}
			try
			{
				_003C_003E1__state = -3;
				if (flag)
				{
					Action onFrame = new Action(@class._003CResumeRoutine_003Eb__0);
					_003Cbox_003E5__9 = new SuperTapeRecorder.RecBox();
					SuperSolveJob._pumpBox = _003Cbox_003E5__9;
					SuperSolveJob._progBuilt = -1;
					SuperSolveJob._progTicksAtBuilt = 0L;
					SuperSolveJob._progTicksPerFrame = 200.0;
					_003Cit_003E5__10 = SuperTapeRecorder.RecordCore(cs, _003Csc_003E5__2.rod, default(SuperShellSpec), _003Csc_003E5__2.jet, (_003Csc_003E5__2.kind == 2) ? SuperTapeRecorder.Kind.Jet : SuperTapeRecorder.Kind.Rod, _003Csc_003E5__2.plates, _003Csc_003E5__2.spacing, _003Csc_003E5__2.durationS, _003Csc_003E5__2.tapeName, _003Csc_003E5__2.marginM, _003Csc_003E5__2.cadenceS, _003Csc_003E5__2.tapeDir, onFrame, _003Cdrive_003E5__6, checkpoint: true, d, _003Cbox_003E5__9, new Action(CancelPump), SuperTapeRecorder.GamePipeline, SuperTapeRecorder.GamePaced, gameDomain: true, null);
					_003Ccancelled_003E5__11 = false;
					_003Cfailure_003E5__12 = null;
				}
				while (true)
				{
					bool flag2 = (SuperSolveJob._meter != null) ? SuperSolveJob._meter.Move(_003Cit_003E5__10) : _003Cit_003E5__10.MoveNext();
					if (!flag2)
					{
						break;
					}
					SuperSolveJob.MarkStepLoopFromPump();
					_003C_003E2__current = null;
					_003C_003E1__state = 1;
					return true;
				}
			}
			catch (Cancelled)
			{
				_003Ccancelled_003E5__11 = true;
			}
			catch (Exception ex)
			{
				_003Cfailure_003E5__12 = ex;
			}
			if (!_003Ccancelled_003E5__11)
			{
				if (_003Cfailure_003E5__12 == null)
				{
					SuperTapeRecorder.Summary s = _003Cbox_003E5__9.s;
					if (_003Cdrive_003E5__6 != null)
					{
						Debug.Log("[Super] Привод ДЗ: " + _003Cdrive_003E5__6.Passport());
					}
					if (!s.ok && s.frames <= d.frames)
					{
						SuperCheckpoint.ActiveRun = null;
						Fail("[Super] Продолжить не удалось: " + s.failure + " Лента и контрольная точка оставлены; оборванная запись по-прежнему в библиотеке.");
						SetOutcome(RunEnd.Failed, File.Exists(_003CtmpPath_003E5__7) ? _003CtmpPath_003E5__7 : "", d.frames, FileLen(_003CtmpPath_003E5__7), s.failure, resumed: true, d.run, s.failureKey, s.failureArgs);
					}
					else
					{
						if (!s.ok)
						{
							Debug.LogWarning("[Super] Расчёт помечен недействительным: " + s.failure + " — лента записана, но числа прогона недействительны.");
						}
						Finish(s, _003CtmpPath_003E5__7, _003CfinalPath_003E5__8);
						SetOutcome(s.ok ? RunEnd.Done : RunEnd.Invalid, _003CfinalPath_003E5__8, s.frames, s.fileBytes, s.ok ? "" : s.failure, resumed: true, d.run, s.ok ? "" : (s.failureKey ?? ""), s.ok ? null : s.failureArgs);
						AttachHollowLine(_003Cdrive_003E5__6, _003Csc_003E5__2.durationS);
					}
				}
				else
				{
					SuperCheckpoint.ActiveRun = null;
					string message = _003Cfailure_003E5__12.Message;
					Fail("[Super] Продолжение упало: " + message + KeptNote(_003CtmpPath_003E5__7));
					SetOutcome(RunEnd.Failed, File.Exists(_003CtmpPath_003E5__7) ? _003CtmpPath_003E5__7 : "", Frames, FileLen(_003CtmpPath_003E5__7), _003Cfailure_003E5__12.Message, resumed: true, d.run, null, null);
				}
			}
			else
			{
				SuperCheckpoint.ActiveRun = null;
				Debug.Log("[Super] Продолжение отменено игроком — оборванная лента и точки оставлены.");
				SetOutcome(RunEnd.Cancelled, "", 0, 0L, "", resumed: true, d.run, null, null);
			}
			_003Cbox_003E5__9 = null;
			_003Cit_003E5__10 = null;
			_003Cfailure_003E5__12 = null;
			_003C_003Em__Finally1();
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		private void _003C_003Em__Finally1()
		{
			_003C_003E1__state = -1;
			SuperSolveJob.EndMeter();
			if (_003Cdisplay_003E5__4.taken)
			{
				QualitySettings.vSyncCount = _003Cdisplay_003E5__4.vsync;
				Application.targetFrameRate = _003Cdisplay_003E5__4.targetFps;
				OnDemandRendering.renderFrameInterval = _003Cdisplay_003E5__4.renderInterval;
			}
			_003CresumeMeter_003E5__3.End();
			string text = ((_003CresumeMeter_003E5__3.cpuUtil >= 0.0) ? _003CresumeMeter_003E5__3.cpuUtil.ToString("P0") : "недоступно");
			Debug.Log(string.Format("[S25] Продолжение: наибольшее замирание главного потока {0:F0} мс ", _003CresumeMeter_003E5__3.maxMoveMs) + string.Format("(первый MoveNext {0:F0} мс), MoveNext {1}, ЦПУ {2}, ", _003CresumeMeter_003E5__3.firstMoveMs, _003CresumeMeter_003E5__3.moves, text) + string.Format("пик рабочего набора {0:F0} МБ.", (double)_003CresumeMeter_003E5__3.peakWorkingSetBytes * 9.5367431640625E-07));
			SuperEraDrive.ProbeEveryS = _003CsavedProbe_003E5__5;
			SuperSolveJob.Running = false;
			SuperSolveJob._progressSw = null;
			SuperHud.RefreshResumeProbe();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}
	}

	[CompilerGenerated]
	private sealed class _003CRodRoutine_003Ed__98 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public IList<SuperPlateSpec> plates;

		public SuperEraDrive drive;

		public SuperRodSpec rod;

		public float cutAzimuthDeg;

		public ComputeShader cs;

		public byte[] decor;

		private float _003CsavedProbe_003E5__2;

		private string _003CtapeName_003E5__3;

		private string _003CtmpPath_003E5__4;

		private string _003CfinalPath_003E5__5;

		private DisplayState _003Cdisplay_003E5__6;

		private SuperTapeRecorder.RecBox _003Cbox_003E5__7;

		private IEnumerator _003Cit_003E5__8;

		private bool _003Ccancelled_003E5__9;

		private Exception _003Cfailure_003E5__10;

		object IEnumerator<object>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003CRodRoutine_003Ed__98(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			if (_003C_003E1__state == -3 || _003C_003E1__state == 1)
			{
				_003C_003Em__Finally1();
			}
		}

		private bool MoveNext()
		{
			bool flag = _003C_003E1__state == 0;
			_003C_003Ec__DisplayClass98_0 @class = null;
			if (flag)
			{
				_003C_003E1__state = -1;
				@class = new _003C_003Ec__DisplayClass98_0();
				@class.plates = plates;
				SuperSolveJob.Running = true;
				SuperSolveJob.Frames = 0;
				SuperSolveJob.Expected = 0;
				SuperSolveJob._progressSw = Stopwatch.StartNew();
				SuperSolveJob.Progress = 0f;
				SuperSolveJob.CancelRequested = false;
				SuperSolveJob.LastTapePath = null;
				_003CsavedProbe_003E5__2 = SuperEraDrive.ProbeEveryS;
				if (drive != null)
				{
					SuperEraDrive.ProbeEveryS = Mathf.Min(_003CsavedProbe_003E5__2, SuperPreset.SpacingM / (Mathf.Max(1f, rod.velocityMps) * 4f));
				}
				float maxObliquityDeg = SuperScenePlan.MaxObliquityDeg(@class.plates);
				@class.margin = SuperPreset.MarginFor(maxObliquityDeg, rod.velocityMps, SuperPreset.DurationS);
				string tapeDir = SuperTapeRecorder.TapeDir;
				_003CtapeName_003E5__3 = NewTapeName();
				_003CtmpPath_003E5__4 = Path.GetFullPath(Path.Combine(tapeDir, _003CtapeName_003E5__3 + ".writing"));
				_003CfinalPath_003E5__5 = Path.GetFullPath(Path.Combine(tapeDir, _003CtapeName_003E5__3));
				Debug.Log(string.Concat(string.Format("[Super] Расчёт: плит {0}, шаг {1:F1} мм, ", @class.plates.Count, SuperPreset.SpacingM * 1000f), string.Format("окно {0:F0} мкс, габарит {1:F0} мм ", SuperPreset.DurationS * 1000000f, @class.margin * 1000f), string.Format("(наклон до {0:F0}°); конвейер кадров {1}, ", maxObliquityDeg, SuperTapeRecorder.GamePipeline), "темп шагов ", SuperTapeRecorder.GamePaced ? "вкл" : "выкл", "."));
				@class.rodForPlan = rod;
				RecordBegin(SafePlan(new Func<SuperScenePlan>(@class._003CRodRoutine_003Eb__0)), _003CtapeName_003E5__3, cutAzimuthDeg);
				SuperSolveJob._stepLoopMarked = false;
				SuperSolveJob._stepLoopEnded = false;
				SuperSolveJob._meter = new SuperRunMeter();
				SuperSolveJob._meter.Begin();
				_003Cdisplay_003E5__6 = EnterSolveDisplay();
			}
			else
			{
				if (_003C_003E1__state != 1)
				{
					return false;
				}
			}
			try
			{
				_003C_003E1__state = -3;
				if (flag)
				{
					_003C_003Ec__DisplayClass98_1 displayClass = new _003C_003Ec__DisplayClass98_1();
					float cadenceS = Mathf.Max(2e-07f, SuperPreset.DurationS / 150f);
					displayClass.expected = Mathf.Max(1, (int)(SuperPreset.DurationS / cadenceS));
					displayClass.seen = 0;
					SuperCheckpoint.KeepLatestRunOnly(Path.GetFileNameWithoutExtension(_003CtapeName_003E5__3));
					SuperCheckpoint.ActiveRun = Path.GetFileNameWithoutExtension(_003CtapeName_003E5__3);
					SuperSolveJob.Expected = displayClass.expected;
					_003Cbox_003E5__7 = new SuperTapeRecorder.RecBox();
					SuperSolveJob._pumpBox = _003Cbox_003E5__7;
					SuperSolveJob._progBuilt = -1;
					SuperSolveJob._progTicksAtBuilt = 0L;
					SuperSolveJob._progTicksPerFrame = 200.0;
					_003Cit_003E5__8 = SuperTapeRecorder.RecordCore(cs, rod, default(SuperShellSpec), default(SuperJetSpec), SuperTapeRecorder.Kind.Rod, @class.plates, SuperPreset.SpacingM, SuperPreset.DurationS, _003CtapeName_003E5__3 + ".writing", @class.margin, cadenceS, SuperTapeRecorder.TapeDir, new Action(displayClass._003CRodRoutine_003Eb__1), drive, checkpoint: true, null, _003Cbox_003E5__7, new Action(CancelPump), SuperTapeRecorder.GamePipeline, SuperTapeRecorder.GamePaced, gameDomain: true, decor);
					_003Ccancelled_003E5__9 = false;
					_003Cfailure_003E5__10 = null;
				}
				while (true)
				{
					bool flag2 = (SuperSolveJob._meter != null) ? SuperSolveJob._meter.Move(_003Cit_003E5__8) : _003Cit_003E5__8.MoveNext();
					if (!flag2)
					{
						break;
					}
					SuperSolveJob.MarkStepLoopFromPump();
					_003C_003E2__current = null;
					_003C_003E1__state = 1;
					return true;
				}
			}
			catch (Cancelled)
			{
				_003Ccancelled_003E5__9 = true;
			}
			catch (Exception ex)
			{
				_003Cfailure_003E5__10 = ex;
			}
			SuperSolveJob.Conclude(_003Ccancelled_003E5__9, _003Cfailure_003E5__10, _003Cbox_003E5__7.s, _003CtapeName_003E5__3, _003CtmpPath_003E5__4, _003CfinalPath_003E5__5, drive, "[Super] Расчёт не удался: ", SuperPreset.DurationS);
			_003Cbox_003E5__7 = null;
			_003Cit_003E5__8 = null;
			_003Cfailure_003E5__10 = null;
			_003C_003Em__Finally1();
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		private void _003C_003Em__Finally1()
		{
			_003C_003E1__state = -1;
			SuperSolveJob.EndMeter();
			if (_003Cdisplay_003E5__6.taken)
			{
				QualitySettings.vSyncCount = _003Cdisplay_003E5__6.vsync;
				Application.targetFrameRate = _003Cdisplay_003E5__6.targetFps;
				OnDemandRendering.renderFrameInterval = _003Cdisplay_003E5__6.renderInterval;
			}
			SuperEraDrive.ProbeEveryS = _003CsavedProbe_003E5__2;
			SuperSolveJob.Running = false;
			SuperSolveJob._progressSw = null;
			SuperHud.RefreshResumeProbe();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}
	}

	private static Stopwatch _progressSw;

	public static bool CancelRequested;

	private static Runner _runner;

	private static SuperRunRecord _lastRecord;

	public const string KeyHollowFired = "super.run.hollow_fired";

	public const string KeyHollowShort = "super.run.hollow_short";

	public const string KeyHollowMissed = "super.run.hollow_missed";

	public const string KeyHollowNoContact = "super.run.hollow_nocontact";

	private static SuperRunRecord _record;

	private static Stopwatch _wallSw;

	private static SuperRunMeter _meter;

	private static bool _stepLoopMarked;

	private static bool _stepLoopEnded;

	public static int PauseFloorGaps;

	public static double PauseFloorSumS;

	public static double PauseFloorWallS;

	private static long _logMark;

	public const string TapeName = "player_shot.tape";

	public const string TapePrefix = "player_";

	private const string TempSuffix = ".writing";

	private static SuperTapeRecorder.RecBox _pumpBox;

	private static int _progBuilt;

	private static long _progTicksAtBuilt;

	private static double _progTicksPerFrame;

	public static int RenderIntervalDuringSolve;

	public static int TargetFrameRateDuringSolve;

	public const int TargetFrames = 150;

	public static bool SabotageFinishWithoutTape;

	public static bool ExportAfterSolve;

	public static bool Running { get; private set; }

	public static float Progress { get; private set; }

	public static int Frames { get; private set; }

	public static int Expected { get; private set; }

	public static double ElapsedS => (_wallSw != null) ? _wallSw.Elapsed.TotalSeconds : 0.0;

	public static string LastTapePath { get; private set; }

	public static Outcome LastOutcome { get; private set; }

	public static SuperRunRecord LastRecord { get; private set; }

	public static string ViewerRequest { get; private set; }

	private static Runner Host()
	{
		if (_runner == null)
		{
			GameObject gameObject = new GameObject("SuperSolveJobRunner");
			gameObject.hideFlags = HideFlags.HideAndDontSave;
			UnityEngine.Object.DontDestroyOnLoad(gameObject);
			_runner = gameObject.AddComponent<Runner>();
		}
		return _runner;
	}

	public static void DismissOutcome()
	{
		LastOutcome = null;
	}

	private static void SetOutcome(RunEnd end, string tapePath, int frames, long bytes, string reason, bool resumed, string run, string reasonKey = null, object[] reasonArgs = null)
	{
		Outcome outcome = new Outcome();
		outcome.tapePath = "";
		outcome.reason = "";
		outcome.reasonKey = "";
		outcome.run = "";
		outcome.hollowKey = "";
		outcome.end = end;
		outcome.tapePath = tapePath ?? "";
		outcome.frames = frames;
		outcome.tapeBytes = bytes;
		outcome.reason = reason ?? "";
		outcome.reasonKey = reasonKey ?? "";
		outcome.reasonArgs = reasonArgs;
		outcome.resumed = resumed;
		outcome.record = (resumed ? null : _lastRecord);
		outcome.run = run ?? "";
		LastOutcome = outcome;
		LastRecord = null;
	}

	private static long FileLen(string path)
	{
		if (!string.IsNullOrEmpty(path) && File.Exists(path))
		{
			return new FileInfo(path).Length;
		}
		return 0L;
	}

	public static bool HollowLineNeedsAttention(string key)
	{
		return key == "super.run.hollow_short" || key == "super.run.hollow_missed" || key == "super.run.hollow_nocontact";
	}

	public static bool HollowRunLine(SuperEraDrive drive, float windowS, out string key, out object[] args)
	{
		key = "";
		args = null;
		SuperShellDrive shell = null;
		for (SuperEraDrive d = drive; d != null; d = d.next)
		{
			shell = (d as SuperShellDrive);
			if (shell != null)
			{
				break;
			}
		}
		if (shell == null || shell.SabotageZeroVelocity || shell.SabotageNeverFires || shell.SabotageNoRemainder || shell.SabotageOpenBase || shell.velocityMps <= 0f)
		{
			return false;
		}
		float delayS = Mathf.Max(0f, shell.delayMultiplier) * shell.initiationDelayS;
		if (!shell.Fired)
		{
			if (shell.ContactTimeS < 0f)
			{
				key = "super.run.hollow_nocontact";
				args = new object[2] { windowS * 1000000f, delayS * 1000000f };
				return true;
			}
			key = "super.run.hollow_missed";
			args = new object[3] { shell.ContactTimeS * 1000000f, delayS * 1000000f, windowS * 1000000f };
			return true;
		}
		key = ((shell.velocityMps - shell.velocityMps * 0.001f > shell.DeliveredMps) ? "super.run.hollow_short" : "super.run.hollow_fired");
		args = new object[5] { shell.ContactTimeS * 1000000f, delayS * 1000000f, shell.FireTimeS * 1000000f, shell.DeliveredMps, shell.velocityMps };
		return true;
	}

	private static void AttachHollowLine(SuperEraDrive drive, float windowS)
	{
		Outcome lastOutcome = LastOutcome;
		if (lastOutcome != null)
		{
			string key;
			object[] args;
			if (HollowRunLine(drive, windowS, out key, out args))
			{
				lastOutcome.hollowKey = key;
				lastOutcome.hollowArgs = args;
			}
		}
	}

	private static void MarkStepLoopFromPump()
	{
		SuperTapeRecorder.Summary s = ((_pumpBox != null) ? _pumpBox.s : null);
		if (_meter == null || s == null)
		{
			return;
		}
		if (!_stepLoopMarked)
		{
			if (s.stepNow > 0)
			{
				_meter.MarkStepLoopBegin();
				_stepLoopMarked = true;
			}
		}
		else if (!_stepLoopEnded && s.stepsTotal > 0 && s.stepNow >= s.stepsTotal)
		{
			_meter.MarkStepLoopEnd();
			_stepLoopEnded = true;
		}
	}

	private static SuperScenePlan SafePlan(Func<SuperScenePlan> build)
	{
		try
		{
			return build();
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[Super] План сцены не построен: " + ex.Message);
			return null;
		}
	}

	private static void RecordBegin(SuperScenePlan plan, string tapeName, float cutAzimuthDeg = 0f)
	{
		_record = null;
		_wallSw = null;
		_logMark = -1L;
		_lastRecord = null;
		LastRecord = null;
		if (plan == null)
		{
			return;
		}
		try
		{
			_logMark = SuperAllocAudit.NativeLogMark();
			SuperRunRecord rec = SuperRunRecord.NewOnThisMachine();
			rec.FillPredicted(plan);
			rec.tape = tapeName ?? "";
			rec.view.cutAzimuthDeg = cutAzimuthDeg;
			SuperRunLedger.Version game = SuperRunLedger.Version.Game;
			rec.pipelineVersion = game.pipelineVersion;
			rec.workers = game.workers;
			rec.gpuPath = game.gpuPath;
			rec.fieldsPath = game.fieldsPath ?? "";
			rec.xiScatter = (SuperDamage.FailureStrainScatterOn ? 1 : 0);
			rec.xiM = SuperDamage.FailureStrainScatterModulus;
			rec.xiSalt = (SuperDamage.FailureStrainScatterOn ? ((long)SuperDamage.FailureStrainScatterSalt) : 0);
			rec.s43Alpha = SuperDamage.S43Alpha;
			rec.s43Beta = SuperDamage.S43Beta;
			rec.s43VRefMm3 = SuperDamage.S43VRefMm3;
			rec.s43HRefMm = SuperDamage.S43HRefMm;
			double medianSecondsPerParticleFrame;
			double minK;
			double maxK;
			int n;
			if (SuperRunLedger.TapeCoefficient(rec.device, out medianSecondsPerParticleFrame, out minK, out maxK, out n))
			{
				int expectedFrames = Mathf.Max(1, (int)(plan.durationS / Mathf.Max(2e-07f, plan.durationS / 150f)));
				rec.SetTapePrediction(medianSecondsPerParticleFrame, minK, maxK, plan.particles, expectedFrames);
			}
			_record = rec;
			_wallSw = Stopwatch.StartNew();
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[Super] Запись прогона не открыта: " + ex.Message);
		}
	}

	private static void RecordEnd(SuperTapeRecorder.Summary s, string tapePath, bool completed, string failure)
	{
	}

	public static void ForgetTape(string path)
	{
		try
		{
			if (!string.IsNullOrEmpty(LastTapePath) && !string.IsNullOrEmpty(path) && string.Equals(Path.GetFullPath(LastTapePath), Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase))
			{
				LastTapePath = null;
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[Super] ForgetTape: путь не разобран («" + path + "»): " + ex.Message);
		}
	}

	public static string NewTapeName()
	{
		return "player_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".tape";
	}

	private static string RunOf(string tapeName)
	{
		return Path.GetFileNameWithoutExtension(tapeName);
	}

	private static void CancelPump()
	{
		UpdateLiveProgress();
		if (CancelRequested)
		{
			throw new Cancelled();
		}
	}

	private static void UpdateLiveProgress()
	{
		if (_pumpBox == null)
		{
			return;
		}
		SuperTapeRecorder.Summary s = _pumpBox.s;
		if (s == null || Expected <= 0 || s.stepsTotal <= 0)
		{
			return;
		}
		float num = 0.5f;
		if (_record != null && _record.predicted.solveS > 0.0 && _record.predicted.tapeS > 0.0)
		{
			num = Mathf.Clamp((float)(_record.predicted.solveS / (_record.predicted.solveS + _record.predicted.tapeS)), 0.1f, 0.9f);
		}
		float num2 = Mathf.Clamp01((float)s.stepNow / (float)s.stepsTotal);
		long workTicks = s.stages.WorkTicks;
		int framesBuilt = s.framesBuilt;
		if (framesBuilt != _progBuilt)
		{
			int num3 = framesBuilt - s.baseFrames;
			if (num3 > 0 && workTicks > 0)
			{
				_progTicksPerFrame = Math.Max(1.0, (double)workTicks / num3);
				_progBuilt = framesBuilt;
				_progTicksAtBuilt = workTicks;
			}
		}
		float num4 = Mathf.Clamp01((float)(((double)framesBuilt + 1.0 - Math.Abs((double)(_progTicksAtBuilt - workTicks) / ((double)Math.Max(1, s.inFlightK) * _progTicksPerFrame))) / (double)Expected));
		float value = (1f - num) * num4 + num * num2;
		if (value > Progress)
		{
			Progress = value;
		}
	}

	private static void EndMeter()
	{
		SuperRunMeter meter = _meter;
		_meter = null;
		if (meter != null)
		{
			meter.End();
		}
	}

	private static DisplayState EnterSolveDisplay()
	{
		DisplayState d = default(DisplayState);
		d.vsync = QualitySettings.vSyncCount;
		d.targetFps = Application.targetFrameRate;
		d.renderInterval = OnDemandRendering.renderFrameInterval;
		QualitySettings.vSyncCount = 0;
		Application.targetFrameRate = TargetFrameRateDuringSolve;
		OnDemandRendering.renderFrameInterval = RenderIntervalDuringSolve;
		d.taken = true;
		return d;
	}

	private static void LeaveSolveDisplay(DisplayState d)
	{
		if (d.taken)
		{
			QualitySettings.vSyncCount = d.vsync;
			Application.targetFrameRate = d.targetFps;
			OnDemandRendering.renderFrameInterval = d.renderInterval;
		}
	}

	public static void BeginWith(ComputeShader cs, SuperJetSpec jet, IList<SuperPlateSpec> plates, SuperEraDrive drive = null, float cutAzimuthDeg = 0f, byte[] decor = null)
	{
		Host().StartCoroutine(JetRoutine(cs, jet, plates, drive, cutAzimuthDeg, decor));
	}

	public static void ResumeWith(ComputeShader cs, SuperCheckpoint.Data d)
	{
		Host().StartCoroutine(ResumeRoutine(cs, d));
	}

	public static ComputeShader FindShader()
	{
		return Resources.Load<ComputeShader>("SuperSolver");
	}

	public static float JetWindowS(in SuperJetSpec jet, IList<SuperPlateSpec> plates)
	{
		ContinuumProperties jetMat = ContinuumMaterials.Get(SuperBaker.Material(jet.material));
		float jetRho = jetMat.rho0;
		float plateRho = jetRho;
		float entryBest = float.NegativeInfinity;
		int bestIndex = 0;
		if (plates != null && plates.Count > 0)
		{
			for (int i = 0; i < plates.Count; i++)
			{
				SuperPlateSpec p = plates[i];
				if (SuperScenePose.OnLine(in p, jet.diameterM * 0.5f, out float entryZ) && entryZ > entryBest)
				{
					entryBest = entryZ;
					bestIndex = i;
				}
			}
			SuperPlateSpec best = plates[bestIndex];
			plateRho = ContinuumMaterials.Get(SuperBaker.Material(best.material)).rho0;
		}
		float ratio = ((1E-06f > plateRho / jetRho) ? 1E-06f : (plateRho / jetRho));
		float approach = Mathf.Max(1f, jet.vTipMps - jet.vTipMps / (Mathf.Sqrt(ratio) + 1f));
		float leadS = jet.lengthM / approach;
		float farDepth = 0f;
		if (plates != null)
		{
			foreach (SuperPlateSpec p2 in plates)
			{
				farDepth = Mathf.Max(farDepth, SuperBaker.FarDepthM(in p2));
			}
		}
		float window = Mathf.Abs(jet.tipZM) + farDepth;
		window /= Mathf.Max(1f, jet.vTipMps);
		window += leadS;
		window *= 1.25f;
		if (!(5E-06f > window))
		{
			return window;
		}
		return 5E-06f;
	}

	public static void Begin(SuperJetSpec jet, IList<SuperPlateSpec> plates, SuperEraDrive drive = null, float cutAzimuthDeg = 0f, byte[] decor = null)
	{
		ComputeShader cs = FindShader();
		if (cs != null)
		{
			Host().StartCoroutine(JetRoutine(cs, jet, plates, drive, cutAzimuthDeg, decor));
		}
		else
		{
			Debug.LogError("[Super] Не найден SuperSolver.compute — расчёт струи невозможен.");
		}
	}

	[IteratorStateMachine(typeof(_003CJetRoutine_003Ed__86))]
	private static IEnumerator JetRoutine(ComputeShader cs, SuperJetSpec jet, IList<SuperPlateSpec> plates, SuperEraDrive drive, float cutAzimuthDeg = 0f, byte[] decor = null)
	{
		_003CJetRoutine_003Ed__86 stateMachine = new _003CJetRoutine_003Ed__86(0);
		stateMachine.cs = cs;
		stateMachine.jet = jet;
		stateMachine.plates = plates;
		stateMachine.drive = drive;
		stateMachine.cutAzimuthDeg = cutAzimuthDeg;
		stateMachine.decor = decor;
		return stateMachine;
	}

	private static void Conclude(bool cancelled, Exception failure, SuperTapeRecorder.Summary s, string tapeName, string tmpPath, string finalPath, SuperEraDrive drive, string failPrefix, float windowS)
	{
		string run = Path.GetFileNameWithoutExtension(tapeName);
		if (cancelled)
		{
			RecordEnd(null, null, completed: false, "отменён игроком");
			Discard(tmpPath);
			SuperCheckpoint.DiscardAll(run);
			SuperCheckpoint.ActiveRun = null;
			Debug.Log("[Super] Расчёт отменён игроком — сцена не тронута.");
			SetOutcome(RunEnd.Cancelled, "", 0, 0L, "", resumed: false, run, null, null);
			return;
		}
		if (failure != null)
		{
			SuperCheckpoint.ActiveRun = null;
			string message = failure.Message;
			RecordEnd(null, tmpPath, completed: false, message);
			Debug.LogError(string.Concat("[Super] Отказ прогона (", failure.GetType().FullName, "): ", failure.Message, "\n", failure.StackTrace, (failure.InnerException != null) ? string.Concat("\n--- внутреннее (", failure.InnerException.GetType().FullName, "): ", failure.InnerException.Message, "\n", failure.InnerException.StackTrace) : ""));
			Fail(failPrefix + message + KeptNote(tmpPath));
			SetOutcome(RunEnd.Failed, File.Exists(tmpPath) ? tmpPath : "", Frames, FileLen(tmpPath), failure.Message, resumed: false, run, null, null);
			return;
		}
		if (s == null || (!File.Exists(tmpPath) && !SabotageFinishWithoutTape))
		{
			string failure2 = ((s != null && !string.IsNullOrEmpty(s.failure)) ? s.failure : "лента не создана");
			string reasonKey;
			object[] reasonArgs;
			if (s == null)
			{
				reasonKey = "super.fail.no_tape";
				reasonArgs = null;
			}
			else
			{
				reasonKey = string.IsNullOrEmpty(s.failureKey) ? "" : s.failureKey;
				reasonArgs = s.failureArgs;
			}
			SuperCheckpoint.ActiveRun = null;
			SuperCheckpoint.DiscardAll(run);
			RecordEnd(s, null, completed: false, failure2);
			Debug.LogWarning("[Super] Расчёт не начат — решатель отказал до первого кадра: " + failure2);
			SetOutcome(RunEnd.Refused, "", 0, 0L, failure2, resumed: false, run, reasonKey, reasonArgs);
			if (ResultDisplay.Instance != null)
			{
				ResultDisplay.Instance.Show(SuperHud.RefusedLine(LastOutcome), MenuTheme.Refused);
			}
			return;
		}
		if (drive != null)
		{
			Debug.Log("[Super] Привод ДЗ: " + drive.Passport());
		}
		if (!s.ok)
		{
			Debug.LogWarning("[Super] Расчёт помечен недействительным: " + s.failure + " — лента записана, но числа прогона недействительны.");
		}
		Finish(s, tmpPath, finalPath);
		string reason;
		if (s.ok)
		{
			reason = ((_lastRecord != null) ? (_lastRecord.invalidReason ?? "") : "");
		}
		else
		{
			reason = s.failure;
		}
		SetOutcome(string.IsNullOrEmpty(reason) ? RunEnd.Done : RunEnd.Invalid, finalPath, s.frames, s.fileBytes, reason, resumed: false, run, s.ok ? "" : (s.failureKey ?? ""), s.ok ? null : s.failureArgs);
		AttachHollowLine(drive, windowS);
	}

	private static void Finish(SuperTapeRecorder.Summary s, string tmpPath, string finalPath)
	{
		if (File.Exists(finalPath))
		{
			File.Delete(finalPath);
		}
		File.Move(tmpPath, finalPath);
		LastTapePath = finalPath;
		Progress = 1f;
		string fileName = Path.GetFileName(finalPath);
		string run = Path.GetFileNameWithoutExtension(fileName);
		SuperCheckpoint.DiscardAll(run);
		SuperCheckpoint.ActiveRun = null;
		if (s != null)
		{
			Debug.Log(string.Format("[Super] Готово: кадров {0}, {1:F1} МБ, ", s.frames, (double)s.fileBytes * 9.5367431640625E-07) + string.Format("{0:F0} с, вне сетки {1:P3}.", s.wallS, s.shot.escapedFraction));
			RecordEnd(s, finalPath, completed: true, null);
		}
	}

	private static string KeptNote(string tmpPath)
	{
		if (File.Exists(tmpPath))
		{
			return " Записанные кадры сохранены (" + Path.GetFileName(tmpPath) + ", в библиотеке — как оборванная лента).";
		}
		return "";
	}

	public static void Resume(SuperCheckpoint.Probe probe)
	{
		try
		{
			if (!Running && probe != null && probe.usable)
			{
				ComputeShader computeShader = FindShader();
				if (computeShader == null)
				{
					Fail("[Super] Не найден вычислительный шейдер SuperSolver.");
				}
				else
				{
					SuperCheckpoint.Data data = SuperCheckpoint.Read(probe.path);
					Host().StartCoroutine(ResumeRoutine(computeShader, data));
				}
			}
		}
		catch (Exception ex)
		{
			Fail("[Super] Контрольная точка не прочиталась: " + ex.Message);
		}
		SuperHud.RefreshResumeProbe();
	}

	internal static string ResumeRefusal(SuperCheckpoint.Data d)
	{
		if (d == null)
		{
			throw new ArgumentNullException("d");
		}
		if (d.damageEnabled != SuperDamage.Enabled || d.coherentFlyers != SuperEra.CoherentFlyerPlates || d.softening != SuperDamage.SofteningCoefficient || d.coherentFloorPa != SuperDamage.CoherentTensionFloorPa || d.capN != SuperDamage.PressureCapFactorN)
		{
			return "[Super] Конфигурация решателя изменилась с момента точки — продолжение было бы другим прогоном под тем же именем. Отброшено.";
		}
		string text = XiStateMismatch(d);
		if (text != null)
		{
			return "[Super] " + text;
		}
		return null;
	}

	internal static string XiStateMismatch(SuperCheckpoint.Data d)
	{
		if (d.xiScatterOn != SuperDamage.FailureStrainScatterOn)
		{
			return Localization.T("super.s43.refuse.ckpt_xi", new object[3] { "xi", d.xiScatterOn ? 1 : 0, SuperDamage.FailureStrainScatterOn ? 1 : 0 });
		}
		if (d.xiScatterOn || SuperDamage.FailureStrainScatterOn)
		{
			if (d.xiModulus != SuperDamage.FailureStrainScatterModulus)
			{
				return XiCkptText("m", d.xiModulus, SuperDamage.FailureStrainScatterModulus);
			}
			if (d.xiSalt != SuperDamage.FailureStrainScatterSalt)
			{
				return Localization.T("super.s43.refuse.ckpt_xi", new object[3]
				{
					"salt",
					"0x" + d.xiSalt.ToString("X8", CultureInfo.InvariantCulture),
					"0x" + SuperDamage.FailureStrainScatterSalt.ToString("X8", CultureInfo.InvariantCulture)
				});
			}
		}
		if (d.s43Alpha != SuperDamage.S43Alpha)
		{
			return XiCkptText("alpha", d.s43Alpha, SuperDamage.S43Alpha);
		}
		if (d.s43Beta != SuperDamage.S43Beta)
		{
			return XiCkptText("beta", d.s43Beta, SuperDamage.S43Beta);
		}
		if (d.s43Alpha != 0.0 || SuperDamage.S43Alpha != 0.0)
		{
			if (d.s43VRefMm3 != SuperDamage.S43VRefMm3)
			{
				return XiCkptText("vref", d.s43VRefMm3, SuperDamage.S43VRefMm3);
			}
		}
		if (d.s43Beta != 0.0 || SuperDamage.S43Beta != 0.0)
		{
			if (d.s43HRefMm != SuperDamage.S43HRefMm)
			{
				return XiCkptText("href", d.s43HRefMm, SuperDamage.S43HRefMm);
			}
		}
		if (d.s43Alpha != 0.0 || d.s43Beta != 0.0 || SuperDamage.S43ScalingOn)
		{
			if (d.xiCeiling != 10f)
			{
				return XiCkptText("ceiling", d.xiCeiling, 10.0);
			}
		}
		return null;
	}

	private static string XiCkptText(string field, double inPoint, double now)
	{
		string text = inPoint.ToString("G6", CultureInfo.InvariantCulture);
		string text2 = now.ToString("G6", CultureInfo.InvariantCulture);
		if (text != text2)
		{
			text = inPoint.ToString("R", CultureInfo.InvariantCulture);
			text2 = now.ToString("R", CultureInfo.InvariantCulture);
		}
		return Localization.T("super.s43.refuse.ckpt_xi", new object[3] { field, text, text2 });
	}

	[IteratorStateMachine(typeof(_003CResumeRoutine_003Ed__95))]
	private static IEnumerator ResumeRoutine(ComputeShader cs, SuperCheckpoint.Data d)
	{
		_003CResumeRoutine_003Ed__95 stateMachine = new _003CResumeRoutine_003Ed__95(0);
		stateMachine.cs = cs;
		stateMachine.d = d;
		return stateMachine;
	}

	public static void Begin(SuperRodSpec rod, IList<SuperPlateSpec> plates, SuperEraDrive drive = null, float cutAzimuthDeg = 0f, byte[] decor = null)
	{
		ComputeShader cs = FindShader();
		if (cs != null)
		{
			Host().StartCoroutine(RodRoutine(cs, rod, plates, drive, cutAzimuthDeg, decor));
		}
		else
		{
			Fail("[Super] Не найден вычислительный шейдер SuperSolver. В сборке он должен лежать в папке Resources; в редакторе берётся из Assets/Scripts/Super/.");
		}
	}

	public static void BeginWith(ComputeShader cs, SuperRodSpec rod, IList<SuperPlateSpec> plates, SuperEraDrive drive = null, float cutAzimuthDeg = 0f, byte[] decor = null)
	{
		Host().StartCoroutine(RodRoutine(cs, rod, plates, drive, cutAzimuthDeg, decor));
	}

	[IteratorStateMachine(typeof(_003CRodRoutine_003Ed__98))]
	private static IEnumerator RodRoutine(ComputeShader cs, SuperRodSpec rod, IList<SuperPlateSpec> plates, SuperEraDrive drive, float cutAzimuthDeg = 0f, byte[] decor = null)
	{
		_003CRodRoutine_003Ed__98 stateMachine = new _003CRodRoutine_003Ed__98(0);
		stateMachine.cs = cs;
		stateMachine.rod = rod;
		stateMachine.plates = plates;
		stateMachine.drive = drive;
		stateMachine.cutAzimuthDeg = cutAzimuthDeg;
		stateMachine.decor = decor;
		return stateMachine;
	}

	public static void OpenViewer()
	{
		if (!AppFlow.SceneInBuild("SuperViewer"))
		{
			Fail("[Super] Сцена показа «SuperViewer» не в списке сцен сборки. Собери её: меню ArmorSim → Build SUPER Viewer Scene.");
			return;
		}
		ViewerRequest = ((LastTapePath != null) ? LastTapePath : "");
		SceneManager.LoadScene("SuperViewer", LoadSceneMode.Single);
	}

	public static void OpenViewer(string tapePath)
	{
		if (!AppFlow.SceneInBuild("SuperViewer"))
		{
			Fail("[Super] Сцена показа «SuperViewer» не в списке сцен сборки. Собери её: меню ArmorSim → Build SUPER Viewer Scene.");
			return;
		}
		if (tapePath == null)
		{
			tapePath = ((LastTapePath != null) ? LastTapePath : "");
		}
		ViewerRequest = tapePath;
		SceneManager.LoadScene("SuperViewer", LoadSceneMode.Single);
	}

	public static string TakeViewerRequest()
	{
		string request = ViewerRequest;
		ViewerRequest = null;
		return request;
	}

	private static void Discard(string tmpPath)
	{
		if (File.Exists(tmpPath))
		{
			File.Delete(tmpPath);
		}
	}

	private static void Fail(string text)
	{
		Debug.LogError(text);
		if (ResultDisplay.Instance != null)
		{
			ResultDisplay.Instance.Show(text, MenuTheme.Failure);
		}
	}

	static SuperSolveJob()
	{
		PauseFloorGaps = -1;
		PauseFloorSumS = -2.0;
		PauseFloorWallS = -2.0;
		_progTicksAtBuilt = -1L;
		_progTicksPerFrame = 200.0;
		RenderIntervalDuringSolve = 1;
		TargetFrameRateDuringSolve = 60;
		SabotageFinishWithoutTape = false;
		ExportAfterSolve = true;
	}
}
