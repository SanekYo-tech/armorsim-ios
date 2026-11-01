using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class MaterialLibrary
{
	private struct Spec
	{
		public Color albedo;

		public float metallic;

		public float smooth;

		public Spec(Color a, float m, float s)
		{
			albedo = a;
			metallic = m;
			smooth = s;
		}
	}

	private static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

	private static Shader standard;

	private const string TriplanarShaderResourcePath = "Shaders/TriplanarMetal";

	private const string TriplanarShaderName = "ArmorSim/TriplanarMetal";

	private static readonly Dictionary<string, Texture2D> texCache = new Dictionary<string, Texture2D>();

	private static readonly Dictionary<Texture2D, Color> meanCache = new Dictionary<Texture2D, Color>();

	private static Shader triplanar;

	private static bool triplanarSearched;

	private static Shader Standard()
	{
		if (standard == null)
		{
			standard = Shader.Find("Standard");
		}
		return standard;
	}

	private static Shader Triplanar()
	{
		if (!triplanarSearched)
		{
			triplanarSearched = true;
			triplanar = Resources.Load<Shader>(TriplanarShaderResourcePath);
			if (triplanar == null)
			{
				triplanar = Shader.Find(TriplanarShaderName);
			}
			if (triplanar == null)
			{
				Debug.LogWarning("[MaterialLibrary] Шейдер «ArmorSim/TriplanarMetal» не найден — текстуры не накладываются, все материалы идут по откату на Standard.");
			}
		}
		return triplanar;
	}

	private static Texture2D LoadTex(string folder, string name)
	{
		string key = folder + "/" + name;
		if (texCache.TryGetValue(key, out Texture2D tex))
		{
			return tex;
		}
		tex = Resources.Load<Texture2D>("Textures/" + key);
		if (tex == null)
		{
			tex = Resources.Load<Texture2D>("Textures/" + name);
		}
		texCache[key] = tex;
		return tex;
	}

	private static Texture2D ArmorTex(ArmorPlate.ArmorType t)
	{
		return LoadTex("Armor", t.ToString());
	}

	private static Texture2D CoreTex(Projectile.CoreType t)
	{
		return LoadTex("Core", t.ToString());
	}

	private static Color TextureMean(Texture2D tex)
	{
		if (tex == null)
		{
			return Color.white;
		}
		if (meanCache.TryGetValue(tex, out Color mean))
		{
			return mean;
		}
		if (!TryMeanFromPixels(tex, out mean) && !TryMeanFromGpu(tex, out mean))
		{
			mean = Color.white;
			Debug.LogWarning("[MaterialLibrary] Средняя яркость текстуры «" + tex.name + "» не определена (Read/Write Enabled выключен, свёртка на видеокарте недоступна) — нормировка отключена, средняя яркость плиты может уехать относительно таблицы ArmorSpec.");
		}
		meanCache[tex] = mean;
		return mean;
	}

	private static bool TryMeanFromPixels(Texture2D tex, out Color mean)
	{
		mean = Color.white;
		if (!tex.isReadable || tex.mipmapCount < 1)
		{
			return false;
		}
		Color[] pixels = tex.GetPixels(tex.mipmapCount - 1);
		if (pixels == null || pixels.Length == 0)
		{
			return false;
		}
		float r = pixels[0].r;
		float g = pixels[0].g;
		float b = pixels[0].b;
		if (QualitySettings.activeColorSpace == ColorSpace.Linear)
		{
			r = Mathf.GammaToLinearSpace(r);
			g = Mathf.GammaToLinearSpace(g);
			b = Mathf.GammaToLinearSpace(b);
		}
		mean = new Color(Mathf.Max(r, 0.001f), Mathf.Max(g, 0.001f), Mathf.Max(b, 0.001f), 1f);
		return true;
	}

	private static bool TryMeanFromGpu(Texture2D tex, out Color mean)
	{
		mean = Color.white;
		if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
		{
			return false;
		}
		RenderTexture prev = RenderTexture.active;
		RenderTextureFormat format = (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat) ? RenderTextureFormat.ARGBFloat : (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf) ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32));
		int w = Mathf.Max(tex.width, 1);
		int h = Mathf.Max(tex.height, 1);
		RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, format, RenderTextureReadWrite.Linear);
		Texture2D readback = null;
		try
		{
			rt.filterMode = FilterMode.Bilinear;
			rt.wrapMode = TextureWrapMode.Clamp;
			Graphics.Blit(tex, rt);
			while (w > 1 || h > 1)
			{
				w = Mathf.Max(w / 2, 1);
				h = Mathf.Max(h / 2, 1);
				RenderTexture next = RenderTexture.GetTemporary(w, h, 0, format, RenderTextureReadWrite.Linear);
				next.filterMode = FilterMode.Bilinear;
				next.wrapMode = TextureWrapMode.Clamp;
				Graphics.Blit(rt, next);
				RenderTexture.ReleaseTemporary(rt);
				rt = next;
			}
			readback = new Texture2D(1, 1, (format != 0) ? TextureFormat.RGBAFloat : TextureFormat.RGBA32, false, true);
			RenderTexture.active = rt;
			readback.ReadPixels(new Rect(0f, 0f, 1f, 1f), 0, 0, false);
			readback.Apply(false, false);
			Color c = readback.GetPixel(0, 0);
			mean = new Color(Mathf.Max(c.r, 0.001f), Mathf.Max(c.g, 0.001f), Mathf.Max(c.b, 0.001f), 1f);
			return true;
		}
		finally
		{
			RenderTexture.active = prev;
			if (rt != null)
			{
				RenderTexture.ReleaseTemporary(rt);
			}
			if (readback != null)
			{
				if (Application.isPlaying)
				{
					Object.Destroy(readback);
				}
				else
				{
					Object.DestroyImmediate(readback);
				}
			}
		}
	}

	private static RenderTexture TempRT(int w, int h, RenderTextureFormat fmt)
	{
		RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, fmt, RenderTextureReadWrite.Linear);
		rt.filterMode = FilterMode.Bilinear;
		rt.wrapMode = TextureWrapMode.Clamp;
		return rt;
	}

	private static Color Clamp(Color c)
	{
		return new Color(Mathf.Max(c.r, 0.001f), Mathf.Max(c.g, 0.001f), Mathf.Max(c.b, 0.001f), 1f);
	}

	private static Spec CoreSpec(Projectile.CoreType t)
	{
		switch (t)
		{
		case Projectile.CoreType.WHA:
			return new Spec(new Color(0.3f, 0.3f, 0.33f, 1f), 0.9f, 0.55f);
		case Projectile.CoreType.DU:
			return new Spec(new Color(0.15f, 0.14f, 0.16f, 1f), 0.85f, 0.45f);
		case Projectile.CoreType.Steel:
			return new Spec(new Color(0.55f, 0.55f, 0.58f, 1f), 0.9f, 0.6f);
		case Projectile.CoreType.WC:
			return new Spec(new Color(0.42f, 0.45f, 0.5f, 1f), 0.7f, 0.7f);
		default:
			return new Spec(new Color(0.4f, 0.4f, 0.44f, 1f), 0.85f, 0.55f);
		}
	}

	private static Spec ArmorSpec(ArmorPlate.ArmorType t)
	{
		switch (t)
		{
		case ArmorPlate.ArmorType.RHA:
			return new Spec(new Color(0.38f, 0.38f, 0.42f, 1f), 0.85f, 0.45f);
		case ArmorPlate.ArmorType.HHA:
			return new Spec(new Color(0.3f, 0.3f, 0.34f, 1f), 0.85f, 0.5f);
		case ArmorPlate.ArmorType.Aluminum:
			return new Spec(new Color(0.75f, 0.76f, 0.78f, 1f), 0.8f, 0.35f);
		case ArmorPlate.ArmorType.Titanium:
			return new Spec(new Color(0.52f, 0.51f, 0.45f, 1f), 0.75f, 0.45f);
		case ArmorPlate.ArmorType.Ceramic:
			return new Spec(new Color(0.8f, 0.75f, 0.65f, 1f), 0.1f, 0.2f);
		case ArmorPlate.ArmorType.Tungsten:
			return new Spec(new Color(0.32f, 0.32f, 0.35f, 1f), 0.85f, 0.55f);
		case ArmorPlate.ArmorType.ERA:
			return new Spec(new Color(0.32f, 0.42f, 0.3f, 1f), 0.25f, 0.35f);
		case ArmorPlate.ArmorType.Textolite:
			return new Spec(new Color(0.576f, 0.484f, 0.227f, 1f), 0f, 0.1f);
		case ArmorPlate.ArmorType.ERAHeavy:
			return new Spec(new Color(0.26f, 0.33f, 0.26f, 1f), 0.25f, 0.3f);
		case ArmorPlate.ArmorType.BoronCarbide:
			return new Spec(new Color(0.22f, 0.22f, 0.24f, 1f), 0.15f, 0.45f);
		case ArmorPlate.ArmorType.TitaniumDiboride:
			return new Spec(new Color(0.45f, 0.47f, 0.5f, 1f), 0.45f, 0.4f);
		case ArmorPlate.ArmorType.DepletedUraniumArmour:
			return new Spec(new Color(0.24f, 0.23f, 0.21f, 1f), 0.8f, 0.35f);
		case ArmorPlate.ArmorType.Alumina:
			return new Spec(new Color(0.88f, 0.88f, 0.86f, 1f), 0.05f, 0.25f);
		case ArmorPlate.ArmorType.CastSteel:
			return new Spec(new Color(0.34f, 0.33f, 0.33f, 1f), 0.85f, 0.21f);
		default:
			return new Spec(new Color(0.5f, 0.5f, 0.55f, 1f), 0.8f, 0.4f);
		}
	}

	private static Spec FreshCut(Spec s)
	{
		s.albedo = new Color(Mathf.Min(1f, s.albedo.r * 1.3f), Mathf.Min(1f, s.albedo.g * 1.3f), Mathf.Min(1f, s.albedo.b * 1.3f), 1f);
		s.smooth = Mathf.Min(1f, s.smooth + 0.1f);
		return s;
	}

	private static Material Build(Spec s, string key, Texture2D tex)
	{
		if (tex != null && !GameSettings.Textures)
		{
			key = key + "_notex";
			tex = null;
		}
		if (cache.TryGetValue(key, out Material m) && m != null)
		{
			return m;
		}
		Shader shader = ((tex != null) ? Triplanar() : null);
		if (shader == null)
		{
			shader = Standard();
			tex = null;
		}
		m = new Material(shader);
		m.color = s.albedo;
		if (m.HasProperty("_Metallic"))
		{
			m.SetFloat("_Metallic", s.metallic);
		}
		if (m.HasProperty("_Glossiness"))
		{
			m.SetFloat("_Glossiness", s.smooth);
		}
		if (tex != null)
		{
			m.SetTexture("_MainTex", tex);
			Color mean = TextureMean(tex);
			m.SetVector("_TexMean", new Vector4(mean.r, mean.g, mean.b, 1f));
		}
		cache[key] = m;
		return m;
	}

	public static Material Core(Projectile.CoreType t)
	{
		return Build(CoreSpec(t), "core_" + t, CoreTex(t));
	}

	public static Material CoreFreshCut(Projectile.CoreType t)
	{
		return Build(FreshCut(CoreSpec(t)), "corecut_" + t, CoreTex(t));
	}

	public static Material Armor(ArmorPlate.ArmorType t)
	{
		return Build(ArmorSpec(t), "armor_" + t, ArmorTex(t));
	}

	public static Material ArmorFreshCut(ArmorPlate.ArmorType t)
	{
		return Build(FreshCut(ArmorSpec(t)), "armorcut_" + t, ArmorTex(t));
	}

	public static Color CoreFreshCutColor(Projectile.CoreType t)
	{
		return FreshCut(CoreSpec(t)).albedo;
	}

	public static Material ChannelInterior()
	{
		return Build(new Spec(new Color(0.055f, 0.055f, 0.06f, 1f), 0f, 0.05f), "channel_interior", null);
	}
}
