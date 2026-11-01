using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class Flat2DLayoutAudit
{
	public struct Finding
	{
		public string control;

		public string kind;

		public string detail;

		public override string ToString()
		{
			return "[" + kind + "] " + control + ": " + detail;
		}
	}

	public const float MinW = 20f;

	public const float MinH = 14f;

	private const float Eps = 1.5f;

	public static string Path(Transform t)
	{
		string text = t.name;
		for (Transform parent = t.parent; parent != null; parent = parent.parent)
		{
			text = parent.name + "/" + text;
		}
		return text;
	}

	public static Rect ScreenRect(RectTransform rt, Camera cam)
	{
		Vector3[] array = new Vector3[4];
		rt.GetWorldCorners(array);
		if (cam == null)
		{
			return new Rect(array[0].x, array[0].y, array[2].x - array[0].x, array[2].y - array[0].y);
		}
		Vector3 vector = cam.WorldToScreenPoint(array[0]);
		Vector3 vector2 = cam.WorldToScreenPoint(array[2]);
		return new Rect(Mathf.Min(vector.x, vector2.x), Mathf.Min(vector.y, vector2.y), Mathf.Abs(vector2.x - vector.x), Mathf.Abs(vector2.y - vector.y));
	}

	public static void EnsureVisible(RectTransform rt)
	{
		ScrollRect scrollRect = rt.GetComponentInParent<ScrollRect>();
		if (scrollRect == null)
		{
			return;
		}
		if (scrollRect.viewport == null || scrollRect.content == null)
		{
			return;
		}
		Canvas.ForceUpdateCanvases();
		Rect rect = scrollRect.viewport.rect;
		Rect rect2 = scrollRect.content.rect;
		if (rect.height <= rect2.height + 1f)
		{
			return;
		}
		Vector3 position = rt.position;
		Vector3 vector = scrollRect.viewport.InverseTransformPoint(position);
		float num = (0f - vector.y - rect2.height * 0.5f) / (rect.height - rect2.height);
		if (num < 0f)
		{
			num = 0f;
		}
		else if (num > 1f)
		{
			num = 1f;
		}
		scrollRect.verticalNormalizedPosition = 1f - num;
		Canvas.ForceUpdateCanvases();
	}

	private static bool Contains(Rect outer, Rect inner)
	{
		return outer.x - Eps <= inner.x && outer.x + outer.width + Eps >= inner.x + inner.width && outer.y - Eps <= inner.y && outer.y + outer.height + Eps >= inner.y + inner.height;
	}

	public static List<Finding> Run(Canvas canvas, Rect surface, Transform only = null)
	{
		List<Finding> list = new List<Finding>();
		if (canvas == null)
		{
			Finding item = new Finding
			{
				control = "(канвас)",
				kind = "НЕТ КАНВАСА",
				detail = "null"
			};
			list.Add(item);
			return list;
		}
		Canvas.ForceUpdateCanvases();
		Camera worldCamera = canvas.worldCamera;
		Rect screen = ScreenRect(canvas.transform as RectTransform, worldCamera);
		GraphicRaycaster graphicRaycaster = canvas.GetComponent<GraphicRaycaster>();
		Transform transform = ((only != null) ? only : canvas.transform);
		ScrollRect[] array = canvas.GetComponentsInChildren<ScrollRect>(true);
		float[] array2 = new float[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array2[i] = array[i].verticalNormalizedPosition;
		}
		Selectable[] array3 = transform.GetComponentsInChildren<Selectable>(true);
		Selectable[] array4 = array3;
		foreach (Selectable selectable in array4)
		{
			RectTransform rectTransform = selectable.transform as RectTransform;
			string path = Path(selectable.transform);
			if (!selectable.gameObject.activeInHierarchy)
			{
				continue;
			}
			if (!selectable.interactable)
			{
				continue;
			}
			EnsureVisible(rectTransform);
			Rect rect = ScreenRect(rectTransform, worldCamera);
			if (!(selectable is Scrollbar) && (rect.width < MinW || rect.height < MinH))
			{
				list.Add(new Finding
				{
					control = path,
					kind = "СЛИШКОМ МАЛ",
					detail = string.Format("{0:F0}×{1:F0} px при минимуме {2:F0}×{3:F0}", rect.width, rect.height, MinW, MinH)
				});
			}
			if (!Contains(screen, rect))
			{
				list.Add(new Finding
				{
					control = path,
					kind = "ВНЕ ЭКРАНА",
					detail = string.Format("прямоугольник {0} не помещается в канвас {1}", rect, screen)
				});
			}
			Transform parent = rectTransform.parent;
			while (parent != null)
			{
				RectMask2D rectMask2D = parent.GetComponent<RectMask2D>();
				if (rectMask2D != null && rectMask2D.enabled)
				{
					Rect rect2 = ScreenRect(parent as RectTransform, worldCamera);
					if (!Contains(rect2, rect) && !rect2.Overlaps(rect))
					{
						list.Add(new Finding
						{
							control = path,
							kind = "ОБРЕЗАН МАСКОЙ",
							detail = "целиком вне " + Path(parent) + " — без прокрутки недостижим"
						});
					}
				}
				parent = parent.parent;
			}
			GraphicRaycaster graphicRaycaster2 = selectable.GetComponentInParent<GraphicRaycaster>();
			if (graphicRaycaster2 == null)
			{
				graphicRaycaster2 = graphicRaycaster;
			}
			if (graphicRaycaster2 != null && rect.width > 1f && rect.height > 1f)
			{
				PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
				pointerEventData.position = new Vector2(rect.x + rect.width * 0.5f, rect.y + rect.height * 0.5f);
				List<RaycastResult> list2 = new List<RaycastResult>();
				graphicRaycaster2.Raycast(pointerEventData, list2);
				bool flag = false;
				if (list2.Count > 0)
				{
					Transform transform2 = list2[0].gameObject.transform;
					while (transform2 != null)
					{
						if (transform2 == selectable.transform)
						{
							flag = true;
							break;
						}
						transform2 = transform2.parent;
					}
				}
				if (!flag)
				{
					list.Add(new Finding
					{
						control = path,
						kind = "ЛУЧ НЕ ДОХОДИТ",
						detail = ((list2.Count > 0) ? ("сверху лежит " + Path(list2[0].gameObject.transform)) : "в центр органа не попадает ничего")
					});
				}
			}
			if (surface.width > 1f && surface.Contains(new Vector2(rect.x + rect.width * 0.5f, rect.y + rect.height * 0.5f)) && !path.EndsWith("/Surface"))
			{
				list.Add(new Finding
				{
					control = path,
					kind = "ПОВЕРХ ХОЛСТА",
					detail = "центр внутри прямоугольника холста — частицы рисуются ПОСЛЕ канваса и закроют его"
				});
			}
		}
		TMP_Text[] array5 = transform.GetComponentsInChildren<TMP_Text>(true);
		TMP_Text[] array6 = array5;
		foreach (TMP_Text tmpText in array6)
		{
			if (!tmpText.gameObject.activeInHierarchy || string.IsNullOrEmpty(tmpText.text))
			{
				continue;
			}
			Rect rect3 = ScreenRect(tmpText.rectTransform, worldCamera);
			string path2 = Path(tmpText.transform);
			tmpText.ForceMeshUpdate();
			Vector2 rendered = tmpText.GetRenderedValues(false);
			float num2 = canvas.scaleFactor;
			if (rendered.x * num2 > rect3.width + Eps || rendered.y * num2 > rect3.height + Eps)
			{
				list.Add(new Finding
				{
					control = path2,
					kind = "ТЕКСТ ВЫЛЕЗАЕТ",
					detail = string.Format("нарисовано {0:F0}×{1:F0} px в прямоугольнике {2:F0}×{3:F0} px: ", rendered.x * num2, rendered.y * num2, rect3.width, rect3.height) + "«" + Short(tmpText.text) + "»"
				});
			}
			int num3 = ((TMP_Settings.missingGlyphCharacter != 0) ? TMP_Settings.missingGlyphCharacter : 9633);
			TMP_TextInfo textInfo = tmpText.textInfo;
			for (int j = 0; j < textInfo.characterCount; j++)
			{
				if (textInfo.characterInfo[j].character == num3 && tmpText.text.IndexOf((char)num3) < 0)
				{
					list.Add(new Finding
					{
						control = path2,
						kind = "НЕТ ГЛИФА",
						detail = string.Format("символ на позиции {0} шрифт не содержит — на экране ", j) + string.Format("нарисован «{0}» в «{1}»", (char)num3, Short(tmpText.text))
					});
				}
			}
			if (!tmpText.enableWordWrapping && !tmpText.enableAutoSizing && tmpText.overflowMode == TextOverflowModes.Overflow)
			{
				list.Add(new Finding
				{
					control = path2,
					kind = "РЕЖИМ OVERFLOW",
					detail = "ни переноса, ни автоподбора, ни обрезки — вылезти ничто не мешает"
				});
			}
		}
		for (int k = 0; k < array.Length; k++)
		{
			array[k].verticalNormalizedPosition = array2[k];
		}
		Canvas.ForceUpdateCanvases();
		return list;
	}

	public static bool CanBeHit(Canvas canvas, Selectable sel, out string why)
	{
		why = null;
		if (canvas == null || sel == null)
		{
			why = "нет канваса или органа";
			return false;
		}
		GameObject gameObject = sel.gameObject;
		if (!gameObject.activeInHierarchy)
		{
			why = "выключен вместе с родителем";
			return false;
		}
		if (!sel.interactable)
		{
			why = "недоступен (interactable = false)";
			return false;
		}
		Canvas.ForceUpdateCanvases();
		EnsureVisible(sel.transform as RectTransform);
		Camera worldCamera = canvas.worldCamera;
		RectTransform rectTransform = sel.transform as RectTransform;
		Rect rect = ScreenRect(rectTransform, worldCamera);
		if (!(sel is Scrollbar) && (rect.width < MinW || rect.height < MinH))
		{
			why = string.Format("слишком мал: {0:F0}×{1:F0} px", rect.width, rect.height);
			return false;
		}
		Rect screen = ScreenRect(canvas.transform as RectTransform, worldCamera);
		if (!Contains(screen, rect))
		{
			why = string.Format("вне экрана: {0}", screen);
			return false;
		}
		GraphicRaycaster graphicRaycaster = sel.GetComponentInParent<GraphicRaycaster>();
		if (graphicRaycaster == null)
		{
			graphicRaycaster = canvas.GetComponent<GraphicRaycaster>();
		}
		if (graphicRaycaster == null)
		{
			why = "у канваса нет GraphicRaycaster — мышь не дойдёт вовсе";
			return false;
		}
		PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
		pointerEventData.position = new Vector2(rect.x + rect.width * 0.5f, rect.y + rect.height * 0.5f);
		List<RaycastResult> list = new List<RaycastResult>();
		graphicRaycaster.Raycast(pointerEventData, list);
		if (list.Count == 0)
		{
			bool flag = RectTransformUtility.RectangleContainsScreenPoint(rectTransform, new Vector2(rect.x + rect.width * 0.5f, rect.y + rect.height * 0.5f), worldCamera);
			why = string.Format("луч в центр не попадает ни во что: центр {0}, орган {1}, ", new Vector2(rect.x + rect.width * 0.5f, rect.y + rect.height * 0.5f), rect) + string.Format("канвас {0}, камера {1}, ", ScreenRect(canvas.transform as RectTransform, worldCamera), (worldCamera == null) ? "нет" : worldCamera.pixelRect.ToString()) + string.Format("точка внутри прямоугольника органа: {0}", flag);
			return false;
		}
		Transform transform = list[0].gameObject.transform;
		while (transform != null)
		{
			if (transform == sel.transform)
			{
				return true;
			}
			transform = transform.parent;
		}
		why = "сверху лежит " + Path(list[0].gameObject.transform);
		return false;
	}

	private static string Short(string s)
	{
		if (s == null)
		{
			return "";
		}
		if (s.Length <= 42)
		{
			return s.Replace("\n", " ⏎ ");
		}
		return s.Substring(0, 42).Replace("\n", " ⏎ ") + "…";
	}

	public static string Report(List<Finding> f)
	{
		if (f.Count == 0)
		{
			return "   раскладка: нарушений нет";
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (Finding item in f)
		{
			stringBuilder.AppendLine("   ⛔ " + item.ToString());
		}
		return stringBuilder.ToString().TrimEnd();
	}
}
