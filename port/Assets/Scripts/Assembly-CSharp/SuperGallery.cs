using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

public static class SuperGallery
{
	public sealed class Arm
	{
		public readonly string file;

		public readonly string armKey;

		public Arm(string file, string armKey = null)
		{
			this.file = file;
			this.armKey = armKey;
		}
	}

	public sealed class Item
	{
		public readonly string id;

		public readonly string source;

		public readonly string caveat;

		public readonly ReadOnlyCollection<Arm> arms;

		public Item(string id, string source, string caveat, params Arm[] arms)
		{
			this.id = id;
			this.source = source;
			this.caveat = caveat;
			this.arms = new ReadOnlyCollection<Arm>(arms);
		}
	}

	public sealed class ResolvedArm
	{
		public Arm arm;

		public string path;

		public long bytes;

		public bool Present => path != null;
	}

	public sealed class Resolved
	{
		public Item item;

		public List<ResolvedArm> arms;

		public bool AnyPresent
		{
			get
			{
				foreach (ResolvedArm arm in arms)
				{
					if (arm.Present)
					{
						return true;
					}
				}
				return false;
			}
		}

		public bool AllPresent
		{
			get
			{
				foreach (ResolvedArm arm in arms)
				{
					if (!arm.Present)
					{
						return false;
					}
				}
				return true;
			}
		}
	}

	public static readonly ReadOnlyCollection<Item> Items = Array.AsReadOnly(new Item[7]
	{
		new Item("jet_era", "REPORT_SUPER_the_big_piece_2026-09-03.md:193-200; шапки лент 16.09", null, new Arm[2]
		{
			new Arm("s9_money_jet_eraon.tape", "era_on"),
			new Arm("s9_money_jet_eraoff.tape", "era_off")
		}),
		new Item("ricochet65", "REPORT_SUPER_ansys_picture_2026-08-30.md:30; шапка ленты 16.09", null, new Arm[1]
		{
			new Arm("money_ricochet_65deg_1500.tape")
		}),
		new Item("penetration0", "REPORT_SUPER_ansys_picture_2026-08-30.md:30; шапка ленты 16.09", null, new Arm[1]
		{
			new Arm("money_penetration_0deg_1500.tape")
		}),
		new Item("pack3030", "REPORT_SUPER_visible_fracture_2026-08-31.md:208; шапки лент 16.09", null, new Arm[2]
		{
			new Arm("owner_pack_30_60_gap100_30_0_1500.tape", "v1500"),
			new Arm("owner_pack_30_60_gap100_30_0_1800.tape", "v1800")
		}),
		new Item("capped_core", "NightS12.cs:134-213 — стенд есть, записи ленты нет", null, new Arm[2]
		{
			new Arm("s12_cap_capped.tape", "capped"),
			new Arm("s12_cap_bare.tape", "bare")
		}),
		new Item("al_spall", "NightS12.cs:93-94 — подпись стенда «тыльный откол»; шапка ленты 16.09", null, new Arm[1]
		{
			new Arm("s12_al_plate.tape")
		}),
		new Item("core_jacket", "REPORT_SUPER_catalogue_2026-09-01.md:230-233; шапка ленты 16.09", "super.gallery.core_jacket.caveat", new Arm[1]
		{
			new Arm("s8_shell")
		})
	});

	public static List<Resolved> Scan()
	{
		List<Resolved> list = new List<Resolved>(Items.Count);
		foreach (Item item in Items)
		{
			Resolved resolved = new Resolved();
			resolved.arms = new List<ResolvedArm>();
			resolved.item = item;
			foreach (Arm arm in item.arms)
			{
				string path = SuperTapeFiles.FindTape(arm.file);
				long bytes = 0L;
				if (path != null)
				{
					bytes = new FileInfo(path).Length;
				}
				ResolvedArm resolvedArm = new ResolvedArm();
				resolvedArm.arm = arm;
				resolvedArm.path = path;
				resolvedArm.bytes = bytes;
				resolved.arms.Add(resolvedArm);
			}
			list.Add(resolved);
		}
		return list;
	}

	public static string ArmCaption(Resolved r, ResolvedArm a)
	{
		string text = Localization.T("super.gallery." + r.item.id + ".title");
		if (a.arm.armKey != null)
		{
			text += "  ·  " + Localization.T("super.gallery.arm." + a.arm.armKey);
		}
		if (a.Present)
		{
			return text + "  ·  " + SuperTapeLibrary.Megabytes(a.bytes);
		}
		return text + "  —  " + Localization.T("super.gallery.absent", new object[1] { a.arm.file });
	}

	public static void Census(List<Resolved> scan, out int itemsPresent, out int armsPresent, out int armsTotal, out List<string> missingFiles)
	{
		itemsPresent = 0;
		armsPresent = 0;
		armsTotal = 0;
		missingFiles = new List<string>();
		foreach (Resolved resolved in scan)
		{
			if (resolved.AnyPresent)
			{
				itemsPresent++;
			}
			foreach (ResolvedArm resolvedArm in resolved.arms)
			{
				armsTotal++;
				if (resolvedArm.Present)
				{
					armsPresent++;
				}
				else
				{
					missingFiles.Add(resolvedArm.arm.file);
				}
			}
		}
	}
}
