using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SuperHud : MonoBehaviour
{
	public enum OutcomeLineKind
	{
		Head = 0,
		Failure = 1,
		Text = 2,
		Dim = 3,
		Refused = 4
	}

	private sealed class Salvage
	{
		public string path;

		public long len;

		public bool done;

		public int frames;

		public float lastUs;

		public string error;
	}

	private static SuperHud _instance;

	private static bool _inSandbox;

	private static Texture2D _bgTex;

	private static Texture2D _lineTex;

	private static GUIStyle _panel;

	private static GUIStyle _head;

	private static GUIStyle _text;

	private static GUIStyle _dim;

	private static GUIStyle _button;

	private static GUIStyle _refused;

	private static SuperShotRunner.SceneCost _cost;

	private static int _costFrame;

	private static bool _costValid;

	private static float _costAtT;

	private static float _costTookS;

	public const float CostSlowS = 0.012f;

	public const float CostSlowEveryS = 0.25f;

	private static bool _costDirtyRelease;

	private static bool _heightWarned;

	private static string _durationText;

	private static float _clampedWant;

	private static float _clampedTo;

	private static SuperCheckpoint.Probe _probe;

	private static bool _probed;

	private static bool _tapeProbed;

	private static bool _tapeKnown;

	private static double _tapeMed;

	private static double _tapeLo;

	private static double _tapeHi;

	private static int _tapeRows;

	private static bool _inMenu;

	private static bool _progressCollapsed;

	private static GUIStyle _failure;

	private static bool _deleteArmed;

	private static string _screenNote;

	private static Salvage _salvage;

	private static int _salvageAskFrame;

	private static SuperCheckpoint.Probe _runProbe;

	private static string _runProbeRun;

	private static bool _runProbeDone;

	private static int _runProbeAskFrame;

	private static string _orphanPath;

	private static bool _orphanScanned;

	private static bool _orphanHidden;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Install()
	{
		if (_instance != null)
		{
			return;
		}
		GameObject gameObject = new GameObject("SuperHud");
		gameObject.hideFlags = (HideFlags)61;
		UnityEngine.Object.DontDestroyOnLoad(gameObject);
		_instance = gameObject.AddComponent<SuperHud>();
		SceneManager.activeSceneChanged += delegate
		{
			Recheck();
		};
		SceneManager.sceneLoaded += delegate(Scene scene, LoadSceneMode mode)
		{
			Recheck();
		};
		SceneManager.sceneUnloaded += delegate(Scene scene)
		{
			Recheck();
		};
		Recheck();
	}

	private static void Recheck()
	{
		string name = SceneManager.GetActiveScene().name;
		_inSandbox = !string.IsNullOrEmpty(AppFlow.SandboxSceneName) && name == AppFlow.SandboxSceneName;
		_inMenu = name == AppFlow.MenuSceneName;
	}

	private static Texture2D Solid(Color c)
	{
		Texture2D texture2D = new Texture2D(1, 1);
		texture2D.hideFlags = (HideFlags)61;
		texture2D.SetPixel(0, 0, c);
		texture2D.Apply();
		return texture2D;
	}

	private static void EnsureStyles()
	{
		if (_panel != null)
		{
			return;
		}
		_bgTex = Solid(new Color(MenuTheme.Surface.r, MenuTheme.Surface.g, MenuTheme.Surface.b, 0.94f));
		_lineTex = Solid(MenuTheme.Accent);
		_panel = new GUIStyle(GUI.skin.box);
		_panel.padding = new RectOffset(12, 12, 10, 10);
		_panel.normal.background = _bgTex;
		_head = new GUIStyle(GUI.skin.label);
		_head.fontStyle = FontStyle.Bold;
		_head.fontSize = 13;
		_head.normal.textColor = MenuTheme.TextPrimary;
		_text = new GUIStyle(GUI.skin.label);
		_text.fontSize = 12;
		_text.normal.textColor = MenuTheme.TextPrimary;
		_dim = new GUIStyle(GUI.skin.label);
		_dim.fontSize = 11;
		_dim.wordWrap = true;
		_dim.normal.textColor = MenuTheme.TextMuted;
		_refused = new GUIStyle(_dim);
		_refused.normal.textColor = MenuTheme.Refused;
		_button = new GUIStyle(GUI.skin.button);
		_button.fontSize = 12;
		_button.normal.background = Solid(MenuTheme.Pressed);
		_button.hover.background = Solid(MenuTheme.Hover);
		_button.active.background = Solid(MenuTheme.Accent);
		_button.focused.background = _button.normal.background;
		Color textPrimary = MenuTheme.TextPrimary;
		_button.focused.textColor = textPrimary;
		_button.active.textColor = textPrimary;
		_button.hover.textColor = textPrimary;
		_button.normal.textColor = textPrimary;
	}

	private void OnGUI()
	{
		bool flag = _inSandbox && AppFlow.ActiveMode == SimMode.Super;
		if (!flag && !_inMenu)
		{
			return;
		}
		EnsureStyles();
		if (!SuperViewerUI.IsOpen)
		{
			if (!SuperSolveJob.Running)
			{
				SuperSolveJob.Outcome outcome = SuperSolveJob.LastOutcome;
				if (outcome != null)
				{
					DrawOutcome(outcome);
				}
				else if (flag)
				{
					DrawSettings();
				}
				else if (!MainMenuUI.OverlayOpen)
				{
					DrawInterruptedOffer(standalone: true);
				}
			}
			else if (flag && !_progressCollapsed)
			{
				DrawProgress();
			}
			else
			{
				DrawChip(flag);
			}
		}
		else if (SuperSolveJob.Running)
		{
			DrawChip(false);
		}
	}

	private void DrawProgress()
	{
		GUILayout.BeginArea(new Rect((float)(Screen.width - 560) * 0.5f, (float)Screen.height * 0.5f - 212f, 560f, 212f), _panel);
		GUILayout.Label(Localization.T("super.progress.title"), _head);
		float num = Mathf.Clamp(SuperSolveJob.Progress, 0f, 1f);
		Rect rect = GUILayoutUtility.GetRect(536f, 18f);
		GUI.HorizontalScrollbar(rect, 0f, Mathf.Max(0.02f, num), 0f, 1f);
		GUILayout.Label(string.Format("{0:F0} %   ·   ", num * 100f) + Localization.T("super.progress.frames", new object[1] { SuperSolveJob.Frames }) + "   ·   " + Localization.T("super.progress.elapsed", new object[1] { Mins(SuperSolveJob.ElapsedS) }), _text);
		if (SuperSolveJob.Frames >= 2 && SuperSolveJob.Expected > SuperSolveJob.Frames && SuperSolveJob.ElapsedS > 0.0)
		{
			GUILayout.Label(Localization.T("super.progress.left", new object[1] { Mins(SuperSolveJob.ElapsedS / (double)SuperSolveJob.Frames * (double)(SuperSolveJob.Expected - SuperSolveJob.Frames)) }), _text);
		}
		else
		{
			GUILayout.Label(Localization.T("super.progress.left_unknown"), _dim);
		}
		GUILayout.Label(Localization.T("super.run.leave_hint"), _dim);
		GUILayout.BeginHorizontal();
		if (GUILayout.Button(Localization.T("super.run.collapse"), _button))
		{
			_progressCollapsed = true;
		}
		if (SuperSolveJob.CancelRequested)
		{
			GUILayout.Label(Localization.T("super.progress.cancelling"), _dim);
		}
		else if (GUILayout.Button(Localization.T("super.progress.cancel"), _button))
		{
			SuperSolveJob.CancelRequested = true;
		}
		GUILayout.EndHorizontal();
		GUILayout.EndArea();
	}

	private void DrawSettings()
	{
		EnsureStyles();
		float num = _cost.isHollow ? 760f : 620f;
		float num2 = Mathf.Min((float)Screen.height - 20f, num);
		if (num > num2 && !_heightWarned)
		{
			_heightWarned = true;
			Debug.LogWarning(string.Format("[Super] Экран {0} px ниже панели параметров ({1:F0}): нижние строки панели обрезаны.", Screen.height, num));
		}
		float num3 = Mathf.Max(10f, (float)(Screen.width - 400 - 300));
		GUI.DrawTexture(new Rect(num3, 10f, 3f, num2), _lineTex);
		GUILayout.BeginArea(new Rect(num3 + 3f, 10f, 397f, num2), _panel);
		GUILayout.Label(Localization.T("super.hud.title"), _head);
		GUILayout.Label(Localization.T("super.hud.duration", new object[2] { SuperPreset.DurationS * 1000000f, 10000f }), _text);
		SuperPreset.DurationS = GUILayout.HorizontalSlider(SuperPreset.DurationS, 5e-05f, 0.01f);
		GUILayout.BeginHorizontal();
		GUILayout.Label(Localization.T("super.hud.duration_exact"), _dim, GUILayout.Width(170f));
		GUI.SetNextControlName("super.duration.exact");
		bool flag = GUI.GetNameOfFocusedControl() == "super.duration.exact";
		if (flag && _durationText == null)
		{
			_durationText = (SuperPreset.DurationS * 1000000f).ToString("F0", CultureInfo.InvariantCulture);
		}
		_durationText = GUILayout.TextField(_durationText, GUILayout.Width(90f));
		if (flag && float.TryParse(_durationText, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) && result > 0f)
		{
			SuperPreset.DurationS = Mathf.Clamp(result * 1e-06f, 5e-05f, 0.01f);
		}
		GUILayout.EndHorizontal();
		GUILayout.Label(Localization.T("super.hud.duration_ceiling"), _dim);
		GUILayout.Label(Localization.T("super.hud.spacing", new object[1] { SuperPreset.SpacingM * 1000f }), _text);
		SuperPreset.SpacingM = GUILayout.HorizontalSlider(SuperPreset.SpacingM, 0.0002f, 0.008f);
		Projectile proj = UnityEngine.Object.FindFirstObjectByType<Projectile>();
		if (Time.frameCount != _costFrame && CostDue())
		{
			CostTimed(proj);
			_costFrame = Time.frameCount;
		}
		float value2 = (_cost.isJet ? SuperShotRunner.JetSpacingCeiling(_cost.jetDiameterM) : 0f);
		float num4 = _cost.hollowTriggerRowS;
		if (num4 != 0f || _cost.isHollow)
		{
			float value = SuperPreset.TriggerTimeFor(num4);
			string key = SuperPreset.TriggerTimeSet ? "super.hud.trigger_time_knob" : "super.hud.trigger_time_row";
			GUILayout.Label(Localization.T(key, new object[2] { value * 1000000f, num4 * 1000000f }), _text);
			value = GUILayout.HorizontalSlider(value, 0f, 0.01f);
			SuperPreset.TriggerTimeS = value;
			GUILayout.BeginHorizontal();
			GUILayout.Label(Localization.T("super.hud.trigger_time_hint", new object[2] { _cost.hollowCavity, _cost.hollowWallVMps }), _dim);
			if (SuperPreset.TriggerTimeSet && GUILayout.Button(Localization.T("super.hud.trigger_time_reset"), _button, GUILayout.Width(130f)))
			{
				SuperPreset.TriggerTimeFromRow();
			}
			GUILayout.EndHorizontal();
		}
		string text = Localization.T("super.hud.burst_none");
		if (_cost.hollowBurst.present)
		{
			if (_cost.hollowBurst.beforeFaceM > 0f)
			{
				text = Localization.T("super.hud.burst_before", new object[1] { _cost.hollowBurst.beforeFaceM * 1000f });
			}
			else if (_cost.hollowBurst.behindRearM > 0f)
			{
				text = Localization.T("super.hud.burst_behind", new object[1] { _cost.hollowBurst.behindRearM * 1000f });
			}
			else
			{
				text = Localization.T("super.hud.burst_inside", new object[1] { -_cost.hollowBurst.beforeFaceM * 1000f });
			}
		}
		GUILayout.Label(Localization.T("super.hud.burst_place", new object[3] { text, _cost.hollowBurst.targetShown, _cost.hollowBurst.plateThickM * 1000f }), _text);
		GUILayout.Label(Localization.T(_cost.hollowBurst.aheadBodies ? "super.hud.burst_estimate_ahead" : "super.hud.burst_estimate"), _dim);
		float num5 = ((_cost.windowS > 0f) ? _cost.windowS : SuperPreset.DurationS);
		string text2 = ((SuperPreset.DurationS - 1e-09f > num5) ? Localization.T("super.hud.jet_window", new object[1] { num5 * 1000000f }) : "");
		GUILayout.Label(Localization.T("super.hud.bodies", new object[2] { _cost.bodies, _cost.particles / 1000000.0 }), _text);
		SuperScenePlan plan = _cost.plan;
		if (plan == null)
		{
			GUILayout.Label(Localization.T("super.hud.time_no_scene"), _dim);
		}
		else
		{
			SuperScenePlan.SolveBand(plan.solveSecondsPredicted, out var lowS, out var highS);
			GUILayout.Label(Localization.T("super.hud.grid", new object[5] { plan.nx, plan.ny, plan.nz, (double)plan.particles / 1000000.0, plan.steps }), _text);
			ProbeTapeCoefficient();
			if (!_tapeKnown)
			{
				text2 += Localization.T("super.hud.solve_only", new object[2] { Mins(lowS), Mins(highS) });
				GUILayout.Label(text2, _text);
				GUILayout.Label(Localization.T("super.hud.no_total"), _dim);
			}
			else
			{
				long num6 = Math.Max(1L, (long)Math.Ceiling((double)plan.steps / Math.Max(2e-07, (double)plan.steps / 150.0)));
				GUILayout.Label(text2 + Localization.T("super.hud.total", new object[4]
				{
					Mins(lowS + (double)plan.steps * (double)num6 * _tapeLo),
					Mins((double)plan.steps * (double)num6 * _tapeHi + highS),
					Mins(lowS),
					Mins(highS)
				}), _text);
				GUILayout.Label(Localization.T("super.hud.total_source", new object[2] { _tapeRows, _tapeMed * 1000000000.0 }), _dim);
			}
			if (plan.vramKnown)
			{
				GUILayout.Label(Localization.T("super.hud.vram", new object[3]
				{
					(double)plan.gpuBytesTotal / 1048576.0,
					plan.vramMB,
					(double)plan.gpuBytesPeakInferred / 1048576.0
				}), _dim);
			}
			else
			{
				GUILayout.Label(Localization.T("super.hud.vram_unknown", new object[1] { (double)plan.gpuBytesTotal / 1048576.0 }), _dim);
			}
		}
		GUILayout.Label(Localization.T("super.hud.cost_knobs"), _dim);
		if (_cost.ok)
		{
			if (_cost.bodies > 0 && SuperPreset.CellsAcross(_cost.thinnestM, SuperPreset.SpacingM) < 4f)
			{
				GUILayout.Label(Localization.T("super.hud.thin", new object[2] { _cost.thinnestM * 1000f, SuperPreset.CellsAcross(_cost.thinnestM, SuperPreset.SpacingM) }), _refused);
			}
		}
		else
		{
			GUILayout.Label(_cost.reasonText, _refused);
		}
		if (_cost.isJet)
		{
			float num8 = Mathf.Max(value2, 0.0002f);
			GUILayout.Label(Localization.T("super.hud.jet_step", new object[2] { num8 * 1000f, value2 * 1000f }), _text);
			GUILayout.Label(Localization.T("super.hud.jet_floor", new object[3] { num8 * 1000f, value2 * 1000f, 0.2f }), _dim);
		}
		GUILayout.BeginHorizontal();
		if (GUILayout.Button(Localization.T("super.hud.fast"), _button))
		{
			SuperPreset.ApplyFast();
		}
		if (GUILayout.Button(Localization.T("super.hud.fine"), _button))
		{
			SuperPreset.ApplyFine();
		}
		if (_cost.plan != null && GUILayout.Button(Localization.T("super.hud.step_budget"), _button))
		{
			SuperShotRunner.ReadScene(proj, out var rod, out var jet, out var isJet, out var plates, out var drive, out var reasonKey, out var reasonArgs);
			double rodVolumeM3 = SuperCavity.BudgetVolume(rod, SuperPreset.SpacingM);
			float num9 = (float)SuperShotRunner.SpacingForCeiling(rodVolumeM3, plates, SuperPreset.ParticleBudget);
			_clampedWant = SuperPreset.SpacingM;
			_clampedTo = num9;
			SuperPreset.SpacingM = num9;
		}
		if (_cost.isJet && GUILayout.Button(Localization.T("super.hud.step_jet"), _button))
		{
			SuperPreset.SpacingM = value2;
		}
		GUILayout.EndHorizontal();
		if (_clampedTo > 0f)
		{
			GUILayout.Label(Localization.T("super.hud.hollow_clamped", new object[3] { _clampedWant * 1000f, _clampedTo * 1000f, 4 }), _dim);
		}
		GUILayout.BeginHorizontal();
		if (SuperSolveJob.ViewerRequest == null && GUILayout.Button(Localization.T("super.hud.show"), _button))
		{
			SuperSolveJob.OpenViewer();
		}
		if (GUILayout.Button(Localization.T("super.hud.library"), _button))
		{
			SuperSolveJob.OpenViewer("");
		}
		GUILayout.EndHorizontal();
		DrawInterruptedOffer(standalone: false);
		if (!string.IsNullOrEmpty(_cost.passport))
		{
			GUILayout.Label(_cost.passport, _dim);
		}
		GUILayout.EndArea();
	}

	private static bool CostDue()
	{
		if (!_costValid)
		{
			return true;
		}
		if (Input.GetMouseButton(0) || TouchInputHelper.TouchActive)
		{
			return false;
		}
		if (_costTookS <= CostSlowS || Time.unscaledTime - _costAtT >= CostSlowEveryS)
		{
			return true;
		}
		return _costDirtyRelease;
	}

	private static void CostTimed(Projectile proj)
	{
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		_cost = SuperShotRunner.Cost(proj);
		_costTookS = Time.realtimeSinceStartup - realtimeSinceStartup;
		_costAtT = Time.unscaledTime;
		_costValid = true;
		_costDirtyRelease = false;
	}

	public static void CostNow()
	{
		_costDirtyRelease = true;
	}

	private void DrawResumeOffer()
	{
		DrawInterruptedOffer(standalone: false);
	}

	public static void RefreshResumeProbe()
	{
		_probed = false;
		_probe = null;
		_tapeProbed = false;
		_salvage = null;
		_runProbe = null;
		_runProbeRun = null;
		_runProbeDone = false;
		_orphanScanned = false;
		_deleteArmed = false;
	}

	private static void ProbeTapeCoefficient()
	{
		if (_tapeProbed)
		{
			return;
		}
		_tapeProbed = true;
		string graphicsDeviceName = SystemInfo.graphicsDeviceName;
		try
		{
			_tapeKnown = SuperRunLedger.TapeCoefficient(graphicsDeviceName, out _tapeMed, out _tapeLo, out _tapeHi, out _tapeRows);
		}
		catch (Exception ex)
		{
			_tapeKnown = false;
			Debug.LogWarning("[Super] Реестр прогонов не прочитан: " + ex.Message);
		}
	}

	private static string Mins(double seconds)
	{
		if (double.IsNaN(seconds) || double.IsInfinity(seconds))
		{
			return "?";
		}
		if (seconds < 90.0)
		{
			return Localization.T("super.unit.sec", new object[1] { seconds });
		}
		if (seconds < 5400.0)
		{
			return Localization.T("super.unit.min", new object[1] { seconds / 60.0 });
		}
		return Localization.T("super.unit.hour", new object[1] { seconds / 3600.0 });
	}

	private static string KeyedReason(SuperSolveJob.Outcome o)
	{
		if (o == null || string.IsNullOrEmpty(o.reasonKey))
		{
			return null;
		}
		if (o.reasonArgs != null && o.reasonArgs.Length != 0)
		{
			return Localization.T(o.reasonKey, o.reasonArgs);
		}
		return Localization.T(o.reasonKey);
	}

	private static string ReasonLine(SuperSolveJob.Outcome o)
	{
		string keyedReason = KeyedReason(o);
		if (keyedReason != null)
		{
			return Localization.T("super.run.reason", new object[1] { keyedReason });
		}
		if (o == null || string.IsNullOrEmpty(o.reason))
		{
			return "";
		}
		return Localization.T("super.run.reason_technical", new object[1] { o.reason });
	}

	public static string RefusedLine(SuperSolveJob.Outcome o)
	{
		string text = KeyedReason(o);
		if (text == null)
		{
			text = ((o != null && !string.IsNullOrEmpty(o.reason)) ? Localization.T("super.run.reason_technical", new object[1] { o.reason }) : "");
		}
		return Localization.T("super.run.refused_solver", new object[1] { text });
	}

	private static string HollowLine(SuperSolveJob.Outcome o)
	{
		if (o == null || string.IsNullOrEmpty(o.hollowKey))
		{
			return "";
		}
		if (o.hollowArgs != null && o.hollowArgs.Length != 0)
		{
			return Localization.T(o.hollowKey, o.hollowArgs);
		}
		return Localization.T(o.hollowKey);
	}

	public static List<KeyValuePair<OutcomeLineKind, string>> OutcomeLines(SuperSolveJob.Outcome o)
	{
		List<KeyValuePair<OutcomeLineKind, string>> lines = new List<KeyValuePair<OutcomeLineKind, string>>();
		void Add(OutcomeLineKind k, string s)
		{
			if (!string.IsNullOrEmpty(s))
			{
				lines.Add(new KeyValuePair<OutcomeLineKind, string>(k, s));
			}
		}
		if (o == null)
		{
			return lines;
		}
		double num = (double)o.tapeBytes / 1048576.0;
		switch (o.end)
		{
		case SuperSolveJob.RunEnd.Done:
			Add(OutcomeLineKind.Head, Localization.T(o.resumed ? "super.run.done_resumed" : "super.run.done"));
			Add(OutcomeLineKind.Text, Localization.T("super.run.tape_line", new object[2] { o.tapePath, num }));
			Add(SuperSolveJob.HollowLineNeedsAttention(o.hollowKey) ? OutcomeLineKind.Failure : OutcomeLineKind.Head, HollowLine(o));
			break;
		case SuperSolveJob.RunEnd.Invalid:
			Add(OutcomeLineKind.Failure, Localization.T("super.run.invalid"));
			Add(OutcomeLineKind.Text, Localization.T("super.run.tape_line", new object[2] { o.tapePath, num }));
			Add(SuperSolveJob.HollowLineNeedsAttention(o.hollowKey) ? OutcomeLineKind.Failure : OutcomeLineKind.Head, HollowLine(o));
			Add(OutcomeLineKind.Dim, ReasonLine(o));
			Add(OutcomeLineKind.Dim, Localization.T("super.run.invalid_note"));
			break;
		case SuperSolveJob.RunEnd.Failed:
			Add(OutcomeLineKind.Failure, Localization.T("super.run.failed"));
			Add(OutcomeLineKind.Dim, ReasonLine(o));
			if (string.IsNullOrEmpty(o.tapePath))
			{
				Add(OutcomeLineKind.Dim, Localization.T("super.run.nothing_saved"));
			}
			break;
		case SuperSolveJob.RunEnd.Refused:
			Add(OutcomeLineKind.Head, Localization.T("super.run.refused"));
			Add(OutcomeLineKind.Refused, RefusedLine(o));
			break;
		default:
			Add(OutcomeLineKind.Head, Localization.T(o.resumed ? "super.run.cancelled_resumed" : "super.run.cancelled"));
			break;
		}
		return lines;
	}

	private static GUIStyle StyleOf(OutcomeLineKind k)
	{
		switch (k)
		{
		case OutcomeLineKind.Head:
			return _head;
		case OutcomeLineKind.Failure:
			return _failure;
		case OutcomeLineKind.Dim:
			return _dim;
		case OutcomeLineKind.Refused:
			return _refused;
		default:
			return _text;
		}
	}

	private static void EnsureRunStyles()
	{
		if (_failure != null)
		{
			return;
		}
		_failure = new GUIStyle(_text);
		_failure.wordWrap = true;
		_failure.fontStyle = FontStyle.Bold;
		_failure.normal.textColor = MenuTheme.Failure;
	}

	private void DrawChip(bool canExpand)
	{
		GUILayout.BeginArea(new Rect((float)(Screen.width - 620) * 0.5f, 8f, 620f, 64f), _panel);
		float num = Mathf.Clamp(SuperSolveJob.Progress, 0f, 1f);
		GUILayout.Label(Localization.T("super.run.chip", new object[4]
		{
			num * 100f,
			SuperSolveJob.Frames,
			SuperSolveJob.Expected,
			Mins(SuperSolveJob.ElapsedS)
		}), _text);
		GUILayout.BeginHorizontal();
		if (canExpand && GUILayout.Button(Localization.T("super.run.expand"), _button))
		{
			_progressCollapsed = false;
		}
		if (SuperSolveJob.CancelRequested)
		{
			GUILayout.Label(Localization.T("super.progress.cancelling"), _dim);
		}
		else if (GUILayout.Button(Localization.T("super.progress.cancel"), _button))
		{
			SuperSolveJob.CancelRequested = true;
		}
		GUILayout.EndHorizontal();
		GUILayout.EndArea();
	}

	private void DrawOutcome(SuperSolveJob.Outcome o)
	{
		EnsureRunStyles();
		float num = Mathf.Min((float)Screen.height - 40f, 400f);
		GUILayout.BeginArea(new Rect((float)(Screen.width - 620) * 0.5f, Mathf.Max(20f, (float)Screen.height * 0.5f - num * 0.5f), 620f, num), _panel);
		foreach (KeyValuePair<OutcomeLineKind, string> item in OutcomeLines(o))
		{
			GUILayout.Label(item.Value, StyleOf(item.Key));
		}
		if (o.end <= SuperSolveJob.RunEnd.Invalid)
		{
			DrawPredictedVsActual(o);
			DrawTapeActions(o, recovered: false);
		}
		else if (o.end == SuperSolveJob.RunEnd.Failed && !string.IsNullOrEmpty(o.tapePath))
		{
			DrawSalvageLine(o.tapePath);
			DrawInterruptedActions(o.tapePath, o.run, dismissOutcome: true);
		}
		else if (GUILayout.Button(Localization.T("super.run.ok"), _button))
		{
			SuperSolveJob.DismissOutcome();
		}
		if (!string.IsNullOrEmpty(_screenNote))
		{
			GUILayout.Label(_screenNote, _refused);
		}
		GUILayout.EndArea();
	}

	private void DrawPredictedVsActual(SuperSolveJob.Outcome o)
	{
		SuperRunRecord record = o.record;
		if (record == null || record.predicted == null || record.actual == null)
		{
			GUILayout.Label(Localization.T(o.resumed ? "super.run.pva_resumed" : "super.run.pva_none"), _dim);
			return;
		}
		SuperRunRecord.Predicted predicted = record.predicted;
		SuperRunRecord.Actual actual = record.actual;
		string text = ((predicted.solveS > 0.0) ? Localization.T("super.run.pva_pred_solve", new object[2] { Mins(predicted.solveLowS), Mins(predicted.solveHighS) }) : Localization.T("super.run.pva_pred_none"));
		if (predicted.tapeS >= 0.0)
		{
			text += Localization.T("super.run.pva_pred_tape", new object[2] { Mins(predicted.tapeLowS), Mins(predicted.tapeHighS) });
		}
		GUILayout.Label(text, _text);
		GUILayout.Label(Localization.T("super.run.pva_actual", new object[4]
		{
			Mins(Math.Max(0.0, actual.solveS)),
			Mins(Math.Max(0.0, actual.tapeS)),
			Mins(Math.Max(0.0, actual.bakeUploadS)),
			Mins(Math.Max(0.0, actual.wallS))
		}), _text);
		if (predicted.solveS > 0.0 && actual.solveS > 0.0)
		{
			GUILayout.Label(Localization.T("super.run.pva_miss_solve", new object[3]
			{
				(predicted.solveS - actual.solveS) / actual.solveS * 100.0,
				-10.8,
				13.4
			}), _dim);
		}
		if (predicted.tapeS >= 0.0 && predicted.solveS > 0.0 && actual.solveS >= 0.0 && actual.tapeS >= 0.0 && actual.tapeS + actual.solveS > 0.0)
		{
			GUILayout.Label(Localization.T("super.run.pva_miss_total", new object[1]
			{
				(predicted.tapeS + predicted.solveS - (actual.tapeS + actual.solveS)) / (actual.tapeS + actual.solveS) * 100.0
			}), _dim);
		}
		GUILayout.Label(Localization.T(record.CountsForBand() ? "super.run.pva_counts" : "super.run.pva_not_counted"), _dim);
	}

	private void DrawTapeActions(SuperSolveJob.Outcome o, bool recovered)
	{
		GUILayout.BeginHorizontal();
		if (_deleteArmed)
		{
			GUILayout.Label(Localization.T("super.run.delete_confirm"), _text);
			if (GUILayout.Button(Localization.T("super.run.delete_yes"), _button))
			{
				_deleteArmed = false;
				if (!SuperTapeFiles.TryDelete(o.tapePath, out string refusalKey))
				{
					_screenNote = Localization.T(refusalKey, new object[1] { Path.GetFileName(o.tapePath) });
				}
				else
				{
					SuperSolveJob.DismissOutcome();
					_screenNote = "";
					RefreshResumeProbe();
				}
			}
			if (GUILayout.Button(Localization.T("super.run.delete_no"), _button))
			{
				_deleteArmed = false;
			}
		}
		else
		{
			if (GUILayout.Button(Localization.T("super.run.open"), _button) && o != null)
			{
				SuperSolveJob.DismissOutcome();
				_screenNote = "";
				SuperSolveJob.OpenViewer(o.tapePath);
			}
			if (GUILayout.Button(Localization.T("super.run.keep"), _button))
			{
				SuperSolveJob.DismissOutcome();
				_screenNote = "";
			}
			if (GUILayout.Button(Localization.T("super.run.delete"), _button))
			{
				_deleteArmed = true;
			}
		}
		GUILayout.EndHorizontal();
	}

	private static void DrawSalvageLine(string writingPath)
	{
		long num = FileLenOf(writingPath);
		if (_salvage == null || _salvage.path != writingPath || _salvage.len != num)
		{
			_salvage = new Salvage
			{
				path = writingPath,
				len = num
			};
			_salvageAskFrame = Time.frameCount;
		}
		if (!_salvage.done)
		{
			GUILayout.Label(Localization.T("super.run.salvage_checking"), _dim);
		}
		else if (_salvage.frames > 0)
		{
			GUILayout.Label(Localization.T("super.run.salvage", new object[2] { _salvage.frames, _salvage.lastUs }), _text);
		}
		else
		{
			GUILayout.Label(Localization.T("super.run.salvage_none", new object[1] { _salvage.error }), _dim);
		}
	}

	private static void ComputeSalvage()
	{
		if (_salvage == null || _salvage.done)
		{
			return;
		}
		if (Time.frameCount <= _salvageAskFrame + 1)
		{
			return;
		}
		try
		{
			using (SuperTape.Reader reader = SuperTape.Reader.OpenRecovering(_salvage.path))
			{
				_salvage.frames = reader.FrameCount;
				_salvage.lastUs = ((reader.FrameCount > 0) ? (reader.FrameTime(reader.FrameCount - 1) * 1000000f) : 0f);
			}
		}
		catch (Exception ex)
		{
			_salvage.frames = 0;
			_salvage.error = ex.Message;
		}
		_salvage.done = true;
	}

	private void DrawInterruptedActions(string writingPath, string run, bool dismissOutcome)
	{
		if (_runProbeRun != run)
		{
			_runProbeRun = run;
			_runProbe = null;
			_runProbeDone = false;
			_runProbeAskFrame = Time.frameCount;
		}
		string text;
		if (!_runProbeDone)
		{
			text = Localization.T("super.run.checkpoint_checking");
		}
		else if (_runProbe == null)
		{
			text = Localization.T("super.run.checkpoint_none");
		}
		else if (_runProbe.usable)
		{
			text = Localization.T("super.run.checkpoint_ok", new object[3]
			{
				_runProbe.simTimeS * 1000000f,
				_runProbe.durationS * 1000000f,
				_runProbe.frames
			});
		}
		else
		{
			text = Localization.T("super.run.checkpoint_bad", new object[1] { _runProbe.note });
		}
		GUILayout.Label(text, _dim);
		if (_deleteArmed)
		{
			GUILayout.Label(Localization.T("super.run.discard_confirm"), _text);
		}
		GUILayout.BeginHorizontal();
		if (_deleteArmed)
		{
			if (GUILayout.Button(Localization.T("super.run.delete_yes"), _button))
			{
				_deleteArmed = false;
				if (File.Exists(writingPath))
				{
					if (SuperTapeFiles.TryDelete(writingPath, out string refusalKey))
					{
						SuperCheckpoint.DiscardAll(run);
						if (dismissOutcome)
						{
							SuperSolveJob.DismissOutcome();
						}
						_screenNote = "";
						RefreshResumeProbe();
					}
					else
					{
						_screenNote = Localization.T(refusalKey, new object[1] { Path.GetFileName(writingPath) });
					}
				}
				else
				{
					SuperCheckpoint.DiscardAll(run);
					if (dismissOutcome)
					{
						SuperSolveJob.DismissOutcome();
					}
					_screenNote = "";
					RefreshResumeProbe();
				}
			}
			if (GUILayout.Button(Localization.T("super.run.delete_no"), _button))
			{
				_deleteArmed = false;
			}
		}
		else
		{
			if (File.Exists(writingPath) && GUILayout.Button(Localization.T("super.run.view_partial"), _button))
			{
				if (dismissOutcome)
				{
					SuperSolveJob.DismissOutcome();
				}
				_screenNote = "";
				SuperSolveJob.OpenViewer(writingPath);
			}
			if (_runProbeDone && _runProbe != null && _runProbe.usable && GUILayout.Button(Localization.T("super.resume.continue"), _button))
			{
				SuperCheckpoint.Probe runProbe = _runProbe;
				if (dismissOutcome)
				{
					SuperSolveJob.DismissOutcome();
				}
				_screenNote = "";
				RefreshResumeProbe();
				SuperSolveJob.Resume(runProbe);
			}
		}
		if (GUILayout.Button(Localization.T("super.run.discard"), _button))
		{
			_deleteArmed = true;
		}
		if (dismissOutcome && GUILayout.Button(Localization.T("super.run.later"), _button))
		{
			SuperSolveJob.DismissOutcome();
			_screenNote = "";
		}
		GUILayout.EndHorizontal();
	}

	private static void ComputeRunProbe()
	{
		if (_runProbeRun == null || _runProbeDone)
		{
			return;
		}
		if (Time.frameCount <= _runProbeAskFrame + 1)
		{
			return;
		}
		try
		{
			_runProbe = SuperCheckpoint.ProbeLatest(_runProbeRun);
		}
		catch (Exception ex)
		{
			_runProbe = new SuperCheckpoint.Probe
			{
				usable = false,
				note = ex.Message,
				run = _runProbeRun
			};
		}
		_runProbeDone = true;
	}

	private static void ScanOrphan()
	{
		if (_orphanScanned)
		{
			return;
		}
		_orphanScanned = true;
		_orphanPath = null;
		string tapeDir = SuperTapeRecorder.TapeDir;
		if (!Directory.Exists(tapeDir))
		{
			return;
		}
		try
		{
			DateTime t = DateTime.MinValue;
			string[] files = Directory.GetFiles(tapeDir, "*.writing");
			for (int i = 0; i < files.Length; i++)
			{
				string text = files[i];
				if (SuperTapeFiles.IsActiveRunTape(text))
				{
					continue;
				}
				if (!Path.GetFileName(text).StartsWith("player_", StringComparison.Ordinal))
				{
					continue;
				}
				DateTime lastWriteTime = File.GetLastWriteTime(text);
				if (lastWriteTime > t)
				{
					t = lastWriteTime;
					_orphanPath = text;
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[Super] Поиск оборванных лент не удался: " + ex.Message);
		}
	}

	public static void RunOfferRect(float screenW, float screenH, out Rect accent, out Rect area)
	{
		float num = Mathf.Clamp(screenW * 0.28f, 300f, 620f);
		num = Mathf.Min(num, screenW - 32f);
		float num2 = Mathf.Clamp((620f - num) * 0.3f + 230f, 230f, 320f);
		num2 = Mathf.Min(num2, screenH - 24f);
		float num3 = Mathf.Max(16f, screenW - 16f - num);
		float num4 = Mathf.Max(12f, screenH - 16f - num2);
		accent = new Rect(num3, num4, 3f, num2);
		area = new Rect(num3 + 3f, num4, num - 3f, num2);
	}

	public static Rect LegacyRunOfferRect(float screenW)
	{
		return new Rect((screenW - 620f) * 0.5f, 12f, 620f, 230f);
	}

	private void DrawInterruptedOffer(bool standalone)
	{
		if (_orphanHidden || SuperSolveJob.Running || SuperSolveJob.LastOutcome != null)
		{
			return;
		}
		ScanOrphan();
		if (_orphanPath == null || !File.Exists(_orphanPath))
		{
			return;
		}
		EnsureRunStyles();
		string run = SuperTapeFiles.RunNameOf(_orphanPath);
		bool headWrap = _head.wordWrap;
		bool textWrap = _text.wordWrap;
		try
		{
			if (standalone)
			{
				RunOfferRect(Screen.width, Screen.height, out var accent, out var area);
				GUI.DrawTexture(accent, _lineTex);
				GUILayout.BeginArea(area, _panel);
				_head.wordWrap = true;
				_text.wordWrap = true;
			}
			GUILayout.Label(Localization.T("super.run.interrupted_title", new object[2]
			{
				Path.GetFileName(_orphanPath),
				File.GetLastWriteTime(_orphanPath).ToString("g", Localization.Culture)
			}), standalone ? _head : _text);
			DrawSalvageLine(_orphanPath);
			DrawInterruptedActions(_orphanPath, run, dismissOutcome: false);
			if (!_deleteArmed && GUILayout.Button(Localization.T("super.run.later"), _button))
			{
				_orphanHidden = true;
			}
			if (!string.IsNullOrEmpty(_screenNote))
			{
				GUILayout.Label(_screenNote, _refused);
			}
		}
		finally
		{
			_head.wordWrap = headWrap;
			_text.wordWrap = textWrap;
		}
		if (standalone)
		{
			GUILayout.EndArea();
		}
	}

	private void Update()
	{
		ComputeSalvage();
		ComputeRunProbe();
	}

	private static long FileLenOf(string path)
	{
		if (!File.Exists(path))
		{
			return -1L;
		}
		return new FileInfo(path).Length;
	}

	private static void ResetRunScreenCaches()
	{
		_salvage = null;
		_runProbe = null;
		_runProbeRun = null;
		_runProbeDone = false;
		_orphanScanned = false;
		_deleteArmed = false;
	}

	static SuperHud()
	{
		_costFrame = -1;
		_costAtT = -1f;
		_screenNote = "";
		_salvageAskFrame = -1;
		_runProbeAskFrame = -1;
	}
}
