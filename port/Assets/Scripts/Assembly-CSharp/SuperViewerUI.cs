using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public sealed class SuperViewerUI : MonoBehaviour
{
	private struct MineRow
	{
		public RectTransform rect;

		public SuperTapeLibrary.Entry entry;
	}

	private struct KeyedLabel
	{
		public TextMeshProUGUI label;

		public string key;
	}

	[CompilerGenerated]
	private sealed class _003COpenRoutine_003Ed__96 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public SuperViewerUI _003C_003E4__this;

		public string path;

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
		public _003COpenRoutine_003Ed__96(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
		}

		private bool MoveNext()
		{
			switch (_003C_003E1__state)
			{
			default:
				return false;
			case 0:
				_003C_003E1__state = -1;
				_003C_003E2__current = null;
				_003C_003E1__state = 1;
				return true;
			case 1:
				_003C_003E1__state = -1;
				_003C_003E2__current = null;
				_003C_003E1__state = 2;
				return true;
			case 2:
				_003C_003E1__state = -1;
				_003C_003E4__this.OpenTapeNow(path);
				_003C_003E4__this._opening = false;
				return false;
			}
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}
	}

	private SuperTape.Reader _tape;

	private SuperTapeFeed _feed;

	private SuperTapeVerifier _verifier;

	private bool _corruptAnnounced;

	private SuperTapeRender _render;

	private RawImage _picture;

	private TextMeshProUGUI _readout;

	private Slider _frames;

	private int _frame;

	private int _field;

	private bool _playing;

	private bool _cut;

	private bool _surface;

	private float _speed;

	private float _carry;

	private int _loadedFrame;

	private int _loadedField;

	private bool _loadedCut;

	private bool _loadedSurf;

	private float _loadedAz;

	private float _fitCentreZ;

	private float _fitHalfY;

	private float _fitHalfZ;

	private int _loadedW;

	private int _loadedH;

	private SuperTape.Frame _cur;

	private readonly SuperViewState _view;

	private float _loadedYaw;

	private float _loadedPitch;

	private bool _decorShown;

	private bool _loadedDecor;

	private TextMeshProUGUI _decorLabel;

	private Canvas _canvas;

	private RectTransform _libraryPanel;

	private bool _showGallery;

	private Button _tabGallery;

	private Button _tabMine;

	private TextMeshProUGUI _libWhere;

	private TextMeshProUGUI _banner;

	private string _tapePath;

	private readonly List<Behaviour> _hidden;

	private static int _open;

	public const float ZoomStep = 1.15f;

	public const float ZoomInLimit = 0.05f;

	public const float ZoomOutLimit = 4f;

	private int _loadedZoom;

	public const float DragDegPerPixel = 0.3f;

	public const float ArrowStepDeg = 5f;

	private float _touchPinchAccum;

	private string _lastSaveDir;

	public const float StatusHoldS = 8f;

	private string _status;

	private Color _statusColor;

	private float _statusUntil;

	private bool _statusShown;

	private bool _opening;

	private Button _compactAll;

	private readonly List<string> _compactCandidates;

	private SuperTapeCompact.Job _compactJob;

	private float _compactTickAt;

	private const float CompactBtnW = 170f;

	public const float MineRowHeight = 64f;

	public const float ConfirmWindowS = 5f;

	private const float OpenBtnW = 150f;

	private const float DeleteBtnW = 240f;

	private string _mineDir;

	private readonly List<MineRow> _rows;

	private RectTransform _libViewport;

	private TextMeshProUGUI _libStatus;

	private string _libMsg;

	private Color _libMsgColor;

	private float _libMsgUntil;

	private int _libSig;

	private string _armedPath;

	private string _armedName;

	private float _armedUntil;

	private Button _armedBtn;

	private readonly List<KeyedLabel> _keyed;

	private TextMeshProUGUI _marker;

	private const float MarkerY = 80f;

	public const float BarHeight = 196f;

	private const float Row1Y = 20f;

	private const float Row2Y = 66f;

	private Image[] _bands;

	private TextMeshProUGUI[] _ticks;

	private TextMeshProUGUI _legendTitle;

	private TextMeshProUGUI _legendDead;

	private TextMeshProUGUI _stamp;

	private TextMeshProUGUI _counter;

	private const float LegendTitleMargin = 12f;

	private TextMeshProUGUI _fieldLabel;

	public SuperViewState View => _view;

	public bool HasTape => _tape != null;

	public string TapePath => _tapePath;

	public static bool IsOpen => _open > 0;

	public bool DecorShown => _decorShown;

	public string StatusText => ((_status != null) ? _status : "");

	public SuperTapeVerifier VerifierForGate => _verifier;

	public SuperTapeCompact.Job CompactJobForGate => _compactJob;

	public SuperTapeCompact.Result[] LastCompactResults { get; private set; }

	private static Color InkColor => SuperLegend.Ink;

	public SuperViewerUI()
	{
		_field = 1;
		_playing = true;
		_cut = true;
		_surface = true;
		_speed = 1f;
		_loadedFrame = -1;
		_loadedW = -1;
		_view = new SuperViewState();
		_decorShown = true;
		_loadedDecor = true;
		_showGallery = true;
		_tapePath = "";
		_hidden = new List<Behaviour>();
		_lastSaveDir = "";
		_status = "";
		_statusUntil = -1f;
		_compactCandidates = new List<string>();
		LastCompactResults = new SuperTapeCompact.Result[0];
		_rows = new List<MineRow>();
		_libMsg = "";
		_libMsgUntil = -1f;
		_libSig = int.MinValue;
		_keyed = new List<KeyedLabel>();
	}

	private void Start()
	{
		_open++;
		SuperTapeCompact.CleanStale(SuperTapeRecorder.TapeDir);
		HideSandbox();
		BuildUI();
		string text = SuperSolveJob.TakeViewerRequest();
		if (text == null)
		{
			text = SuperSolveJob.LastTapePath ?? "";
		}
		if (string.IsNullOrEmpty(text))
		{
			ShowLibrary(on: true);
		}
		else
		{
			OpenTape(text);
		}
	}

	private void OnDestroy()
	{
		Localization.OnLanguageChanged -= RefreshTexts;
		_open = Math.Max(0, _open - 1);
		if (_feed != null)
		{
			_feed.Dispose();
			_feed = null;
		}
		if (_compactJob != null)
		{
			_compactJob.Cancel();
		}
		if (_verifier != null)
		{
			_verifier.Dispose();
			_verifier = null;
		}
		if (_render != null)
		{
			_render.Dispose();
			_render = null;
		}
		if (_tape != null)
		{
			_tape.Dispose();
			_tape = null;
		}
		RestoreSandbox();
	}

	private void HideSandbox()
	{
		Scene scene = base.gameObject.scene;
		Behaviour[] array = UnityEngine.Object.FindObjectsByType<Behaviour>(FindObjectsSortMode.None);
		Behaviour[] array2 = array;
		foreach (Behaviour behaviour in array2)
		{
			if (behaviour.gameObject.scene != scene && behaviour.enabled)
			{
				behaviour.enabled = false;
				_hidden.Add(behaviour);
			}
		}
		Behaviour[] array3 = UnityEngine.Object.FindObjectsByType<Behaviour>(FindObjectsSortMode.None);
		Behaviour[] array4 = array3;
		foreach (Behaviour behaviour2 in array4)
		{
			if (behaviour2.gameObject.scene != scene && behaviour2.enabled)
			{
				behaviour2.enabled = false;
				_hidden.Add(behaviour2);
			}
		}
	}

	private void RestoreSandbox()
	{
		if (_hidden != null)
		{
			foreach (Behaviour item in _hidden)
			{
				if (item != null)
				{
					item.enabled = true;
				}
			}
			_hidden.Clear();
		}
	}

	public void Exit()
	{
		RestoreSandbox();
		SceneManager.UnloadSceneAsync(base.gameObject.scene);
	}

	private void Zoom(int delta)
	{
		_view.Zoom(delta);
	}

	private static bool PointerOnUi()
	{
		return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
	}

	private void ReadZoomInput()
	{
		if (_libraryPanel != null && _libraryPanel.gameObject.activeSelf)
		{
			return;
		}
		if (WheelZones.CursorOverPanel())
		{
			return;
		}
		if (TouchInputHelper.TouchActive)
		{
			if (Input.touchCount == 2)
			{
				_touchPinchAccum += TouchInputHelper.PinchDelta;
				float step = 40f;
				while (Mathf.Abs(_touchPinchAccum) >= step)
				{
					_view.Zoom((_touchPinchAccum > 0f) ? 1 : (-1));
					_touchPinchAccum += ((_touchPinchAccum > 0f) ? (-step) : step);
				}
			}
			return;
		}
		Vector2 mouseScrollDelta = Input.mouseScrollDelta;
		if (mouseScrollDelta.y > 0.01f)
		{
			_view.Zoom(1);
		}
		else if (mouseScrollDelta.y < -0.01f)
		{
			_view.Zoom(-1);
		}
		if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus))
		{
			_view.Zoom(-1);
		}
		if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
		{
			_view.Zoom(1);
		}
	}

	private void ReadRotateInput()
	{
		if (_libraryPanel != null && _libraryPanel.gameObject.activeSelf)
		{
			return;
		}
		if (TouchInputHelper.TouchActive)
		{
			if (Input.touchCount == 1)
			{
				Touch touch = Input.GetTouch(0);
				if (touch.phase == TouchPhase.Moved && !TouchInputHelper.PointerOverUI && touch.deltaPosition.sqrMagnitude > 0f)
				{
					_view.Rotate(-touch.deltaPosition.x * DragDegPerPixel * 10f, touch.deltaPosition.y * DragDegPerPixel * 10f);
				}
			}
			return;
		}
		if (Input.GetMouseButton(1) && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
		{
			float num = Input.GetAxisRaw("Mouse X");
			float num2 = Input.GetAxisRaw("Mouse Y");
			if (num != 0f || num2 != 0f)
			{
				_view.Rotate(-num * DragDegPerPixel * 10f, num2 * DragDegPerPixel * 10f);
			}
		}
		if (Input.GetKeyDown(KeyCode.LeftArrow))
		{
			_view.Rotate(-5f, 0f);
		}
		if (Input.GetKeyDown(KeyCode.RightArrow))
		{
			_view.Rotate(5f, 0f);
		}
		if (Input.GetKeyDown(KeyCode.UpArrow))
		{
			_view.Rotate(0f, 5f);
		}
		if (Input.GetKeyDown(KeyCode.DownArrow))
		{
			_view.Rotate(0f, -5f);
		}
		if (Input.GetKeyDown(KeyCode.Home))
		{
			_view.Reset();
		}
	}

	private void Rotate(float dYaw, float dPitch)
	{
		_view.Rotate(dYaw, dPitch);
	}

	public void ResetView()
	{
		_view.Reset();
	}

	public static int PlaybackAdvance(ref float carry)
	{
		if (carry >= 1f)
		{
			carry -= 1f;
			if (carry > 1f)
			{
				carry = 1f;
			}
			return 1;
		}
		return 0;
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			if (_libraryPanel != null && _libraryPanel.gameObject.activeSelf && _tape != null)
			{
				ShowLibrary(on: false);
				return;
			}
			RestoreSandbox();
			SceneManager.UnloadSceneAsync(base.gameObject.scene);
			return;
		}
		TickStatus();
		TickLibrary();
		TickVerification();
		TickCompaction();
		if (_tape == null)
		{
			return;
		}
		ReadZoomInput();
		ReadRotateInput();
		if (_playing)
		{
			_carry += (float)_tape.FrameCount / 5f * Time.unscaledDeltaTime * _speed;
			if (PlaybackAdvance(ref _carry) != 0)
			{
				_frame = (_frame + 1) % _tape.FrameCount;
			}
		}
		int num = Mathf.RoundToInt(((_canvas != null) ? _canvas.scaleFactor : 1f) * BarHeight);
		int num2 = Mathf.Max(Screen.width, 64);
		int num3 = Mathf.Max(Screen.height - num, 36);
		bool flag;
		if (_frame == _loadedFrame && _field == _loadedField && _cut == _loadedCut && _surface == _loadedSurf)
		{
			flag = !Mathf.Approximately(_view.azimuthDeg, _loadedAz);
		}
		else
		{
			flag = true;
		}
		bool flag2;
		if (num2 == _loadedW && num3 == _loadedH && _view.zoomSteps == _loadedZoom && _view.yawDeg == _loadedYaw && _view.pitchDeg == _loadedPitch)
		{
			flag2 = _decorShown != _loadedDecor;
		}
		else
		{
			flag2 = true;
		}
		if (flag || _cur == null)
		{
			RenderNow(num2, num3);
		}
		else if (flag2)
		{
			RenderViewOnly(num2, num3);
		}
	}

	public void RenderViewOnly(int w, int h)
	{
		if (_tape == null || _render == null)
		{
			return;
		}
		if (_cur == null)
		{
			RenderNow(w, h);
			return;
		}
		try
		{
			ApplyFraming(w, h);
			_render.DecorVisible = _decorShown;
			_render.ApplyView();
			RawImage picture = _picture;
			RenderTexture texture = _render.RenderToTexture(w, h);
			if (picture != null)
			{
				picture.texture = texture;
			}
			_loadedW = w;
			_loadedH = h;
			_loadedZoom = _view.zoomSteps;
			_loadedYaw = _view.yawDeg;
			_loadedPitch = _view.pitchDeg;
			_loadedDecor = _decorShown;
			UpdateMarker();
		}
		catch (Exception ex)
		{
			Debug.LogException(ex);
			CloseTape();
			Fail(Localization.T("super.viewer.read_failed", new object[2]
			{
				_frame,
				ex.Message
			}));
			ShowLibrary(on: true);
		}
	}

	public void RenderNow(int w, int h)
	{
		if (_tape == null || _render == null)
		{
			return;
		}
		try
		{
			_cur = ((_feed != null) ? _feed.Get(_frame, _field) : _tape.ReadFrame(_frame, SuperTape.Detail.OneField, _field));
			ApplyFraming(w, h);
			_render.DecorVisible = _decorShown;
			_render.SetFrame(_cur, _field, _cut, _view.azimuthDeg, _surface);
			_picture.texture = _render.RenderToTexture(w, h);
			_picture.color = Color.white;
			_loadedFrame = _frame;
			_loadedField = _field;
			_loadedCut = _cut;
			_loadedSurf = _surface;
			_loadedAz = _view.azimuthDeg;
			_loadedDecor = _decorShown;
			_loadedW = w;
			_loadedH = h;
			_loadedZoom = _view.zoomSteps;
			_loadedYaw = _view.yawDeg;
			_loadedPitch = _view.pitchDeg;
			if (_frames != null)
			{
				_frames.value = 0f;
				_frames.maxValue = _tape.FrameCount - 1;
				_frames.wholeNumbers = true;
				_frames.value = 0f;
			}
			Readout();
			UpdateLegend();
		}
		catch (Exception ex)
		{
			Debug.LogException(ex);
			CloseTape();
			if (ex is SuperTapeVerifier.CorruptFrameException corruptFrameException)
			{
				Fail(Localization.T("super.viewer.tape_corrupt", new object[1] { corruptFrameException.Frame }));
			}
			else
			{
				Fail(Localization.T("super.viewer.read_failed", new object[2]
				{
					_frame,
					ex.Message
				}));
			}
			ShowLibrary(on: true);
		}
	}

	private void SaveVideo()
	{
		string refusal;
		if (!VideoAllowed(out refusal))
		{
			Refuse(refusal);
			return;
		}
		Refuse(Localization.T("super.viewer.save_editor_only"));
	}

	public void ToggleDecor()
	{
		_decorShown = !_decorShown;
		UpdateDecorLabel();
	}

	private string DecorCaption()
	{
		if (_render != null && _render.HasDecor)
		{
			return Localization.T(((_decorShown) ? "super.viewer.decor_on" : "super.viewer.decor_off"));
		}
		return Localization.T("super.viewer.decor_none");
	}

	private void UpdateDecorLabel()
	{
		if (_decorLabel != null)
		{
			_decorLabel.text = DecorCaption();
		}
	}

	public static float ZoomOutLimitFor(SuperDecor d, float fitCentreZ, float fitHalfY, float fitHalfZ)
	{
		if (d == null || d.Empty || !(fitHalfY > 0f) || !(fitHalfZ > 0f))
		{
			return 4f;
		}
		Bounds bounds = d.Bounds();
		float num = Mathf.Max(Mathf.Abs(bounds.center.x - bounds.extents.x), Mathf.Abs(bounds.center.x + bounds.extents.x));
		float num2 = Mathf.Max(Mathf.Abs(bounds.center.y - bounds.extents.y), Mathf.Abs(bounds.center.y + bounds.extents.y));
		float num3 = Mathf.Max(Mathf.Abs(bounds.center.z - bounds.extents.z - fitCentreZ), Mathf.Abs(bounds.center.z + bounds.extents.z - fitCentreZ));
		return Mathf.Max(4f, Mathf.Max(Mathf.Max(num, num2) / fitHalfY, num3 / fitHalfZ) * 1.1f);
	}

	public bool VideoAllowed(out string refusal)
	{
		refusal = null;
		if (_tape == null || string.IsNullOrEmpty(_tapePath))
		{
			refusal = Localization.T("super.viewer.nothing_to_save");
			return false;
		}
		if (SuperTapeFiles.IsPartialByName(_tapePath))
		{
			refusal = Localization.T("super.viewer.partial_no_video");
			return false;
		}
		if (_tape.Recovered)
		{
			refusal = PartialText(_tape);
			return false;
		}
		return true;
	}

	private static bool IsPartial(SuperTape.Reader r, string path)
	{
		if (r.Recovered)
		{
			return true;
		}
		return SuperTapeFiles.IsPartialByName(path);
	}

	private static string PartialText(SuperTape.Reader r)
	{
		return Localization.T("super.viewer.recovered", new object[2]
		{
			r.FrameCount,
			(r.FrameTime(r.FrameCount - 1) * 1000000f).ToString("F0")
		});
	}

	private void Post(string text, Color color)
	{
		_status = text ?? "";
		_statusColor = color;
		_statusUntil = Time.unscaledTime + StatusHoldS;
		_statusShown = true;
		Readout();
		if (_libraryPanel != null && _libraryPanel.gameObject.activeSelf)
		{
			_libMsg = text ?? "";
			_libMsgColor = color;
			_libMsgUntil = Time.unscaledTime + StatusHoldS;
			_libSig = int.MinValue;
		}
	}

	private void Say(string text)
	{
		Debug.Log("[Super] " + text);
		Post(text, MenuTheme.TextPrimary);
	}

	private void Refuse(string text)
	{
		Debug.LogWarning("[Super] " + text);
		Post(text, MenuTheme.Refused);
	}

	private void TickStatus()
	{
		if (_statusShown && Time.unscaledTime >= _statusUntil)
		{
			_statusShown = false;
			Readout();
		}
	}

	private void Readout()
	{
		if (_readout == null)
		{
			return;
		}
		if (_statusShown)
		{
			_readout.text = _status;
			_readout.color = _statusColor;
			return;
		}
		_readout.color = MenuTheme.TextMuted;
		if (_cur == null || _tape == null)
		{
			_readout.text = "";
			return;
		}
		SuperTape.Header header = _tape.header;
		if (_render != null && _render.LastDebrisDrawn < _cur.debrisCount)
		{
			_readout.text = Localization.T("super.viewer.readout.capped", new object[6]
			{
				SuperTapeLibrary.DisplayFieldName(header.fieldNames[_field]),
				_cur.fieldMin[_field],
				_cur.fieldMax[_field],
				SuperTapeLibrary.DisplayUnit(header.fieldUnits[_field]),
				_cur.debrisCount,
				_render.LastDebrisDrawn
			});
		}
		else
		{
			_readout.text = Localization.T("super.viewer.readout", new object[5]
			{
				SuperTapeLibrary.DisplayFieldName(header.fieldNames[_field]),
				_cur.fieldMin[_field],
				_cur.fieldMax[_field],
				SuperTapeLibrary.DisplayUnit(header.fieldUnits[_field]),
				_cur.debrisCount
			});
		}
	}

	public void OpenTape(string path)
	{
		if (_opening)
		{
			return;
		}
		if (string.IsNullOrEmpty(path))
		{
			Refuse(Localization.T("super.viewer.no_tapes"));
			return;
		}
		long num = 0L;
		try
		{
			num = new FileInfo(path).Length;
		}
		catch
		{
		}
		Say(Localization.T("super.viewer.opening", new object[2]
		{
			Path.GetFileName(path),
			((double)num / 1048576.0).ToString("F0")
		}));
		if (Application.isPlaying && base.isActiveAndEnabled)
		{
			_opening = true;
			StartCoroutine(OpenRoutine(path));
		}
		else
		{
			OpenTapeNow(path);
		}
	}

	[IteratorStateMachine(typeof(_003COpenRoutine_003Ed__96))]
	private IEnumerator OpenRoutine(string path)
	{
		_003COpenRoutine_003Ed__96 openRoutine = new _003COpenRoutine_003Ed__96(0);
		openRoutine._003C_003E4__this = this;
		openRoutine.path = path;
		return openRoutine;
	}

	public bool OpenTapeNow(string path)
	{
		SuperTape.Reader tape = null;
		SuperTapeRender render = null;
		SuperTapeVerifier verifier = null;
		SuperDecor decor = null;
		string decorError = null;
		SuperRunRecord record = null;
		try
		{
			tape = SuperTape.Reader.OpenRecovering(path);
			render = new SuperTapeRender(tape.header.bodyNames.Length);
			try
			{
				if (tape.DecorBytes != null)
				{
					decor = SuperDecor.Decode(tape.DecorBytes);
				}
			}
			catch (Exception ex)
			{
				decorError = ex.Message;
			}
			render.SetDecor(decor);
			SuperTapeRender.FitTape(tape, out var centreZ, out var halfY, out var halfZ);
			if (!tape.Recovered)
			{
				verifier = new SuperTapeVerifier(tape);
			}
			if (_feed != null)
			{
				_feed.Dispose();
			}
			_feed = null;
			if (_verifier != null)
			{
				_verifier.Dispose();
			}
			_verifier = null;
			if (_render != null)
			{
				_render.Dispose();
			}
			if (_tape != null)
			{
				_tape.Dispose();
			}
			_cur = null;
			_loadedFrame = -1;
			_frame = 0;
			_carry = 0f;
			_corruptAnnounced = false;
			_tape = tape;
			SuperTapeFeed superTapeFeed = new SuperTapeFeed(tape);
			superTapeFeed.Verifier = verifier;
			_feed = superTapeFeed;
			_verifier = verifier;
			_render = render;
			_tapePath = path;
			_fitCentreZ = centreZ;
			_fitHalfY = halfY;
			_fitHalfZ = halfZ;
			_view.zoomOutLimit = ZoomOutLimitFor(decor, centreZ, halfY, halfZ);
			if (_view.ZoomFactor > _view.zoomOutLimit)
			{
				_view.zoomSteps = 0;
			}
			UpdateDecorLabel();
			if (decorError != null)
			{
				Refuse(Localization.T("super.viewer.decor_bad", new object[1] { decorError }));
			}
			if (SuperRunRecord.TryLoad(path, out record) && record.view != null && !float.IsNaN(record.view.cutAzimuthDeg) && !float.IsInfinity(record.view.cutAzimuthDeg))
			{
				_view.azimuthDeg = Mathf.Repeat(Mathf.Round(record.view.cutAzimuthDeg / 30f) * 30f + 90f, 180f) - 90f;
			}
			else
			{
				_view.azimuthDeg = 0f;
			}
			_field = Mathf.Clamp(_field, 0, tape.header.fieldNames.Length - 1);
			if (_fieldLabel != null)
			{
				_fieldLabel.text = FieldCaption();
			}
			if (_frames != null)
			{
				_frames.value = 0f;
				_frames.maxValue = tape.FrameCount - 1;
				_frames.wholeNumbers = true;
				_frames.value = 0f;
			}
			_playing = true;
			UpdateBanner();
			if (tape.Recovered)
			{
				Debug.LogWarning("[Super] " + Path.GetFileName(path) + ": " + tape.RecoveryNote);
			}
			_statusShown = false;
			ShowLibrary(on: false);
			return true;
		}
		catch (Exception ex2)
		{
			if (verifier != null)
			{
				verifier.Dispose();
			}
			if (render != null)
			{
				render.Dispose();
			}
			if (tape != null)
			{
				tape.Dispose();
			}
			Fail(Localization.T("super.viewer.open_failed", new object[1] { ex2.Message }));
			if (_tape == null)
			{
				ShowLibrary(on: true);
			}
			return false;
		}
	}

	private void UpdateBanner()
	{
		if (_banner == null)
		{
			return;
		}
		if (_tape != null && _corruptAnnounced && _verifier != null && _verifier.FirstBad >= 0)
		{
			_banner.text = Localization.T("super.viewer.tape_corrupt", new object[1] { _verifier.FirstBad });
		}
		else if (_tape != null && (_tape.Recovered || SuperTapeFiles.IsPartialByName(_tapePath)))
		{
			_banner.text = PartialText(_tape);
		}
		else
		{
			_banner.text = "";
		}
	}

	private void TickVerification()
	{
		if (_verifier == null || _corruptAnnounced)
		{
			return;
		}
		int firstBad = _verifier.FirstBad;
		if (firstBad < 0)
		{
			if (_verifier.Done && _verifier.Failure != null)
			{
				_corruptAnnounced = true;
				Refuse(Localization.T("super.viewer.verify_failed", new object[1] { _verifier.Failure }));
			}
			return;
		}
		_corruptAnnounced = true;
		Fail(Localization.T("super.viewer.tape_corrupt", new object[1] { firstBad }));
		UpdateBanner();
	}

	public void GateAwaitVerification(int ms = 600000)
	{
		if (_verifier != null)
		{
			_verifier.Join(ms);
		}
		TickVerification();
	}

	public void CloseTape()
	{
		if (_feed != null)
		{
			_feed.Dispose();
		}
		_feed = null;
		if (_verifier != null)
		{
			_verifier.Dispose();
		}
		_verifier = null;
		_corruptAnnounced = false;
		if (_render != null)
		{
			_render.Dispose();
		}
		_render = null;
		if (_tape != null)
		{
			_tape.Dispose();
		}
		_tape = null;
		_tapePath = "";
		_cur = null;
		_frame = 0;
		_carry = 0f;
		_loadedFrame = -1;
		if (_picture != null)
		{
			_picture.texture = null;
			_picture.color = MenuTheme.Background;
		}
		if (_frames != null)
		{
			_frames.value = 0f;
		}
		if (_fieldLabel != null)
		{
			_fieldLabel.text = ((_tape == null) ? Localization.T("super.viewer.field") : Localization.T("super.viewer.field_caption", new object[2]
			{
				SuperTapeLibrary.DisplayFieldName(_tape.header.fieldNames[_field]),
				SuperTapeLibrary.DisplayUnit(_tape.header.fieldUnits[_field])
			}));
		}
		UpdateDecorLabel();
		if (_banner != null)
		{
			if (_tape != null && _corruptAnnounced && _verifier != null && _verifier.FirstBad >= 0)
			{
				_banner.text = Localization.T("super.viewer.tape_corrupt", new object[1] { _verifier.FirstBad });
			}
			else if (_tape != null && (_tape.Recovered || SuperTapeFiles.IsPartialByName(_tapePath)))
			{
				_banner.text = Localization.T("super.viewer.recovered", new object[2]
				{
					_tape.FrameCount,
					(_tape.FrameTime(_tape.FrameCount - 1) * 1000000f).ToString("F0")
				});
			}
			else
			{
				_banner.text = "";
			}
		}
		ClearLegend();
		Readout();
	}

	private void ShowLibrary(bool on)
	{
		if (_libraryPanel == null)
		{
			return;
		}
		_libraryPanel.gameObject.SetActive(on);
		Disarm();
		_rows.Clear();
		_libSig = int.MinValue;
		if (!on)
		{
			return;
		}
		Transform transform = _libraryPanel.transform.Find("List/Viewport/Content");
		if (transform == null)
		{
			return;
		}
		for (int num = transform.childCount - 1; num >= 0; num--)
		{
			Kill(transform.GetChild(num).gameObject);
		}
		MenuTheme.SetSelectedVisual(_tabGallery, _showGallery);
		MenuTheme.SetSelectedVisual(_tabMine, !_showGallery);
		if (_showGallery)
		{
			FillGallery(transform);
		}
		else
		{
			FillMine(transform);
		}
		TickLibrary();
	}

	private static void Kill(UnityEngine.Object o)
	{
		if (o == null)
		{
			return;
		}
		if (Application.isPlaying)
		{
			UnityEngine.Object.Destroy(o);
		}
		else
		{
			UnityEngine.Object.DestroyImmediate(o);
		}
	}

	private void SelectTab(bool gallery)
	{
		if (_showGallery != gallery)
		{
			_showGallery = gallery;
			RefreshTexts();
		}
	}

	private void FillGallery(Transform content)
	{
		int itemsPresent = 0;
		int armsPresent = 0;
		int armsTotal = 0;
		List<string> missingFiles = null;
		List<SuperGallery.Resolved> scan = SuperGallery.Scan();
		SuperGallery.Census(scan, out itemsPresent, out armsPresent, out armsTotal, out missingFiles);
		if (_compactAll != null)
		{
			_compactAll.gameObject.SetActive(false);
		}
		IReadOnlyList<string> roots = SuperTapeFiles.GalleryRoots();
		_libWhere.text = Localization.T("super.gallery.census", new object[2] { armsPresent, armsTotal }) + "   ·   " + Localization.T("super.gallery.roots", new object[1]
		{
			((roots.Count != 0) ? string.Join(" ; ", roots) : "—")
		});
		for (int i = 0; i < scan.Count; i++)
		{
			SuperGallery.Resolved resolved = scan[i];
			string text = Localization.T("super.gallery." + resolved.item.id + ".title") + " — " + Localization.T("super.gallery." + resolved.item.id + ".what");
			if (resolved.item.caveat != null)
			{
				text += "  " + Localization.T(resolved.item.caveat);
			}
			MenuTheme.MakeLabelRaw(content, "Head_" + resolved.item.id, text, 17f, FontStyles.Normal, resolved.AnyPresent ? MenuTheme.TextMuted : MenuTheme.TextLocked, TextAlignmentOptions.Left);
			foreach (SuperGallery.ResolvedArm arm in resolved.arms)
			{
				Button button = MenuTheme.MakeButtonRaw(content, "Gal_" + arm.arm.file, SuperGallery.ArmCaption(resolved, arm), 44f, arm.Present ? new UnityAction(delegate
				{
					OpenTape(arm.path);
				}) : null, 16f, TextAlignmentOptions.Left, 16f);
				if (!arm.Present)
				{
					MenuTheme.SetLocked(button, locked: true, null);
				}
			}
		}
		if (itemsPresent == 0)
		{
			MenuTheme.MakeLabelRaw(content, "Empty", Localization.T("super.viewer.no_tapes"), 20f, FontStyles.Normal, MenuTheme.TextMuted, TextAlignmentOptions.Left);
		}
	}

	private void FillMine(Transform content)
	{
		_libWhere.text = Localization.T("super.library.where", new object[1] { ((_mineDir != null) ? _mineDir : SuperTapeRecorder.TapeDir) });
		List<SuperTapeLibrary.Entry> list = SuperTapeLibrary.Scan(_mineDir);
		UpdateCompactAll(list);
		if (list.Count != 0)
		{
			foreach (SuperTapeLibrary.Entry item in list)
			{
				MakeMineRow(content, item);
			}
			return;
		}
		MenuTheme.MakeLabelRaw(content, "Empty", Localization.T("super.viewer.no_tapes"), 20f, FontStyles.Normal, MenuTheme.TextMuted, TextAlignmentOptions.Left);
	}

	private void UpdateCompactAll(List<SuperTapeLibrary.Entry> entries)
	{
		long bytes = 0L;
		_compactCandidates.Clear();
		foreach (SuperTapeLibrary.Entry entry in entries)
		{
			if (entry.version == 5 && !entry.unreadable && !entry.active)
			{
				_compactCandidates.Add(entry.path);
				bytes += entry.bytes;
			}
		}
		if (_compactAll == null)
		{
			return;
		}
		_compactAll.gameObject.SetActive(value: true);
		TextMeshProUGUI label = MenuTheme.LabelOf(_compactAll);
		if (label != null)
		{
			if (_compactCandidates.Count > 0)
			{
				label.text = Localization.T("super.tape.compact.all", new object[2]
				{
					_compactCandidates.Count,
					(bytes / 1073741824.0).ToString("F1")
				});
			}
			else
			{
				label.text = Localization.T("super.tape.compact.all_none");
			}
		}
		MenuTheme.SetLocked(_compactAll, _compactCandidates.Count == 0, null);
	}

	private void OnCompactAllClicked()
	{
		if (_compactCandidates.Count == 0)
		{
			Refuse(Localization.T("super.tape.compact.all_none"));
			return;
		}
		StartCompaction(new List<string>(_compactCandidates));
	}

	private void OnCompactClicked(SuperTapeLibrary.Entry e)
	{
		if (SuperTapeFiles.IsActiveRunTape(e.path))
		{
			Refuse(Localization.T("super.tape.compact.refuse_active", new object[1] { Path.GetFileName(e.path) }));
			return;
		}
		StartCompaction(new List<string> { e.path });
	}

	public bool StartCompaction(List<string> paths)
	{
		if (_compactJob != null && !Volatile.Read(ref _compactJob.done))
		{
			Refuse(Localization.T("super.tape.compact.busy"));
			return false;
		}
		if (_opening)
		{
			return false;
		}
		foreach (string path in paths)
		{
			if (_tape != null && SamePath(_tapePath, path))
			{
				CloseTape();
			}
		}
		LastCompactResults = new SuperTapeCompact.Result[0];
		_compactJob = new SuperTapeCompact.Job(paths);
		_compactTickAt = -1f;
		Say(Localization.T("super.tape.compact.progress", new object[5]
		{
			1,
			paths.Count,
			Path.GetFileName(paths[0]),
			0,
			"…"
		}));
		return true;
	}

	private void TickCompaction()
	{
		SuperTapeCompact.Job job = _compactJob;
		if (job == null)
		{
			return;
		}
		if (Volatile.Read(ref job.done))
		{
			_compactJob = null;
			FinishCompaction(job.Snapshot());
			return;
		}
		if (_compactTickAt > Time.unscaledTime)
		{
			return;
		}
		_compactTickAt = Time.unscaledTime + 0.25f;
		string current = job.Current;
		int frames = Volatile.Read(ref job.frames);
		Say(Localization.T("super.tape.compact.progress", new object[5]
		{
			job.tapeIndex + 1,
			job.Count,
			((current != null) ? Path.GetFileName(current) : ""),
			Volatile.Read(ref job.frame),
			(frames > 0) ? frames.ToString() : "…"
		}));
	}

	public void FinishCompaction(SuperTapeCompact.Result[] rs)
	{
		LastCompactResults = rs;
		long bytesBefore = 0L;
		long bytesAfter = 0L;
		int okCount = 0;
		SuperTapeCompact.Result firstBad = null;
		for (int i = 0; i < rs.Length; i++)
		{
			SuperTapeCompact.Result result = rs[i];
			if (result.ok)
			{
				bytesBefore += result.bytesBefore;
				okCount++;
				bytesAfter += result.bytesAfter;
			}
			else if (firstBad == null)
			{
				firstBad = result;
			}
		}
		if (firstBad != null)
		{
			string text = Localization.T(firstBad.refusalKey, firstBad.args);
			if (firstBad.refusalKey == "super.tape.compact.refuse_active" || firstBad.refusalKey == "super.tape.compact.refuse_not_v5" || firstBad.refusalKey == "super.tape.compact.refuse_checkpoint" || firstBad.refusalKey == "super.tape.compact.refuse_space" || firstBad.refusalKey == "super.tape.compact.cancelled")
			{
				Refuse(text);
			}
			else
			{
				Fail(text);
			}
		}
		else if (rs.Length == 1)
		{
			Say(Localization.T("super.tape.compact.done", new object[4]
			{
				Path.GetFileName(rs[0].path),
				(rs[0].bytesBefore / 1048576.0).ToString("F0"),
				(rs[0].bytesAfter / 1048576.0).ToString("F0"),
				((double)rs[0].bytesBefore / Math.Max(1L, rs[0].bytesAfter)).ToString("F1")
			}));
		}
		else
		{
			Say(Localization.T("super.tape.compact.done_all", new object[4]
			{
				okCount,
				rs.Length,
				(bytesBefore / 1048576.0).ToString("F0"),
				(bytesAfter / 1048576.0).ToString("F0")
			}));
		}
		if (_libraryPanel != null && _libraryPanel.gameObject.activeSelf)
		{
			ShowLibrary(on: true);
		}
	}

	private void MakeMineRow(Transform content, SuperTapeLibrary.Entry entry)
	{
		RectTransform row = MenuTheme.MakeRect(content, "Tape_" + entry.name);
		MenuTheme.AddImage(row, MenuTheme.SurfaceAlt);
		LayoutElement layoutElement = row.gameObject.AddComponent<LayoutElement>();
		layoutElement.minHeight = MineRowHeight;
		layoutElement.preferredHeight = MineRowHeight;
		bool flag = entry.version == 5 && !entry.unreadable;
		float num = 432f + ((flag) ? 180f : 0f);
		string text = (string.IsNullOrEmpty(entry.scene)) ? Localization.T("super.library.unknown_scene") : SuperTapeLibrary.DisplayScene(entry.scene);
		TextMeshProUGUI title = MenuTheme.MakeLabelRaw(row, "Title", text, 17f, FontStyles.Bold, entry.unreadable ? MenuTheme.TextLocked : MenuTheme.TextPrimary, TextAlignmentOptions.Left);
		PlaceRowLine(title, num, -8f, -32f);
		TextMeshProUGUI meta = MenuTheme.MakeLabelRaw(row, "Meta", SuperTapeLibrary.CaptionMeta(entry, withName: true), 14f, FontStyles.Normal, MenuTheme.TextMuted, TextAlignmentOptions.Left);
		PlaceRowLine(meta, num, -34f, -56f);
		Button open = MenuTheme.MakeButtonRaw(row, "Open", Localization.T("super.viewer.open"), 44f, entry.unreadable ? null : new UnityAction(delegate
		{
			OpenTape(entry.path);
		}), 16f, TextAlignmentOptions.Center, 16f);
		MenuTheme.Place((RectTransform)open.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-266f, 0f), new Vector2(150f, 44f));
		open.navigation = new Navigation();
		if (entry.unreadable)
		{
			MenuTheme.SetLocked(open, locked: true, null);
		}
		if (flag)
		{
			Button compact = MenuTheme.MakeButtonRaw(row, "Compact", Localization.T("super.tape.compact.button"), 44f, new UnityAction(delegate
			{
				OnCompactClicked(entry);
			}), 16f, TextAlignmentOptions.Center, 16f);
			MenuTheme.Place((RectTransform)compact.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-426f, 0f), new Vector2(170f, 44f));
			compact.navigation = new Navigation();
		}
		Button del = null;
		del = MenuTheme.MakeButtonRaw(row, "Delete", Localization.T("super.library.delete"), 44f, new UnityAction(delegate
		{
			OnDeleteClicked(entry, del);
		}), 16f, TextAlignmentOptions.Center, 16f);
		MenuTheme.Place((RectTransform)del.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(240f, 44f));
		del.navigation = new Navigation();
		_rows.Add(new MineRow
		{
			rect = row,
			entry = entry
		});
	}

	private static void PlaceRowLine(TextMeshProUGUI t, float rightInset, float top, float bottom)
	{
		RectTransform rectTransform = t.rectTransform;
		rectTransform.anchorMin = new Vector2(0f, 1f);
		rectTransform.anchorMax = new Vector2(1f, 1f);
		rectTransform.pivot = new Vector2(0f, 1f);
		rectTransform.offsetMin = new Vector2(16f, bottom);
		rectTransform.offsetMax = new Vector2(-rightInset, top);
		t.textWrappingMode = TextWrappingModes.NoWrap;
		t.overflowMode = TextOverflowModes.Ellipsis;
	}

	private void OnDeleteClicked(SuperTapeLibrary.Entry e, Button btn)
	{
		if (_opening)
		{
			return;
		}
		if (SuperTapeFiles.IsActiveRunTape(e.path))
		{
			Disarm();
			Refuse(RefusalText("super.library.refuse_active", e.path));
			return;
		}
		if (_armedPath == e.path && Time.unscaledTime <= _armedUntil)
		{
			Disarm();
			DeleteTapeNow(e.path);
			return;
		}
		Disarm();
		_armedPath = e.path;
		_armedName = e.name;
		_armedBtn = btn;
		_armedUntil = Time.unscaledTime + ConfirmWindowS;
		TextMeshProUGUI textMeshProUGUI = ((btn != null) ? MenuTheme.LabelOf(btn) : null);
		if (textMeshProUGUI != null)
		{
			textMeshProUGUI.text = Localization.T("super.library.confirm_delete");
		}
		if (btn != null)
		{
			MenuTheme.SetSelectedVisual(btn, selected: true);
		}
		_libSig = int.MinValue;
	}

	private void Disarm()
	{
		if (_armedBtn != null)
		{
			TextMeshProUGUI label = MenuTheme.LabelOf(_armedBtn);
			if (label != null)
			{
				label.text = Localization.T("super.library.delete");
			}
			MenuTheme.SetSelectedVisual(_armedBtn, selected: false);
		}
		_armedPath = null;
		_armedName = null;
		_armedBtn = null;
		_libSig = int.MinValue;
	}

	public bool DeleteTapeNow(string path)
	{
		string text = null;
		bool flag = false;
		try
		{
			if (_tape != null && SamePath(_tapePath, path))
			{
				CloseTape();
			}
			bool result = SuperTapeFiles.TryDelete(path, out text, out flag);
			string fileName = Path.GetFileName(path);
			if (result)
			{
				if (flag)
				{
					Say(Localization.T("super.library.deleted_export_left", new object[2]
					{
						fileName,
						SuperTapeFiles.ExportDirFor(SuperTapeRecorder.TapeDir, path)
					}));
				}
				else
				{
					Say(Localization.T("super.library.deleted", new object[1] { fileName }));
				}
			}
			else
			{
				string text2 = RefusalText(text, path);
				if (text == "super.library.refuse_active")
				{
					Refuse(text2);
				}
				else
				{
					Fail(text2);
				}
			}
			if (_libraryPanel != null && _libraryPanel.gameObject.activeSelf)
			{
				ShowLibrary(on: true);
			}
			return result;
		}
		catch (Exception ex)
		{
			Debug.LogException(ex);
			Fail(RefusalText("super.library.refuse_io", path));
			return false;
		}
	}

	public static string RefusalText(string refusalKey, string tapePath)
	{
		return Localization.T(refusalKey, new object[1] { ((tapePath != null) ? Path.GetFileName(tapePath) : null) });
	}

	private static bool SamePath(string a, string b)
	{
		if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
		{
			return false;
		}
		return string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
	}

	private void LibMessage(string text, Color color)
	{
		_libMsg = text ?? "";
		_libMsgColor = color;
		_libMsgUntil = Time.unscaledTime + StatusHoldS;
		_libSig = int.MinValue;
	}

	private void TickLibrary()
	{
		if (_libraryPanel == null || _libStatus == null)
		{
			return;
		}
		if (!_libraryPanel.gameObject.activeSelf)
		{
			return;
		}
		if (_armedPath != null && Time.unscaledTime > _armedUntil)
		{
			Disarm();
		}
		bool flag = _libMsgUntil > Time.unscaledTime;
		int num = (_armedPath == null && !flag && !_showGallery) ? HoveredRow() : (-1);
		int num2;
		if (_armedPath != null)
		{
			num2 = 1;
		}
		else if (flag)
		{
			num2 = 2;
		}
		else if (num >= 0)
		{
			num2 = num + 3;
		}
		else
		{
			num2 = ((_showGallery) ? (-1) : 0);
		}
		if (num2 == _libSig)
		{
			return;
		}
		_libSig = num2;
		if (_armedPath != null)
		{
			_libStatus.color = MenuTheme.TextPrimary;
			_libStatus.text = Localization.T("super.library.confirm_hint", new object[3]
			{
				_armedName,
				Localization.T("super.library.confirm_delete"),
				ConfirmWindowS.ToString("F0")
			});
			return;
		}
		if (flag)
		{
			_libStatus.color = _libMsgColor;
			_libStatus.text = _libMsg;
			return;
		}
		_libStatus.color = MenuTheme.TextMuted;
		string text;
		if (_showGallery)
		{
			text = "";
		}
		else
		{
			SuperTapeLibrary.Entry entry = null;
			string scene = null;
			if (num >= 0)
			{
				MineRow mineRow = _rows[num];
				entry = mineRow.entry;
				if (entry != null)
				{
					scene = entry.scene;
				}
			}
			if (entry != null && entry.unreadable && !string.IsNullOrEmpty(entry.note))
			{
				text = Localization.T("super.viewer.unreadable_tag") + ": " + entry.note;
			}
			else if (string.IsNullOrEmpty(scene))
			{
				text = Localization.T("super.library.hover_hint");
			}
			else
			{
				text = Localization.T("super.library.raw_header", new object[1] { FontSubstitutions.Apply(scene) });
			}
		}
		_libStatus.text = text;
	}

	private int HoveredRow()
	{
		if (_rows == null || _rows.Count == 0 || _libViewport == null)
		{
			return -1;
		}
		Vector2 mousePosition = TouchInputHelper.PrimaryPosition;
		if (!RectTransformUtility.RectangleContainsScreenPoint(_libViewport, mousePosition, null))
		{
			return -1;
		}
		for (int i = 0; i < _rows.Count; i++)
		{
			if (_rows[i].rect != null && RectTransformUtility.RectangleContainsScreenPoint(_rows[i].rect, mousePosition, null))
			{
				return i;
			}
		}
		return -1;
	}

	private void Fail(string text)
	{
		Debug.LogError("[Super] " + text);
		Post(text, MenuTheme.Failure);
	}

	private void ApplyFraming(int w, int h)
	{
		_render.centre = new Vector3(0f, 0f, _fitCentreZ);
		SuperViewState.Framing framing = _view.Frame(_fitHalfY, _fitHalfZ, Mathf.Max((float)w / (float)h, 0.2f));
		_render.yawDeg = _view.yawDeg;
		_render.pitchDeg = _view.pitchDeg;
		_render.depthHalf = framing.depthHalf;
		_render.capSideMin = framing.capSideMin;
		_render.orthoSize = framing.orthoSize;
	}

	private void BuildUI()
	{
		GameObject gameObject = new GameObject("SuperViewerCanvas", new Type[3]
		{
			typeof(Canvas),
			typeof(CanvasScaler),
			typeof(GraphicRaycaster)
		});
		gameObject.transform.SetParent(base.transform, worldPositionStays: false);
		_canvas = gameObject.GetComponent<Canvas>();
		_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		_canvas.sortingOrder = 200;
		CanvasScaler obj = gameObject.GetComponent<CanvasScaler>();
		obj.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		obj.referenceResolution = new Vector2(1920f, 1080f);
		obj.matchWidthOrHeight = 0.5f;
		RectTransform root = MenuTheme.MakeRect(gameObject.transform, "Root");
		MenuTheme.Stretch(root);
		MenuTheme.AddImage(root, MenuTheme.Background);
		RectTransform pic = MenuTheme.MakeRect(root, "Picture");
		MenuTheme.Stretch(pic, new Vector2(0f, BarHeight), Vector2.zero);
		_picture = pic.gameObject.AddComponent<RawImage>();
		_picture.color = MenuTheme.Background;
		_picture.raycastTarget = false;
		BuildLegend(pic);
		_banner = MenuTheme.MakeLabelRaw(pic, "Banner", "", 22f, FontStyles.Bold, InkColor, TextAlignmentOptions.BottomLeft);
		MenuTheme.Place(_banner.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 12f), new Vector2(1400f, 30f));
		MakeHint(pic, "HintZoom", "super.viewer.zoom_hint", 48f);
		MakeHint(pic, "HintRotate", "super.viewer.rotate_hint", 76f);
		_marker = MenuTheme.MakeLabelRaw(pic, "ViewMarker", "", 20.3f, FontStyles.Normal, InkColor, TextAlignmentOptions.TopLeft);
		MenuTheme.Place(_marker.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -MarkerY), new Vector2(1600f, 26f));
		RectTransform bar = MenuTheme.MakeRect(root, "Bar");
		bar.anchorMin = Vector2.zero;
		bar.anchorMax = new Vector2(1f, 0f);
		bar.pivot = new Vector2(0.5f, 0f);
		bar.offsetMin = Vector2.zero;
		bar.offsetMax = new Vector2(0f, BarHeight);
		MenuTheme.AddImage(bar, MenuTheme.Surface);
		WheelZones.Register(bar);
		_readout = MenuTheme.MakeLabelRaw(bar, "Readout", "", 20f, FontStyles.Normal, MenuTheme.TextMuted, TextAlignmentOptions.Left);
		MenuTheme.Place(_readout.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -14f), new Vector2(1500f, 26f));
		BuildLibrary(root);
		GameObject gameObject2 = new GameObject("Frames", typeof(Image), typeof(Slider));
		gameObject2.transform.SetParent(bar, worldPositionStays: false);
		Image obj2 = gameObject2.GetComponent<Image>();
		obj2.color = MenuTheme.SurfaceAlt;
		obj2.raycastTarget = true;
		RectTransform r = (RectTransform)gameObject2.transform;
		r.anchorMin = new Vector2(0f, 1f);
		r.anchorMax = new Vector2(1f, 1f);
		r.pivot = new Vector2(0.5f, 1f);
		r.offsetMin = new Vector2(28f, -78f);
		r.offsetMax = new Vector2(-28f, -52f);
		RectTransform fillArea = MenuTheme.MakeRect(r, "FillArea");
		MenuTheme.Stretch(fillArea);
		RectTransform fill = MenuTheme.MakeRect(fillArea, "Fill");
		MenuTheme.Stretch(fill);
		MenuTheme.AddImage(fill, MenuTheme.TextMuted);
		RectTransform handleArea = MenuTheme.MakeRect(r, "HandleArea");
		MenuTheme.Stretch(handleArea);
		RectTransform handle = MenuTheme.MakeRect(handleArea, "Handle");
		handle.sizeDelta = new Vector2(18f, 0f);
		Image targetGraphic = MenuTheme.AddImage(handle, MenuTheme.TextPrimary);
		targetGraphic.raycastTarget = true;
		_frames = gameObject2.GetComponent<Slider>();
		_frames.fillRect = fill;
		_frames.handleRect = handle;
		_frames.targetGraphic = targetGraphic;
		_frames.direction = Slider.Direction.LeftToRight;
		_frames.minValue = 0f;
		_frames.maxValue = 1f;
		_frames.wholeNumbers = true;
		_frames.onValueChanged.AddListener(delegate(float v)
		{
			_frame = Mathf.RoundToInt(v);
			_playing = false;
		});
		_frames.navigation = new Navigation();
		float num = 28f;
		num = BtnKey(bar, "super.viewer.play_pause", num, 180f, delegate
		{
			_playing = !_playing;
		});
		num = BtnKey(bar, "super.viewer.to_start", num, 140f, delegate
		{
			_frame = 0;
			_playing = false;
		});
		num = Btn(bar, "FrameBack", "−1", num, 70f, delegate
		{
			_playing = false;
			_frame = Mathf.Max(0, _frame - 1);
		}, out _, 20f);
		num = Btn(bar, "FrameFwd", "+1", num, 70f, delegate
		{
			_playing = false;
			_frame = ((_tape != null) ? Mathf.Min(_frame + 1, _tape.FrameCount - 1) : _frame);
		}, out _, 20f);
		num = BtnKey(bar, "super.viewer.speed", num, 175f, delegate
		{
			_speed = ((_speed > 0.5f) ? 0.1f : 1f);
		});
		num = Btn(bar, "ZoomIn", "+", num, 60f, delegate
		{
			_view.Zoom(-1);
		}, out _, 20f);
		num = Btn(bar, "ZoomOut", "−", num, 60f, delegate
		{
			_view.Zoom(1);
		}, out _, 20f);
		num = Btn(bar, "Field", FieldCaption(), num, 330f, delegate
		{
			if (_tape != null)
			{
				_field = (_field + 1) % _tape.header.fieldNames.Length;
				if (_fieldLabel != null)
				{
					_fieldLabel.text = FieldCaption();
				}
			}
		}, out _fieldLabel, 20f);
		num = BtnKey(bar, "super.viewer.cut", num, 120f, delegate
		{
			_cut = !_cut;
		});
		num = BtnKey(bar, "super.viewer.field_on_body", num, 200f, delegate
		{
			_surface = !_surface;
		});
		num = BtnKey(bar, "super.viewer.cut_azimuth", num, 170f, delegate
		{
			_view.StepAzimuth();
		});
		if (num > 1920f)
		{
			Debug.LogError(string.Format("[Super] Ряд кнопок показа занял {0:F0} из {1:F0} единиц канвы — ", num, 1920f) + "последние кнопки за краем экрана. Сузь подписи или заведи вторую строку.");
		}
		num = BtnKey(bar, "super.viewer.reset_view", num, 200f, ResetView, 66f);
		num = BtnKey(bar, "super.viewer.library", num, 200f, delegate
		{
			ShowLibrary(_libraryPanel == null || !_libraryPanel.gameObject.activeSelf);
		}, 66f);
		num = BtnKey(bar, "super.viewer.save_video", num, 230f, SaveVideo, 66f);
		num = Btn(bar, "Decor", DecorCaption(), num, 300f, ToggleDecor, out _decorLabel, 66f);
		num = BtnKey(bar, "super.viewer.exit", num, 180f, Exit, 66f);
		if (num > 1920f)
		{
			Debug.LogError(string.Format("[Super] Второй ряд кнопок показа занял {0:F0} из {1:F0} единиц канвы.", num, 1920f));
		}
		Localization.OnLanguageChanged -= RefreshTexts;
		Localization.OnLanguageChanged += RefreshTexts;
	}

	private void RefreshTexts()
	{
		if (this == null)
		{
			Localization.OnLanguageChanged -= RefreshTexts;
			return;
		}
		foreach (KeyedLabel item in _keyed)
		{
			if (item.label != null)
			{
				item.label.text = Localization.T(item.key);
			}
		}
		if (_fieldLabel != null)
		{
			_fieldLabel.text = FieldCaption();
		}
		UpdateDecorLabel();
		_statusShown = false;
		_libMsgUntil = -1f;
		UpdateBanner();
		Readout();
		UpdateLegend();
		if (_libraryPanel != null && _libraryPanel.gameObject.activeSelf)
		{
			ShowLibrary(on: true);
		}
	}

	private void BuildLibrary(RectTransform root)
	{
		_libraryPanel = MenuTheme.MakeRect(root, "Library");
		MenuTheme.Stretch(_libraryPanel, new Vector2(-120f, -40f), new Vector2(120f, 236f));
		MenuTheme.AddImage(_libraryPanel, MenuTheme.Surface);
		WheelZones.Register(_libraryPanel);
		TextMeshProUGUI title = MenuTheme.MakeLabelRaw(_libraryPanel, "Title", Localization.T("super.viewer.library_title"), 26f, FontStyles.Bold, MenuTheme.TextPrimary, TextAlignmentOptions.Left);
		MenuTheme.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -16f), new Vector2(420f, 24f));
		_keyed.Add(new KeyedLabel
		{
			label = title,
			key = "super.viewer.library_title"
		});
		_tabGallery = MenuTheme.MakeButtonRaw(_libraryPanel, "TabGallery", Localization.T("super.gallery.tab"), 40f, new UnityAction(delegate
		{
			if (!_showGallery)
			{
				_showGallery = true;
				ShowLibrary(on: true);
			}
		}), 18f, TextAlignmentOptions.Center, 18f);
		MenuTheme.Place((RectTransform)_tabGallery.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(200f, -14f), new Vector2(200f, 40f));
		_tabMine = MenuTheme.MakeButtonRaw(_libraryPanel, "TabMine", Localization.T("super.gallery.tab_mine"), 40f, new UnityAction(delegate
		{
			if (_showGallery)
			{
				_showGallery = false;
				ShowLibrary(on: true);
			}
		}), 18f, TextAlignmentOptions.Center, 18f);
		MenuTheme.Place((RectTransform)_tabMine.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(686f, -14f), new Vector2(240f, 40f));
		_keyed.Add(new KeyedLabel
		{
			label = MenuTheme.LabelOf(_tabGallery),
			key = "super.gallery.tab"
		});
		_keyed.Add(new KeyedLabel
		{
			label = MenuTheme.LabelOf(_tabMine),
			key = "super.gallery.tab_mine"
		});
		_libWhere = MenuTheme.MakeLabelRaw(_libraryPanel, "Where", "", 14f, FontStyles.Normal, MenuTheme.TextMuted, TextAlignmentOptions.Left);
		MenuTheme.Place(_libWhere.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -52f), new Vector2(1400f, 22f));
		Button close = MenuTheme.MakeButtonRaw(_libraryPanel, "Close", Localization.T("super.viewer.close"), 40f, new UnityAction(delegate
		{
			ShowLibrary(on: false);
		}), 18f, TextAlignmentOptions.Center, 18f);
		MenuTheme.Place((RectTransform)close.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(180f, -24f), new Vector2(180f, 40f));
		_keyed.Add(new KeyedLabel
		{
			label = MenuTheme.LabelOf(close),
			key = "super.viewer.close"
		});
		_libStatus = MenuTheme.MakeLabelRaw(_libraryPanel, "Status", "", 14f, FontStyles.Normal, MenuTheme.TextMuted, TextAlignmentOptions.TopLeft);
		MenuTheme.Place(_libStatus.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(38f, -78f), new Vector2(1630f, 24f));
		_libStatus.textWrappingMode = TextWrappingModes.NoWrap;
		_libStatus.overflowMode = TextOverflowModes.Ellipsis;
		_compactAll = MenuTheme.MakeButtonRaw(_libraryPanel, "CompactAll", "", 40f, new UnityAction(OnCompactAllClicked), 18f, TextAlignmentOptions.Center, 18f);		MenuTheme.Place((RectTransform)_compactAll.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(946f, -14f), new Vector2(740f, 40f));
		_compactAll.navigation = new Navigation();
		_compactAll.gameObject.SetActive(false);
		ScrollRect scrollRect = MenuTheme.MakeScrollList(_libraryPanel, "List", out var content);
		MenuTheme.Stretch((RectTransform)scrollRect.transform, new Vector2(24f, -124f), new Vector2(-24f, -24f));
		_libViewport = scrollRect.viewport;
		_libraryPanel.gameObject.SetActive(false);
	}

	private static Color BandDisplay(float t)
	{
		Color c = SuperTapeRender.Rainbow(t);
		if (QualitySettings.activeColorSpace == ColorSpace.Linear)
		{
			return new Color(Mathf.LinearToGammaSpace(c.r), Mathf.LinearToGammaSpace(c.g), Mathf.LinearToGammaSpace(c.b), c.a);
		}
		return c;
	}

	private static float FontFor(int glyphScale)
	{
		return (float)glyphScale * 7f * 1.45f;
	}

	private void MakeHint(RectTransform pic, string name, string key, float bottom)
	{
		TextMeshProUGUI label = MenuTheme.MakeLabelRaw(pic, name, Localization.T(key), 20.3f, FontStyles.Normal, InkColor, TextAlignmentOptions.BottomLeft);
		MenuTheme.Place(label.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, bottom), new Vector2(1500f, 26f));
		label.raycastTarget = false;
		label.textWrappingMode = TextWrappingModes.NoWrap;
		label.enableAutoSizing = true;
		label.fontSizeMin = 13f;
		label.fontSizeMax = 20.3f;
		_keyed.Add(new KeyedLabel
		{
			label = label,
			key = key
		});
	}

	private void BuildLegend(RectTransform pic)
	{
		RectTransform legend = MenuTheme.MakeRect(pic, "Legend");
		legend.anchorMin = new Vector2(1f, 0f);
		legend.anchorMax = new Vector2(1f, 1f);
		legend.pivot = new Vector2(1f, 0.5f);
		legend.offsetMin = new Vector2(-185f, 130f);
		legend.offsetMax = new Vector2(-151f, -110f);
		_bands = new Image[9];
		for (int i = 0; i < 9; i++)
		{
			RectTransform rect = MenuTheme.MakeRect(legend, "Band" + i);
			rect.anchorMin = new Vector2(0f, (float)i / 9f);
			rect.anchorMax = new Vector2(1f, (float)(i + 1) / 9f);
			rect.offsetMin = Vector2.zero;
			rect.offsetMax = Vector2.zero;
			_bands[i] = MenuTheme.AddImage(rect, BandDisplay(i * 0.125f));
		}
		_ticks = new TextMeshProUGUI[10];
		for (int j = 0; j < 10; j++)
		{
			TextMeshProUGUI textMeshProUGUI = MenuTheme.MakeLabelRaw(legend, "Tick" + j, "", 20.3f, FontStyles.Normal, InkColor, TextAlignmentOptions.Left);
			_ticks[j] = textMeshProUGUI;
			MenuTheme.Place(textMeshProUGUI.rectTransform, new Vector2(1f, (float)j / 9f), new Vector2(0f, 0.5f), new Vector2(44f, 0f), new Vector2(160f, 24f));
		}
		_legendTitle = MenuTheme.MakeLabelRaw(legend, "Title", "", 20.3f, FontStyles.Normal, InkColor, TextAlignmentOptions.Left);
		MenuTheme.Place(_legendTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(-6f, 26f), new Vector2(340f, 26f));
		_legendDead = MenuTheme.MakeLabelRaw(legend, "Dead", "", 20.3f, FontStyles.Normal, SuperLegend.DeadInk, TextAlignmentOptions.Left);
		MenuTheme.Place(_legendDead.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(-60f, 64f), new Vector2(340f, 26f));
		_stamp = MenuTheme.MakeLabelRaw(legend, "Stamp", "", 30.45f, FontStyles.Normal, InkColor, TextAlignmentOptions.TopLeft);
		MenuTheme.Place(_stamp.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -22f), new Vector2(700f, 36f));
		_counter = MenuTheme.MakeLabelRaw(legend, "Counter", "", 20.3f, FontStyles.Normal, InkColor, TextAlignmentOptions.TopLeft);
		MenuTheme.Place(_counter.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -52f), new Vector2(700f, 26f));
	}

	private void UpdateLegend()
	{
		if (_ticks == null || _cur == null || _tape == null)
		{
			return;
		}
		SuperTape.Header header = _tape.header;
		float num = _cur.fieldMin[_field];
		float num2 = _cur.fieldMax[_field];
		for (int i = 0; i < _ticks.Length; i++)
		{
			_ticks[i].text = SuperLegend.Fmt((float)i / 9f * (num2 - num) + num);
		}
		if (_legendTitle != null)
		{
			_legendTitle.text = SuperTapeLibrary.DisplayFieldName(header.fieldNames[_field]) + " [" + SuperTapeLibrary.DisplayUnit(header.fieldUnits[_field]) + "]";
			Vector2 pref = _legendTitle.GetPreferredValues(_legendTitle.text, 0f, 0f);
			float num3 = Mathf.Max(0f, pref.x - 179f);
			_legendTitle.rectTransform.anchoredPosition = new Vector2(-6f - num3, 26f);
		}
		if (_legendDead != null && _cur.fieldMax != null && _field < _cur.fieldMax.Length)
		{
			_legendDead.text = ((_cur.fieldMin[_field] > _cur.fieldMax[_field]) ? "min = max" : "");
		}
		if (_cur != null)
		{
			_stamp.text = Localization.T("super.viewer.stamp", new object[1] { SuperLegend.StampValue(_cur.simTimeS) });
		}
		int frameCount = _tape.FrameCount;
		_counter.text = Localization.T("super.viewer.counter", new object[2]
		{
			_frame,
			frameCount - 1
		});
		UpdateMarker();
	}

	private void UpdateMarker()
	{
		if (_marker == null)
		{
			return;
		}
		string text;
		if (_view.IsCanonical)
		{
			text = "";
		}
		else
		{
			text = Localization.T("super.viewer.view_marker", new object[4]
			{
				_view.yawDeg,
				_view.pitchDeg,
				_view.azimuthDeg,
				_view.Magnification
			});
		}
		_marker.text = text;
	}

	public static string StampText(float simTimeS)
	{
		return Localization.T("super.viewer.stamp", new object[1] { SuperLegend.StampValue(simTimeS) });
	}

	public static string CounterText(int frameIdx, int frameCount)
	{
		return Localization.T("super.viewer.counter", new object[2] { frameIdx, frameCount - 1 });
	}

	private void PlaceLegendTitle()
	{
		if (_legendTitle == null)
		{
			return;
		}
		Vector2 pref = _legendTitle.GetPreferredValues(_legendTitle.text, 0f, 0f);
		float num = Mathf.Max(0f, pref.x - 179f);
		_legendTitle.rectTransform.anchoredPosition = new Vector2(-6f - num, 26f);
	}

	private void ClearLegend()
	{
		if (_ticks == null)
		{
			return;
		}
		TextMeshProUGUI[] array = _ticks;
		foreach (TextMeshProUGUI textMeshProUGUI in array)
		{
			if (textMeshProUGUI != null)
			{
				textMeshProUGUI.text = "";
			}
		}
		if (_legendTitle != null)
		{
			_legendTitle.text = "";
		}
		if (_legendDead != null)
		{
			_legendDead.text = "";
		}
		if (_stamp != null)
		{
			_stamp.text = "";
		}
		if (_counter != null)
		{
			_counter.text = "";
		}
		if (_marker != null)
		{
			_marker.text = "";
		}
	}

	private string FieldCaption()
	{
		if (_tape == null)
		{
			return Localization.T("super.viewer.field");
		}
		return Localization.T("super.viewer.field_caption", new object[2]
		{
			SuperTapeLibrary.DisplayFieldName(_tape.header.fieldNames[_field]),
			SuperTapeLibrary.DisplayUnit(_tape.header.fieldUnits[_field])
		});
	}

	private float BtnKey(RectTransform parent, string key, float x, float w, UnityAction act, float y = 20f)
	{
		float num = Btn(parent, key, Localization.T(key), x, w, act, out var label, y);
		_keyed.Add(new KeyedLabel
		{
			label = label,
			key = key
		});
		return num;
	}

	private float Btn(RectTransform parent, string id, string caption, float x, float w, UnityAction act, out TextMeshProUGUI label, float y)
	{
		RectTransform rect = MenuTheme.MakeRect(parent, "Btn_" + id);
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.zero;
		rect.pivot = Vector2.zero;
		rect.anchoredPosition = new Vector2(x, y);
		rect.sizeDelta = new Vector2(w, 40f);
		Image image = MenuTheme.AddImage(rect, MenuTheme.SurfaceAlt);
		image.raycastTarget = true;
		Button button = rect.gameObject.AddComponent<Button>();
		button.targetGraphic = image;
		MenuTheme.ApplyColors(button);
		button.onClick.AddListener(act);
		button.navigation = new Navigation();
		label = MenuTheme.MakeLabelRaw(rect, "T", caption, 18f, FontStyles.Normal, MenuTheme.TextPrimary, TextAlignmentOptions.Center);
		MenuTheme.Stretch(label.rectTransform, new Vector2(6f, 0f), new Vector2(-6f, 0f));
		label.enableAutoSizing = true;
		label.fontSizeMin = 11f;
		label.fontSizeMax = 18f;
		label.textWrappingMode = TextWrappingModes.NoWrap;
		return x + w + 10f;
	}
}
