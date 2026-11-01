using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public static class SuperFireButton
{
	private sealed class Runner : MonoBehaviour
	{
		[CompilerGenerated]
		private sealed class _003CLoop_003Ed__2 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			private int _003Ci_003E5__2;

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
			public _003CLoop_003Ed__2(int _003C_003E1__state)
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
					if (AppFlow.ActiveMode != SimMode.Super)
					{
						return false;
					}
					_003Ci_003E5__2 = 0;
					break;
				case 1:
					_003C_003E1__state = -1;
					_003Ci_003E5__2++;
					break;
				}
				if (_003Ci_003E5__2 >= Attempts)
				{
					if (UnityEngine.Object.FindFirstObjectByType<Projectile>() != null)
					{
						Debug.LogError("[Super] Кнопка выстрела в сцене не найдена за " + Attempts + " кадров — расчёт в режиме Super подключить не к чему. Ищется объект с именем «FireButton» и компонентом Button.");
					}
					return false;
				}
				if (SuperFireButton.Apply())
				{
					return false;
				}
				_003C_003E2__current = null;
				_003C_003E1__state = 1;
				return true;
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

		private const int Attempts = 30;

		public void Schedule()
		{
			StopAllCoroutines();
			StartCoroutine(Loop());
		}

		[IteratorStateMachine(typeof(_003CLoop_003Ed__2))]
		private IEnumerator Loop()
		{
			return new _003CLoop_003Ed__2(0);
		}
	}

	public const string LabelKey = "sandbox.solve_super";

	private const string FireKey = "sandbox.fire";

	private static bool _hooked;

	private static Runner _runner;

	private const string ButtonObjectName = "FireButton";

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Install()
	{
		if (_hooked)
		{
			return;
		}
		_hooked = true;
		GameObject gameObject = new GameObject("SuperFireButtonHook");
		gameObject.hideFlags = (HideFlags)61;
		UnityEngine.Object.DontDestroyOnLoad(gameObject);
		_runner = gameObject.AddComponent<Runner>();
		SceneManager.sceneLoaded += delegate(Scene scene, LoadSceneMode mode)
		{
			_runner.Schedule();
		};
		_runner.Schedule();
	}

	public static bool Apply()
	{
		if (AppFlow.ActiveMode != SimMode.Super)
		{
			return true;
		}
		List<Button> list = FireButtonCaption.Find();
		if (list.Count == 0)
		{
			return false;
		}
		foreach (Button btn in list)
		{
			btn.onClick = new Button.ButtonClickedEvent();
			btn.onClick.AddListener(OnPressed);
		}
		FireButtonCaption.Apply(LabelKey);
		foreach (Button btn2 in list)
		{
			Debug.Log("[Super] Кнопка «" + btn2.name + "» переведена в режим расчёта.");
		}
		return true;
	}

	private static void OnPressed()
	{
		Projectile proj = UnityEngine.Object.FindFirstObjectByType<Projectile>();
		if (proj == null)
		{
			Debug.LogError("[Super] В сцене нет снаряда — считать нечего.");
		}
		else
		{
			SuperShotRunner.TryBegin(proj);
		}
	}
}
