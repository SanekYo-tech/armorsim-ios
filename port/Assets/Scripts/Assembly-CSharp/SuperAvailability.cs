using System;

public static class SuperAvailability
{
	public struct Verdict
	{
		public bool ok;

		public string key;

		public object[] args;

		public string text;

		public string note;

		public static Verdict Ok => new Verdict
		{
			ok = true,
			key = "",
			args = new object[0],
			text = "",
			note = ""
		};
	}

	private const float ProbeSideM = 0.13f;

	private const float ProbeSpeedMps = 1500f;

	private static Verdict OkFor(string registryName, bool asRod)
	{
		string noteKey = SuperShotRunner.MaterialNoteKey(registryName, asRod);
		Verdict result = Verdict.Ok;
		if (noteKey != null)
		{
			result.note = Localization.T(noteKey);
		}
		return result;
	}

	public static Verdict Refuse(string key, params object[] args)
	{
		return new Verdict
		{
			ok = false,
			key = key,
			args = args ?? new object[0],
			text = Localization.T("super.avail.prefix") + Localization.T(key, args ?? new object[0]),
			note = ""
		};
	}

	public static Verdict Projectile(string id)
	{
		ProjectileDefinition d = Catalog.MakeProjectile(id);
		return (d == null) ? Verdict.Ok : Definition(d);
	}

	public static Verdict Definition(ProjectileDefinition d)
	{
		if (d == null)
		{
			return Verdict.Ok;
		}
		if (d.kind >= ProjectileClass.LongRod && d.kind <= ProjectileClass.HollowBody)
		{
			RodPassports.Refusal(d, out string refusalKey, out object[] refusalArgs);
			if (refusalKey != null)
			{
				return Refuse(refusalKey, refusalArgs);
			}
		}
		if (d.kind == ProjectileClass.ShapedCharge)
		{
			if (d.isTandem)
			{
				return Refuse("super.refuse.tandem");
			}
			SuperShotRunner.TryJetFor(d, out SuperJetSpec jet);
			if (SuperShotRunner.JetGridOk(jet.diameterM, SuperPreset.SpacingM))
			{
				return Verdict.Ok;
			}
			float ceiling = SuperShotRunner.JetSpacingCeiling(jet.diameterM);
			return Refuse((ceiling < 0.0002f) ? "super.refuse.jet_too_fine" : "super.refuse.jet_grid");
		}
		if (d.kind == ProjectileClass.HollowBody)
		{
			SuperShotRunner.HollowRowSilent(d, out string key, out object[] args);
			if (key != null)
			{
				return Refuse(key, args);
			}
			Projectile live = LiveBodyFor(d);
			Projectile.CoreType core = (live != null) ? live.coreType : d.coreType;
			if (SuperShotRunner.MapCore(core) == null)
			{
				return Core(core);
			}
			SuperRodSpec rod = HollowProbeRod(d, live);
			if (SuperShotRunner.HollowCeilingRefusal(rod.velocityMps, d.fragmentVelocityMps, out key, out args))
			{
				return Refuse(key, args);
			}
			if (SuperShotRunner.HollowCavityRefusal(in rod, SuperPreset.SpacingM, out key, out args))
			{
				return Refuse(key, args);
			}
			float triggerTime = SuperPreset.TriggerTimeFor(d.fuzeDelayS);
			if (SuperShotRunner.HollowWindowRefusal(0.002f, 0f, rod.velocityMps, SuperPreset.SpacingM, triggerTime, 5E-06f, SuperPreset.DurationS, "", out key, out args))
			{
				return Refuse(key, args);
			}
			return OkFor(SuperShotRunner.MapCore(core), true);
		}
		if (d.kind != ProjectileClass.LongRod && d.kind != ProjectileClass.ArmorPiercing && d.kind != ProjectileClass.Kinetic)
		{
			return Refuse("super.refuse.round");
		}
		if (SuperShotRunner.MapCore(d.coreType) == null)
		{
			return Core(d.coreType);
		}
		if (d.ballisticCap)
		{
			Projectile live = LiveBodyFor(d);
			Projectile.CoreType core = (live != null) ? live.coreType : d.coreType;
			string material = SuperShotRunner.MapCore(core);
			if (material == null)
			{
				return Core(core);
			}
			float diameter = (live != null) ? live.diameter : d.diameterMM;
			float length = (live != null) ? live.rodLengthMM : d.rodLengthMM;
			float speed = (live != null) ? live.combatVelocity : d.velocityMps;
			if (!SuperShell.FromLive(d, diameter, length, speed, material, false, out SuperShellSpec shell, out string reason, out string passport))
			{
				return Refuse("super.refuse.round");
			}
			SuperShellProfile profile = SuperShellProfile.ForShell(in shell);
			if (!profile.Validate(SuperPreset.SpacingM, out string failKey, out object[] failArgs, out string prose))
			{
				return Refuse(failKey, failArgs);
			}
		}
		Verdict result = OkFor(SuperShotRunner.MapCore(d.coreType), true);
		if (RodPassports.ResolutionNoteArgs(d, SuperPreset.SpacingM, out object[] noteArgs))
		{
			string note = Localization.T("super.note.rod_resolution", noteArgs);
			result.note = (string.IsNullOrEmpty(result.note) ? note : (result.note + "\n" + note));
		}
		return result;
	}

