using System;
using Object = UnityEngine.Object;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class Flat2DRuntimeView : MonoBehaviour
{
	public enum Style
	{
		Izmerenie = 0,
		Dots = 1,
		Surface = 2
	}

	public Style RenderStyle;

	private const float DiscHalfK = 0.8f;

	private const float SplatHalfK = 0.8f;

	private const float BlurPxPerDx = 0.35f;

	private const float SurfThresh = 0.35f;

	private const float SurfBand = 0.1f;

	private const float DepthK = 0.5f;

	private static readonly Color ShadowCol = new Color(0.3f, 0.33f, 0.38f, 0.55f);

	private static readonly Color EdgeCol = new Color(0.1f, 0.11f, 0.13f, 1f);

	public Flat2DSolver Solver;

	public int FieldMode;

	public Rect ViewportPx;

	public Vector2 Lo;

	public Vector2 Hi;

	public float Dx = 0.0005f;

	public int BodiesEverySteps = 25;

	public bool ShowGrid = true;

	public bool ShowDomain;

	public Vector2 DomainLo;

	public Vector2 DomainHi;

	public Vector2[] PinnedPreview;

	private const float MinGridPx = 7f;

	private const int MaxGridLines = 400;

	private const int MaxPinnedDrawn = 4000;

	private Material mat;

	private ComputeBuffer stPos;

	private ComputeBuffer stVel;

	private ComputeBuffer stStress;

	private ComputeBuffer stDamage;

	private ComputeBuffer stFail;

	private ComputeBuffer stMat;

	private ComputeBuffer stBody;

	private int stCount;

	private ComputeBuffer liveBody;

	private int liveBodyAtStep = -1;

	private int stepCounter;

	private readonly List<Vector3[]> outlines = new List<Vector3[]>();

	private readonly List<Color> outlineColors = new List<Color>();

	private readonly List<bool> outlineOpen = new List<bool>();

	private readonly List<Vector3[]> cursorLines = new List<Vector3[]>();

	private readonly List<Color> cursorColors = new List<Color>();

	private readonly List<bool> cursorOpen = new List<bool>();

	private Vector2[] cursorSites;

	private Color cursorSiteColor;

	private const int MaxCursorSites = 4000;

	private const int GlowLutStride = 16;

	private const int GlowLutFloats = 4;

	public const float GlowTminDisplayK = 300f;

	public bool Glow = true;

	public float Exposure = 1f;

	public Flat2DMaterialBlock[] MaterialBlocks;

	public ComputeBuffer TemperatureOverride;

	private double plannedDuration;

	private double coolC;

	private float[] tDisp;

	private float[] tPrev;

	private float[] tRoomOf;

	private float[] tNowScratch;

	private ComputeBuffer bTempDisp;

	private ComputeBuffer bGlowLut;

	private ComputeBuffer bGlowWMeltInv;

	private ComputeBuffer stTemp;

	private int glowLutN;

	private float glowLutTmin;

	private float glowLutTstep;

	private float glowTmeltMax;

	private bool glowSeeded;

	private double lastSimTime;

	private bool haveSimTime;

	private static readonly Vector2[] FieldRanges = new Vector2[7]
	{
		new Vector2(0f, 1f),
		new Vector2(-2E+09f, 8E+09f),
		new Vector2(0f, 1f),
		new Vector2(0f, 800f),
		new Vector2(0f, 1f),
		new Vector2(0f, 1f),
		new Vector2(300f, 300f)
	};

	private Material surfMat;

	private ComputeBuffer stPaint;

	private ComputeBuffer livePaint;

	private ComputeBuffer dummyPaint;

	private bool stPaintOn;

	private bool livePaintOn;

	private static Material lineMaterial;

	public int GridStepInDx { get; private set; } = 1;

	public int PinnedDrawn { get; private set; }

	public int LastBodyCount { get; private set; } = -1;

	public int CursorSitesDrawn { get; private set; }

	public int CursorSitesGiven { get; private set; }

	private static double PalHaloK => 2.0;

	private static double PalLutStepK => 10.0;

	private static double PalLutTminK => 600.0;

	private static double PalTauFadeShare => 0.5;

	private static double PalExposure => 1.0;

	public int DisplayUpdates { get; private set; }

	public string GlowRefusal { get; private set; }

	public double PlannedDurationSeconds
	{
		get
		{
			return plannedDuration;
		}
		set
		{
			double num = ((value > 0.0) ? value : 0.0);
			plannedDuration = num;
			if (num * PalTauFadeShare > 0.0)
			{
				coolC = PalCoolingConstant(num * PalTauFadeShare);
			}
		}
	}

	public double TauFadeSeconds => plannedDuration * PalTauFadeShare;

	public double CoolingConstant => coolC;

	public bool GlowApplies
	{
		get
		{
			if (!Glow || RenderStyle == Style.Izmerenie || Solver == null)
			{
				return false;
			}
			return Mathf.Clamp(FieldMode, 0, 6) != 6;
		}
	}

	public bool GlowSuppressedByField => Mathf.Clamp(FieldMode, 0, 6) == 6;

	public bool PaintShown => (Solver != null) ? livePaintOn : stPaintOn;

	public static int GridStepFor(float pxPerDx)
	{
		int num = 1;
		while (MinGridPx > (float)num * pxPerDx && num < MaxGridLines)
		{
			num *= 10;
		}
		return num;
	}

	public void SetCursor(List<Vector3[]> lines, List<Color> colors, List<bool> open, Vector2[] sites, Color siteColor)
	{
		cursorLines.Clear();
		cursorColors.Clear();
		cursorOpen.Clear();
		if (lines != null)
		{
			cursorLines.AddRange(lines);
		}
		if (colors != null)
		{
			cursorColors.AddRange(colors);
		}
		if (open != null)
		{
			cursorOpen.AddRange(open);
		}
		cursorSites = sites;
		cursorSiteColor = siteColor;
		CursorSitesGiven = ((sites != null) ? sites.Length : 0);
	}

	public void ClearCursor()
	{
		cursorLines.Clear();
		cursorColors.Clear();
		cursorOpen.Clear();
		cursorSites = null;
		CursorSitesDrawn = 0;
	}

	private static double PalCoolingConstant(double tauFade)
	{
		if (tauFade <= 0.0)
		{
			throw new ArgumentOutOfRangeException("tauFade", "τ_fade должно быть > 0");
		}
		return (Math.Pow(798.0, -3.0) - Math.Pow(1500.0, -3.0)) / (tauFade * 3.0);
	}

	private static double PalCool(double T, double t, double c)
	{
		return Flat2DGlowPalette.Cool(T, t, c);
	}

	private static float[] PalBuildLut(double tMin, double tMax)
	{
		return Flat2DGlowPalette.BuildLut(tMin, tMax, PalLutStepK, Flat2DGlowPalette.GamutMode.DesaturateToWhite);
	}

	public void ResetGlow()
	{
		tNowScratch = null;
		tRoomOf = null;
		tPrev = null;
		tDisp = null;
		DisplayUpdates = 0;
		glowSeeded = false;
		haveSimTime = false;
		lastSimTime = 0.0;
		if (stTemp != null)
		{
			stTemp.Release();
		}
		stTemp = null;
		if (bGlowLut != null)
		{
			bGlowLut.Release();
		}
		bGlowLut = null;
		if (bGlowWMeltInv != null)
		{
			bGlowWMeltInv.Release();
		}
		bGlowWMeltInv = null;
		GlowRefusal = null;
	}

	private float TMeltMax()
	{
		if (MaterialBlocks == null || MaterialBlocks.Length == 0)
		{
			return 0f;
		}
		float num = 0f;
		for (int i = 0; i < MaterialBlocks.Length; i++)
		{
			if (MaterialBlocks[i].tMelt >= num)
			{
				num = MaterialBlocks[i].tMelt;
			}
		}
		return num;
	}

	private bool EnsureGlowLut()
	{
		float num = TMeltMax();
		if (PalLutTminK >= (double)num)
		{
			GlowRefusal = ((num > 0f) ? string.Format("свечение: T_melt сцены {0:F0} K ниже низа палитры {1:F0} K", num, PalLutTminK) : "свечение: блоки веществ сцены не переданы виду — шкала неизвестна");
			return false;
		}
		if (bGlowLut != null && glowLutN > 0 && Mathf.Approximately(glowTmeltMax, num))
		{
			GlowRefusal = null;
			return true;
		}
		if (bGlowLut != null)
		{
			bGlowLut.Release();
		}
		float[] array = PalBuildLut(PalLutTminK, (double)num);
		if (array == null || array.Length < GlowLutFloats)
		{
			bGlowLut = null;
			glowLutN = 0;
			GlowRefusal = "свечение: палитра вернула пустой ППТ";
			return false;
		}
		glowLutN = array.Length / GlowLutFloats;
		glowTmeltMax = num;
		glowLutTmin = (float)PalLutTminK;
		glowLutTstep = (float)PalLutStepK;
		bGlowLut = new ComputeBuffer(glowLutN, GlowLutStride);
		bGlowLut.SetData(array);
		BuildMeltAnchors(num);
		GlowRefusal = null;
		return true;
	}

	private void BuildMeltAnchors(float tMeltMax)
	{
		int num = ((MaterialBlocks != null) ? MaterialBlocks.Length : 0);
		float[] array = new float[Math.Max(num, 1)];
		for (int i = 0; i < num; i++)
		{
			double num2 = Flat2DGlowPalette.GlowWeight(MaterialBlocks[i].tMelt, tMeltMax);
			array[i] = ((num2 > 0.0) ? ((float)(1.0 / num2)) : 0f);
		}
		if (bGlowWMeltInv == null || bGlowWMeltInv.count != array.Length)
		{
			if (bGlowWMeltInv != null)
			{
				bGlowWMeltInv.Release();
			}
			bGlowWMeltInv = new ComputeBuffer(array.Length, 4);
		}
		bGlowWMeltInv.SetData(array);
	}

	private void UpdateGlowDisplay(int n)
	{
		if (tDisp == null || tDisp.Length != n)
		{
			tDisp = new float[n];
			tPrev = new float[n];
			tNowScratch = new float[n];
			float[] array = new float[n];
			float num = GlowTminDisplayK;
			if (MaterialBlocks != null && MaterialBlocks.Length != 0)
			{
				num = MaterialBlocks[0].tRoom;
			}
			for (int i = 0; i < n; i++)
			{
				array[i] = num;
			}
			if (Solver != null && MaterialBlocks != null && MaterialBlocks.Length >= 2)
			{
				uint[] array2 = Solver.ReadMaterialIds();
				if (array2 != null && array2.Length == n)
				{
					for (int j = 0; j < n; j++)
					{
						uint num2 = array2[j];
						if (num2 < MaterialBlocks.Length)
						{
							array[j] = MaterialBlocks[num2].tRoom;
						}
					}
				}
			}
			tRoomOf = array;
			glowSeeded = false;
			haveSimTime = false;
		}
		bool flag = false;
		double simTime = 0.0;
		if (Solver != null)
		{
			Solver.SimState(out var _, out simTime, out var _, out var _);
			flag = true;
		}
		if (glowSeeded && haveSimTime && flag && simTime == lastSimTime && TemperatureOverride == null)
		{
			return;
		}
		float[] array3 = tNowScratch;
		if (TemperatureOverride == null)
		{
			if (Solver == null || Solver.TemperatureBuffer == null || Solver.TemperatureBuffer.count < n)
			{
				return;
			}
			Solver.TemperatureBuffer.GetData(array3, 0, 0, n);
		}
		else
		{
			if (TemperatureOverride.count < n)
			{
				return;
			}
			TemperatureOverride.GetData(array3);
		}
		if (array3 == null)
		{
			return;
		}
		double num3 = 0.0;
		if (flag)
		{
			double num4 = haveSimTime ? (simTime - lastSimTime) : simTime;
			if (num4 < 0.0)
			{
				num4 = 0.0;
			}
			num3 = num4;
			haveSimTime = true;
			lastSimTime = simTime;
		}
		if (!glowSeeded)
		{
			for (int k = 0; k < n; k++)
			{
				tDisp[k] = array3[k];
				tPrev[k] = array3[k];
			}
			glowSeeded = true;
		}
		else
		{
			for (int l = 0; l < n; l++)
			{
				float num5 = ((tRoomOf != null) ? tRoomOf[l] : GlowTminDisplayK);
				tDisp[l] = (float)Flat2DGlowPalette.AdvanceDisplayTemperature(tDisp[l], array3[l], tPrev[l], num3, coolC, num5);
				tPrev[l] = array3[l];
			}
		}
		if (stTemp == null || stTemp.count != n)
		{
			if (stTemp != null)
			{
				stTemp.Release();
			}
			stTemp = new ComputeBuffer(n, 4);
		}
		stTemp.SetData(tDisp);
		DisplayUpdates++;
	}

	private float[] ReadTemperatureForDisplay(int n)
	{
		if (TemperatureOverride == null)
		{
			if (Solver == null || Solver.TemperatureBuffer == null || Solver.TemperatureBuffer.count < n)
			{
				return null;
			}
			Solver.TemperatureBuffer.GetData(tNowScratch, 0, 0, n);
			return tNowScratch;
		}
		if (TemperatureOverride.count < n)
		{
			return null;
		}
		TemperatureOverride.GetData(tNowScratch);
		return tNowScratch;
	}

	private float[] BuildRoomFloor(int n)
	{
		float[] array = new float[n];
		float num = GlowTminDisplayK;
		if (MaterialBlocks != null && MaterialBlocks.Length != 0)
		{
			num = MaterialBlocks[0].tRoom;
		}
		for (int i = 0; i < n; i++)
		{
			array[i] = num;
		}
		if (Solver != null && MaterialBlocks != null && MaterialBlocks.Length >= 2)
		{
			uint[] array2 = Solver.ReadMaterialIds();
			if (array2 != null && array2.Length == n)
			{
				for (int j = 0; j < n; j++)
				{
					uint num2 = array2[j];
					if (num2 < MaterialBlocks.Length)
					{
						array[j] = MaterialBlocks[num2].tRoom;
					}
				}
			}
		}
		return array;
	}

	private Material Mat()
	{
		if (mat != null)
		{
			return mat;
		}
		Shader shader = Shader.Find("Flat2D/Particles");
		if (shader == null)
		{
			Debug.LogError("Flat2DRuntimeView: шейдер не найден");
			return null;
		}
		mat = new Material(shader);
		mat.hideFlags = HideFlags.HideAndDontSave;
		return mat;
	}

	private Material SurfMat()
	{
		if (surfMat != null)
		{
			return surfMat;
		}
		Shader shader = Shader.Find("Flat2D/SurfaceScreen");
		if (shader == null)
		{
			Debug.LogError("Flat2DRuntimeView: шейдер Flat2D/SurfaceScreen не найден");
			return null;
		}
		surfMat = new Material(shader);
		surfMat.hideFlags = HideFlags.HideAndDontSave;
		return surfMat;
	}

	private void DrawSurfaceStyle(Material m, int verts, bool clipped)
	{
		Material material = SurfMat();
		if (material == null)
		{
			if (m != null)
			{
				m.SetPass(0);
				Graphics.DrawProceduralNow(MeshTopology.Triangles, verts, 1);
			}
			return;
		}
		int num = Math.Max((int)ViewportPx.width, 8);
		int num2 = Math.Max((int)ViewportPx.height, 8);
		RenderTextureFormat format = (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf) ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.RGFloat);
		RenderTexture renderTexture = RenderTexture.GetTemporary(num, num2, 0, format);
		RenderTexture renderTexture2 = RenderTexture.GetTemporary(num, num2, 0, format);
		renderTexture.wrapMode = TextureWrapMode.Clamp;
		renderTexture2.wrapMode = TextureWrapMode.Clamp;
		RenderTexture active = RenderTexture.active;
		RenderTexture.active = renderTexture;
		GL.Clear(false, true, new Color(0f, 0f, 0f, 0f));
		m.SetFloat("_DiscHalf", Dx * DiscHalfK);
		m.SetPass(2);
		Graphics.DrawProceduralNow(MeshTopology.Triangles, verts, 1);
		float num3 = Hi.x - Lo.x;
		num3 = ((num3 > 1e-09f) ? num3 : 1e-09f);
		float num4 = ViewportPx.width * Dx / num3;
		float num5 = Math.Max(num4 * BlurPxPerDx, 0.5f);
		material.SetFloat("_BlurPx", num5);
		Graphics.Blit(renderTexture, renderTexture2, material, 0);
		Graphics.Blit(renderTexture2, renderTexture, material, 1);
		RenderTexture.active = active;
		if (clipped)
		{
			GL.Viewport(ViewportPx);
		}
		material.SetTexture("_MainTex", renderTexture);
		material.SetFloat("_Thresh", SurfThresh);
		material.SetFloat("_Band", SurfBand);
		material.SetVector("_ShadowPx", new Vector4(num4 * 0.5f, -num4 * 0.5f, 0f, 0f));
		material.SetColor("_ShadowCol", ShadowCol);
		material.SetColor("_EdgeCol", EdgeCol);
		material.SetPass(2);
		GL.PushMatrix();
		GL.LoadOrtho();
		GL.Begin(GL.QUADS);
		GL.TexCoord2(0f, 0f);
		GL.Vertex3(0f, 0f, 0.1f);
		GL.TexCoord2(1f, 0f);
		GL.Vertex3(1f, 0f, 0.1f);
		GL.TexCoord2(1f, 1f);
		GL.Vertex3(1f, 1f, 0.1f);
		GL.TexCoord2(0f, 1f);
		GL.Vertex3(0f, 1f, 0.1f);
		GL.End();
		GL.PopMatrix();
		RenderTexture.ReleaseTemporary(renderTexture);
		RenderTexture.ReleaseTemporary(renderTexture2);
	}

	public void SetStatic(Vector2[] pos, uint[] matIdx)
	{
		SetStatic(pos, matIdx, pos.Length);
	}

	public void SetStaticPaint(uint[] paint, int count)
	{
		stPaintOn = paint != null && count > 0;
		if (!stPaintOn)
		{
			return;
		}
		if (stPaint == null || stPaint.count < count)
		{
			if (stPaint != null)
			{
				stPaint.Release();
			}
			stPaint = new ComputeBuffer(Math.Max((int)Math.Ceiling((double)count * 1.5), count), 4);
		}
		stPaint.SetData(paint, 0, 0, count);
	}

	public void SetLivePaint(uint[] paint)
	{
		if (livePaint != null)
		{
			livePaint.Release();
		}
		livePaint = null;
		livePaintOn = paint != null && paint.Length > 0;
		if (livePaintOn)
		{
			livePaint = new ComputeBuffer(paint.Length, 4);
			livePaint.SetData(paint);
		}
	}

	private void BindPaint(Material m, bool live)
	{
		if (dummyPaint == null)
		{
			dummyPaint = new ComputeBuffer(1, 4);
			dummyPaint.SetData(new uint[1]);
		}
		bool flag = false;
		if (live)
		{
			flag = livePaintOn && livePaint != null && livePaint.count >= Solver.N;
		}
		else if (stPaintOn)
		{
			flag = stPaint != null;
		}
		ComputeBuffer value = flag ? (live ? livePaint : stPaint) : dummyPaint;
		m.SetBuffer("_Paint", value);
		m.SetInt("_PaintOn", flag ? 1 : 0);
	}

	public void SetStatic(Vector2[] pos, uint[] matIdx, int count)
	{
		if (count <= 0)
		{
			stCount = 0;
			return;
		}
		if (stPos == null || stPos.count < count)
		{
			ReleaseStatic();
			int num = Math.Max(count, (int)Math.Ceiling((double)count * 1.5));
			stPos = new ComputeBuffer(num, 8);
			stVel = new ComputeBuffer(num, 8);
			stVel.SetData(new Vector2[num]);
			stStress = new ComputeBuffer(num, 16);
			stStress.SetData(new Vector4[num]);
			stDamage = new ComputeBuffer(num, 4);
			stDamage.SetData(new float[num]);
			stFail = new ComputeBuffer(num, 4);
			stFail.SetData(new float[num]);
			stMat = new ComputeBuffer(num, 4);
			stMat.SetData(new uint[num]);
			stTemp = new ComputeBuffer(num, 4);
			stTemp.SetData(new float[num]);
		}
		stCount = count;
		stPos.SetData(pos, 0, 0, count);
		stMat.SetData(matIdx, 0, 0, count);
	}

	public void SetOutlines(List<Vector3[]> lines, List<Color> colors, List<bool> open = null)
	{
		outlines.Clear();
		outlineColors.Clear();
		outlineOpen.Clear();
		if (lines != null)
		{
			outlines.AddRange(lines);
		}
		if (colors != null)
		{
			outlineColors.AddRange(colors);
		}
		if (open != null)
		{
			outlineOpen.AddRange(open);
		}
	}

	public void NotifyStepped(int steps)
	{
		stepCounter += steps;
	}

	public void ResetBodyCache()
	{
		liveBodyAtStep = -1;
		LastBodyCount = -1;
	}

	private void OnRenderObject()
	{
		Material material = Mat();
		if (material == null)
		{
			return;
		}
		bool flag = ViewportPx.width > 1f;
		if (Solver == null && stCount == 0 && outlines.Count == 0 && cursorLines.Count == 0 && !ShowGrid && !ShowDomain)
		{
			return;
		}
		if (flag)
		{
			GL.Viewport(ViewportPx);
		}
		DrawChrome();
		int num = ((Solver != null) ? Solver.N : stCount) * 6;
		if (Solver == null)
		{
			if (stCount <= 0)
			{
				return;
			}
			material.SetBuffer("_Pos", stPos);
			material.SetBuffer("_Vel", stVel);
			material.SetBuffer("_Stress", stStress);
			material.SetBuffer("_Damage", stDamage);
			material.SetBuffer("_Fail", stFail);
			material.SetBuffer("_Mat", stMat);
			material.SetBuffer("_BodyId", stBody);
			material.SetBuffer("_Temp", stTemp);
			BindPaint(material, live: false);
		}
		else
		{
			material.SetBuffer("_Pos", Solver.PosBuffer);
			material.SetBuffer("_Vel", Solver.VelBuffer);
			material.SetBuffer("_Stress", Solver.StressBuffer);
			material.SetBuffer("_Damage", Solver.DamageBuffer);
			material.SetBuffer("_Fail", Solver.FailBuffer);
			material.SetBuffer("_Mat", Solver.MatBuffer);
			material.SetBuffer("_BodyId", BodyBuffer());
			material.SetBuffer("_Temp", (TemperatureOverride != null) ? TemperatureOverride : Solver.TemperatureBuffer);
			BindPaint(material, live: true);
		}
		material.SetVector("_Bounds", new Vector4(Lo.x, Lo.y, Hi.x, Hi.y));
		material.SetFloat("_HalfSize", Dx * 0.5f);
		int num2 = Mathf.Clamp(FieldMode, 0, 6);
		Vector2 vector = FieldRanges[num2];
		if (num2 == 6)
		{
			float num3 = TMeltMax();
			if (num3 > GlowTminDisplayK)
			{
				vector = new Vector2(GlowTminDisplayK, num3);
			}
		}
		material.SetVector("_Range", new Vector4(vector.x, vector.y, 0f, 0f));
		material.SetInt("_Mode", num2);
		bool flag2 = false;
		if (GlowApplies)
		{
			if (EnsureGlowLut())
			{
				if (coolC <= 0.0)
				{
					GlowRefusal = "свечение: виду не сообщена длительность выстрела (PlannedDurationSeconds) — τ остывания не определено";
				}
				else
				{
					if (Solver != null)
					{
						UpdateGlowDisplay(Solver.N);
					}
					flag2 = stTemp != null && bGlowLut != null && bGlowWMeltInv != null;
				}
			}
		}
		else
		{
			GlowRefusal = null;
		}
		if (RenderStyle == Style.Surface)
		{
			if (flag)
			{
				DrawSurfaceStyle(material, num, clipped: true);
			}
		}
		else
		{
			if (RenderStyle == Style.Dots)
			{
				material.SetFloat("_DiscHalf", Dx * DiscHalfK);
				material.SetVector("_WorldOffset", Vector4.zero);
				material.SetColor("_Tint", new Color(0f, 0f, 0f, 0f));
				material.SetPass(1);
			}
			else
			{
				material.SetPass(0);
			}
			Graphics.DrawProceduralNow(MeshTopology.Triangles, num, 1);
		}
		if (flag2)
		{
			material.SetBuffer("_TempDisp", stTemp);
			material.SetBuffer("_GlowLut", bGlowLut);
			material.SetFloat("_GlowTmin", glowLutTmin);
			material.SetFloat("_GlowTstep", glowLutTstep);
			material.SetInt("_GlowN", glowLutN);
			material.SetFloat("_GlowHaloK", (float)PalHaloK);
			material.SetFloat("_GlowExposure", Exposure);
			material.SetFloat("_GlowHaloBase", Dx * 0.5f);
			material.SetFloat("_GlowIntensity", (float)Flat2DGlowPalette.HaloIntensityScale(1.0));
			material.SetBuffer("_GlowWMeltInv", bGlowWMeltInv);
			material.SetFloat("_DiscHalf", Dx * DiscHalfK);
			material.SetPass(3);
			Graphics.DrawProceduralNow(MeshTopology.Triangles, num, 1);
		}
		if (outlines.Count > 0 || cursorLines.Count > 0 || (cursorSites != null && cursorSites.Length != 0))
		{
			GL.PushMatrix();
			GL.LoadOrtho();
			Material material2 = LineMat();
			material2.SetPass(0);
			GL.Begin(GL.LINES);
			for (int i = 0; i < outlines.Count; i++)
			{
				Color c = ((i < outlineColors.Count) ? outlineColors[i] : Color.white);
				bool openLine = i < outlineOpen.Count && outlineOpen[i];
				Polyline(outlines[i], c, openLine);
			}
			for (int j = 0; j < cursorLines.Count; j++)
			{
				Color c2 = ((j < cursorColors.Count) ? cursorColors[j] : Color.white);
				bool openLine2 = j < cursorOpen.Count && cursorOpen[j];
				Polyline(cursorLines[j], c2, openLine2);
			}
			DrawCursorSites();
			GL.End();
			GL.PopMatrix();
		}
		if (flag)
		{
			if (RenderTexture.active != null)
			{
				GL.Viewport(new Rect(0f, 0f, RenderTexture.active.width, RenderTexture.active.height));
			}
			else
			{
				GL.Viewport(new Rect(0f, 0f, Screen.width, Screen.height));
			}
		}
	}

	private void DrawChrome()
	{
		if (!ShowGrid && !ShowDomain && (PinnedPreview == null || PinnedPreview.Length == 0))
		{
			return;
		}
		float num = Hi.x - Lo.x;
		num = ((num > 1e-09f) ? num : 1e-09f);
		float num2 = Hi.y - Lo.y;
		num2 = ((num2 > 1e-09f) ? num2 : 1e-09f);
		float num3 = ((ViewportPx.width > 1f) ? (Dx * ViewportPx.width / num) : 0f);
		GL.PushMatrix();
		GL.LoadOrtho();
		Material material = LineMat();
		material.SetPass(0);
		GL.Begin(GL.LINES);
		if (ShowGrid && num3 > 0f)
		{
			int num4 = GridStepFor(num3);
			GridStepInDx = num4;
			DrawGridAxis(num4 * Dx, vertical: true, num, num2, num4);
			DrawGridAxis(num4 * Dx, vertical: false, num, num2, num4);
		}
		if (ShowDomain)
		{
			GL.Color(new Color(0.85f, 0.35f, 0.1f, 0.95f));
			Vector2 a = new Vector2((DomainLo.x - Lo.x) / num, (DomainLo.y - Lo.y) / num2);
			Vector2 b = new Vector2((DomainHi.x - Lo.x) / num, (DomainLo.y - Lo.y) / num2);
			Vector2 b2 = new Vector2((DomainHi.x - Lo.x) / num, (DomainHi.y - Lo.y) / num2);
			Vector2 a2 = new Vector2((DomainLo.x - Lo.x) / num, (DomainHi.y - Lo.y) / num2);
			Seg(a, b);
			Seg(b, b2);
			Seg(b2, a2);
			Seg(a2, a);
		}
		PinnedDrawn = 0;
		if (PinnedPreview != null && PinnedPreview.Length != 0)
		{
			int num5 = Mathf.Max(Mathf.CeilToInt(PinnedPreview.Length / (float)MaxPinnedDrawn), 1);
			GL.Color(MenuTheme.Refused);
			float num6 = Dx * 0.5f;
			for (int i = 0; i < PinnedPreview.Length; i += num5)
			{
				Vector2 vector = PinnedPreview[i];
				Seg(new Vector2((vector.x - num6 - Lo.x) / num, (vector.y - num6 - Lo.y) / num2), new Vector2((vector.x + num6 - Lo.x) / num, (vector.y + num6 - Lo.y) / num2));
				Seg(new Vector2((vector.x - num6 - Lo.x) / num, (vector.y + num6 - Lo.y) / num2), new Vector2((vector.x + num6 - Lo.x) / num, (vector.y - num6 - Lo.y) / num2));
				PinnedDrawn++;
			}
		}
		GL.End();
		GL.PopMatrix();
	}

	private void DrawGridAxis(float g, bool vertical, float spanX, float spanY, int stepInDx)
	{
		int num = (int)Math.Floor((double)((vertical ? Lo.x : Lo.y) / g));
		int num2 = (int)Math.Ceiling((double)((vertical ? Hi.x : Hi.y) / g));
		if (num2 - num > MaxGridLines || num > num2)
		{
			return;
		}
		for (int i = num; i <= num2; i++)
		{
			GL.Color((i % 10 == 0) ? new Color(0.35f, 0.38f, 0.44f, 0.85f) : new Color(0.55f, 0.58f, 0.62f, 0.35f));
			float num3 = i * g;
			if (vertical)
			{
				float num4 = Hi.x - Lo.x;
				num4 = ((num4 > 1e-09f) ? num4 : 1e-09f);
				float num5 = Hi.y - Lo.y;
				num5 = ((num5 > 1e-09f) ? num5 : 1e-09f);
				float x = (num3 - Lo.x) / num4;
				Seg(new Vector2(x, 0f), new Vector2(x, 1f));
			}
			else
			{
				float num6 = Hi.x - Lo.x;
				num6 = ((num6 > 1e-09f) ? num6 : 1e-09f);
				float num7 = Hi.y - Lo.y;
				num7 = ((num7 > 1e-09f) ? num7 : 1e-09f);
				float y = (num3 - Lo.y) / num7;
				Seg(new Vector2(0f, y), new Vector2(1f, y));
			}
		}
	}

	private static void Seg(Vector2 a, Vector2 b)
	{
		GL.Vertex3(a.x, a.y, 0.5f);
		GL.Vertex3(b.x, b.y, 0.5f);
	}

	private void Polyline(Vector3[] pts, Color c, bool openLine)
	{
		if (pts == null || pts.Length < 2)
		{
			return;
		}
		GL.Color(c);
		int num = openLine ? (pts.Length - 1) : pts.Length;
		for (int i = 0; i < num; i++)
		{
			float num2 = Hi.x - Lo.x;
			num2 = ((num2 > 1e-09f) ? num2 : 1e-09f);
			float num3 = Hi.y - Lo.y;
			num3 = ((num3 > 1e-09f) ? num3 : 1e-09f);
			Vector3 vector = pts[i];
			Vector3 vector2 = pts[(i + 1) % pts.Length];
			GL.Vertex3((vector.x - Lo.x) / num2, (vector.y - Lo.y) / num3, 0.1f);
			GL.Vertex3((vector2.x - Lo.x) / num2, (vector2.y - Lo.y) / num3, 0.1f);
		}
	}

	private void DrawCursorSites()
	{
		CursorSitesDrawn = 0;
		if (cursorSites == null || cursorSites.Length == 0)
		{
			return;
		}
		int num = Math.Max((int)Math.Ceiling((double)(cursorSites.Length / (float)MaxCursorSites)), 1);
		GL.Color(cursorSiteColor);
		float num2 = Dx * 0.38f;
		for (int i = 0; i < cursorSites.Length; i += num)
		{
			float num3 = Hi.x - Lo.x;
			num3 = ((num3 > 1e-09f) ? num3 : 1e-09f);
			float num4 = Hi.y - Lo.y;
			num4 = ((num4 > 1e-09f) ? num4 : 1e-09f);
			Vector2 vector = cursorSites[i];
			GL.Vertex3((vector.x - num2 - Lo.x) / num3, (vector.y - num2 - Lo.y) / num4, 0.5f);
			GL.Vertex3((vector.x + num2 - Lo.x) / num3, (vector.y - num2 - Lo.y) / num4, 0.5f);
			GL.Vertex3((vector.x + num2 - Lo.x) / num3, (vector.y - num2 - Lo.y) / num4, 0.5f);
			GL.Vertex3((vector.x + num2 - Lo.x) / num3, (vector.y + num2 - Lo.y) / num4, 0.5f);
			GL.Vertex3((vector.x + num2 - Lo.x) / num3, (vector.y + num2 - Lo.y) / num4, 0.5f);
			GL.Vertex3((vector.x - num2 - Lo.x) / num3, (vector.y + num2 - Lo.y) / num4, 0.5f);
			GL.Vertex3((vector.x - num2 - Lo.x) / num3, (vector.y + num2 - Lo.y) / num4, 0.5f);
			GL.Vertex3((vector.x - num2 - Lo.x) / num3, (vector.y - num2 - Lo.y) / num4, 0.5f);
			CursorSitesDrawn++;
		}
	}

	private Vector2 ToView(Vector3 w)
	{
		float num = Hi.x - Lo.x;
		num = ((num >= 1e-09f) ? num : 1e-09f);
		float num2 = Hi.y - Lo.y;
		num2 = ((num2 >= 1e-09f) ? num2 : 1e-09f);
		return new Vector2((w.x - Lo.x) / num, (w.y - Lo.y) / num2);
	}

	private static Material LineMat()
	{
		if (lineMaterial == null)
		{
			lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
			lineMaterial.hideFlags = HideFlags.HideAndDontSave;
			lineMaterial.SetInt("_ZTest", 8);
		}
		return lineMaterial;
	}

	private ComputeBuffer BodyBuffer()
	{
		if (Solver == null)
		{
			return stBody;
		}
		if (liveBody == null || liveBody.count != Solver.N)
		{
			if (liveBody != null)
			{
				liveBody.Release();
			}
			liveBody = new ComputeBuffer(Solver.N, 4);
			liveBody.SetData(new uint[Solver.N]);
			liveBodyAtStep = -1;
		}
		if (FieldMode != 5)
		{
			return liveBody;
		}
		if (liveBodyAtStep >= 0 && stepCounter - liveBodyAtStep < BodiesEverySteps)
		{
			return liveBody;
		}
		Vector2[] array = Solver.ReadPositions();
		uint[] mustMatch = (Solver.SeparateBodies ? Solver.ReadBodyIds() : null);
		if (Solver.Erosion)
		{
			uint[] array2 = Solver.ReadEroded();
			List<int> list = new List<int>(array.Length);
			for (int i = 0; i < array.Length; i++)
			{
				if (array2[i] != 1)
				{
					list.Add(i);
				}
			}
			Vector2[] array3 = new Vector2[list.Count];
			uint[] array4 = ((mustMatch != null) ? new uint[list.Count] : null);
			for (int j = 0; j < list.Count; j++)
			{
				int index = list[j];
				array3[j] = array[index];
				if (array4 != null)
				{
					array4[j] = mustMatch[index];
				}
			}
			LastBodyCount = Flat2DBodies.Label(array3, Solver.H * 2.0, out int[] rank2, array4);
			uint[] array5 = new uint[array.Length];
			for (int k = 0; k < array.Length; k++)
			{
				array5[k] = uint.MaxValue;
			}
			for (int l = 0; l < list.Count; l++)
			{
				array5[list[l]] = (uint)rank2[l];
			}
			liveBody.SetData(array5);
		}
		else
		{
			LastBodyCount = Flat2DBodies.Label(array, Solver.H * 2.0, out int[] rank, mustMatch);
			uint[] array6 = new uint[rank.Length];
			for (int m = 0; m < rank.Length; m++)
			{
				array6[m] = (uint)rank[m];
			}
			liveBody.SetData(array6);
		}
		liveBodyAtStep = stepCounter;
		return liveBody;
	}

	private void ReleaseStatic()
	{
		if (stPos != null)
		{
			stPos.Release();
		}
		if (stVel != null)
		{
			stVel.Release();
		}
		if (stStress != null)
		{
			stStress.Release();
		}
		if (stDamage != null)
		{
			stDamage.Release();
		}
		if (stFail != null)
		{
			stFail.Release();
		}
		if (stMat != null)
		{
			stMat.Release();
		}
		if (stBody != null)
		{
			stBody.Release();
		}
		if (stTemp != null)
		{
			stTemp.Release();
		}
		stTemp = null;
		stBody = null;
		stMat = null;
		stFail = null;
		stDamage = null;
		stStress = null;
		stVel = null;
		stPos = null;
		stCount = 0;
	}

	private void OnDestroy()
	{
		ReleaseStatic();
		if (liveBody != null)
		{
			liveBody.Release();
		}
		liveBody = null;
		if (stPaint != null)
		{
			stPaint.Release();
		}
		stPaint = null;
		if (livePaint != null)
		{
			livePaint.Release();
		}
		livePaint = null;
		if (dummyPaint != null)
		{
			dummyPaint.Release();
		}
		dummyPaint = null;
		if (stTemp != null)
		{
			stTemp.Release();
		}
		stTemp = null;
		if (bGlowLut != null)
		{
			bGlowLut.Release();
		}
		bGlowLut = null;
		if (bGlowWMeltInv != null)
		{
			bGlowWMeltInv.Release();
		}
		bGlowWMeltInv = null;
		if (mat != null)
		{
			Object.Destroy(mat);
		}
		if (surfMat != null)
		{
			Object.Destroy(surfMat);
		}
	}
}
