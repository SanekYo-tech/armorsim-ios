using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class MusicPlayer : MonoBehaviour
{
	public const string ResourceFolder = "Audio/Music";

	private MusicProgram _program;

	private float[] _percents;

	private AudioSource _source;

	private float _waited;

	private float _playing;

	private bool _started;

	private bool _heard;

	private bool _muted;

	private AudioListener _ownListener;

	private int _playingIndex = -1;

	public int PlayingIndex => _playingIndex;

	public MusicProgram Program => _program;

	public static MusicPlayer EnsureIn(Transform parent, MusicProgram program)
	{
		MusicPlayer musicPlayer = Object.FindFirstObjectByType<MusicPlayer>();
		if (musicPlayer != null)
		{
			if (musicPlayer._program.Name == program.Name)
			{
				return musicPlayer;
			}
			musicPlayer.StopNow();
			Object.Destroy(musicPlayer.gameObject);
		}
		GameObject gameObject = new GameObject("MusicPlayer");
		if (parent != null)
		{
			gameObject.transform.SetParent(parent, false);
		}
		MusicPlayer musicPlayer2 = gameObject.AddComponent<MusicPlayer>();
		musicPlayer2.Apply(program);
		return musicPlayer2;
	}

	private void Apply(MusicProgram program)
	{
		_program = program;
		_percents = MusicPolicy.Percents(program.Tracks);
		if (program.SurvivesSceneChange)
		{
			base.transform.SetParent(null, false);
			Object.DontDestroyOnLoad(base.gameObject);
			SceneManager.sceneLoaded += OnSceneLoaded;
		}
		foreach (string item in MusicPolicy.Validate(program.Tracks))
		{
			Debug.LogError("[Music] таблица «" + program.Name + "»: " + item);
		}
		_waited = 0f;
		_started = false;
		_source.volume = program.Plan.Volume;
		EnsureListener();
	}

	private void Awake()
	{
		_source = GetComponent<AudioSource>();
		if (_source == null)
		{
			_source = base.gameObject.AddComponent<AudioSource>();
		}
		_source.playOnAwake = false;
		_source.loop = false;
		_source.spatialBlend = 0f;
		_source.priority = 0;
	}

	private void OnDestroy()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (_program.KeepsPlaying(scene.name, AppFlow.MenuSceneName))
		{
			EnsureListener();
		}
		else
		{
			StopNow();
			Object.Destroy(base.gameObject);
		}
	}

	private void EnsureListener()
	{
		AudioListener audioListener = Object.FindFirstObjectByType<AudioListener>();
		if (audioListener != null && audioListener != _ownListener)
		{
			if (_ownListener != null)
			{
				Object.Destroy(_ownListener);
				_ownListener = null;
			}
			return;
		}
		if (_ownListener != null)
		{
			return;
		}
		_ownListener = base.gameObject.AddComponent<AudioListener>();
		Debug.LogWarning("[Music] В сцене «" + SceneManager.GetActiveScene().name + "» нет AudioListener — звук был бы не слышен вовсе. Завёл свой на проигрывателе; правильное место для него — камера сцены (см. UltraSceneBuilder).");
	}

	public void StopNow()
	{
		if (_source != null)
		{
			_source.Stop();
		}
	}

	private void Update()
	{
		if (GameSettings.Music)
		{
			if (_muted)
			{
				_muted = false;
				_source.UnPause();
			}
			float unscaledDeltaTime = Time.unscaledDeltaTime;
			if (!_started)
			{
				_waited += unscaledDeltaTime;
				if (_program.Plan.DelaySeconds > _waited)
				{
					return;
				}
				_started = true;
				PlayNext();
			}
			else
			{
				_playing += unscaledDeltaTime;
				_source.volume = MusicPolicy.VolumeAt(_program.Plan, _playing);
				if (_source.isPlaying)
				{
					_heard = true;
				}
				else if (_heard)
				{
					PlayNext();
				}
			}
		}
		else if (!_muted)
		{
			_muted = true;
			_source.Pause();
		}
	}

	private void PlayNext()
	{
		AudioClip audioClip = LoadRandomClip(out var index);
		if (audioClip == null)
		{
			base.enabled = false;
			string[] array;
			if (_program.Tracks != null)
			{
				array = new string[_program.Tracks.Length];
				for (int i = 0; i < _program.Tracks.Length; i++)
				{
					array[i] = _program.Tracks[i].Name;
				}
			}
			else
			{
				array = new string[0];
			}
			Debug.LogError("[Music] Программа «" + _program.Name + "»: ни одного трека не нашлось в Assets/Resources/Audio/Music — музыки не будет. Ожидались файлы: " + string.Join(", ", array));
			return;
		}
		_playingIndex = index;
		_heard = false;
		_source.clip = audioClip;
		_source.Play();
	}

	private AudioClip LoadRandomClip(out int index)
	{
		index = -1;
		if (_program.Tracks == null || _program.Tracks.Length == 0)
		{
			return null;
		}
		int num = MusicPolicy.Pick(_percents, Random.value);
		if (num < 0)
		{
			return null;
		}
		for (int i = 0; i < _program.Tracks.Length; i++)
		{
			int num2 = (num + i) % _program.Tracks.Length;
			string text = _program.Tracks[num2].Name;
			AudioClip audioClip = Resources.Load<AudioClip>("Audio/Music/" + text);
			if (audioClip != null)
			{
				index = num2;
				return audioClip;
			}
			Debug.LogError("[Music] Трек не найден: Assets/Resources/Audio/Music/" + text + ".mp3");
		}
		return null;
	}
}