	public static Projectile LiveBodyFor(ProjectileDefinition d)
	{
		if (d == null)
		{
			return null;
		}
		Projectile live = UnityEngine.Object.FindFirstObjectByType<Projectile>();
		if (live == null)
		{
			return null;
		}
		ProjectileDefinition actual = live.GetDefinition();
		if (actual != null && actual.kind == d.kind && string.Equals(actual.name ?? "", d.name ?? "", StringComparison.Ordinal))
		{
			return live;
		}
		return null;
	}

	public static SuperRodSpec HollowProbeRod(ProjectileDefinition d, Projectile live)
	{
		SuperRodSpec rod = default;
		float diameterMm = Math.Max(0.001f, (live != null) ? live.diameter : d.diameterMM);
		rod.diameterM = diameterMm * 0.001f;
		float lengthMm = (live != null) ? live.rodLengthMM : d.rodLengthMM;
		rod.lengthM = Math.Max(rod.diameterM, lengthMm * 0.001f);
		rod.velocityMps = Math.Max(1f, (live != null) ? live.combatVelocity : d.velocityMps);
		rod.nose = d.nose;
		rod.material = SuperShotRunner.MapCore((live != null) ? live.coreType : d.coreType);
		rod.tipZM = 0.002f;
		rod.cavityFraction = d.cavityFrac;
		return rod;
	}

	public static Verdict Core(Projectile.CoreType c)
	{
		string mapped = SuperShotRunner.MapCore(c);
		if (mapped != null)
		{
			return OkFor(mapped, true);
		}
		string key = SuperShotRunner.CoreRefusalKey(c);
		object[] args = new object[1] { SuperShotRunner.CoreName(c) };
		return new Verdict
		{
			ok = false,
			key = key,
			args = args,
			text = Localization.T("super.avail.prefix") + Localization.T(key, args),
			note = ""
		};
	}

	public static Verdict Material(ArmorPlate.ArmorType t)
	{
		string mapped = SuperShotRunner.MapMaterial(t);
		if (mapped != null)
		{
			return OkFor(mapped, false);
		}
		string key = SuperShotRunner.MaterialRefusalKey(t);
		object[] args = new object[1] { Catalog.MaterialName(t) };
		return new Verdict
		{
			ok = false,
			key = key,
			args = args,
			text = Localization.T("super.avail.prefix") + Localization.T(key, args),
			note = ""
		};
	}

	public static Verdict Object(string id)
	{
		if (!Catalog.TryObjectKind(id, out ArmorPlate.ObjectKind kind))
		{
			return Verdict.Ok;
		}
		if (kind == ArmorPlate.ObjectKind.Plate || kind == ArmorPlate.ObjectKind.Rod)
		{
			return Verdict.Ok;
		}
		if (kind == ArmorPlate.ObjectKind.Era)
		{
			if (EraCatalog.TryGet(id, out EraPreset p))
			{
				return Era(p, SuperPreset.SpacingM, 0f);
			}
			return Verdict.Ok;
		}
		if (kind == ArmorPlate.ObjectKind.Nera)
		{
			if (NeraCatalog.TryGet(id, out NeraPreset p))
			{
				return Nera(p, SuperPreset.SpacingM, 0f);
			}
			return Verdict.Ok;
		}
		return Refuse("super.refuse.non_plate");
	}

	public static Verdict Era(EraPreset p, float spacing, float angleDeg)
	{
		SuperEra.SceneCassette c = SuperEra.BuildInScene(p, 0.13f, angleDeg, 0f, spacing, 1500f);
		if (c.refuseKey == null)
		{
			return Verdict.Ok;
		}
		return Refuse(c.refuseKey, c.refuseArgs ?? new object[0]);
	}

	public static Verdict Nera(NeraPreset p, float spacing, float angleDeg)
	{
		SuperNera.Pack pack = SuperNera.Build(p, 0.13f, angleDeg + p.mountAngleDeg, 0f, spacing, p.frontMaterial, p.rearMaterial, p.bonding, p.interlayer);
		if (pack.refuseKey == null)
		{
			return Verdict.Ok;
		}
		return Refuse(pack.refuseKey, pack.refuseArgs ?? new object[0]);
	}

	public static Verdict ForId(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return Verdict.Ok;
		}
		if (Catalog.TryObjectKind(id, out ArmorPlate.ObjectKind _))
		{
			return Object(id);
		}
		if (Catalog.TryMaterialType(id, out ArmorPlate.ArmorType material))
		{
			return Material(material);
		}
		if (Catalog.TryCoreType(id, out Projectile.CoreType core))
		{
			return Core(core);
		}
		foreach (ProjectileCatalogEntry entry in Catalog.Projectiles)
		{
			if (entry.meta.id == id)
			{
				return Projectile(id);
			}
		}
		return Verdict.Ok;
	}
}
