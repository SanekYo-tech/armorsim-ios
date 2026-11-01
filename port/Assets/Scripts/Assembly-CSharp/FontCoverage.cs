using System;
using System.Collections.Generic;
using System.Text;

public static class FontCoverage
{
	public static bool IsExempt(int cp)
	{
		return cp == 9 || cp == 10 || cp == 13 || cp == 32 || cp == 160;
	}

	public static string StripRichText(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return s;
		}
		StringBuilder sb = new StringBuilder(s.Length);
		bool inside = false;
		for (int i = 0; i < s.Length; i++)
		{
			char ch = s[i];
			if (ch == '<')
			{
				inside = true;
			}
			else if (ch == '>')
			{
				inside = false;
			}
			else if (!inside)
			{
				sb.Append(ch);
			}
		}
		return sb.ToString();
	}

	public static List<MissingGlyph> Audit(IDictionary<string, string[]> table, string[] languageNames, Func<int, bool> hasGlyph)
	{
		List<MissingGlyph> missing = new List<MissingGlyph>();
		foreach (KeyValuePair<string, string[]> kv in table)
		{
			for (int li = 0; li < kv.Value.Length; li++)
			{
				string text = kv.Value[li];
				if (string.IsNullOrEmpty(text))
				{
					continue;
				}
				string plain = StripRichText(text);
				for (int i = 0; i < plain.Length; i++)
				{
					char ch = plain[i];
					if (IsExempt(ch) || hasGlyph(ch))
					{
						continue;
					}
					int from = Math.Max(0, i - 12);
					int to = Math.Min(plain.Length, i + 12);
					missing.Add(new MissingGlyph
					{
						key = kv.Key,
						language = ((li < languageNames.Length) ? languageNames[li] : li.ToString()),
						codepoint = ch,
						context = plain.Substring(from, to - from)
					});
				}
			}
		}
		return missing;
	}

	public static string Summarise(List<MissingGlyph> missing, int maxLines = 40)
	{
		if (missing == null || missing.Count == 0)
		{
			return "покрытие полное: пропусков нет";
		}
		Dictionary<int, int> codeCounts = new Dictionary<int, int>();
		Dictionary<string, int> langCounts = new Dictionary<string, int>();
		foreach (MissingGlyph g in missing)
		{
			int n;
			codeCounts.TryGetValue(g.codepoint, out n);
			codeCounts[g.codepoint] = n + 1;
			int m;
			langCounts.TryGetValue(g.language, out m);
			langCounts[g.language] = m + 1;
		}
		StringBuilder sb = new StringBuilder();
		sb.Append("ПРОПУСКОВ: ");
		sb.Append(missing.Count);
		sb.Append(" в ");
		sb.Append(codeCounts.Count);
		sb.Append(" различных кодовых точках\n");
		sb.Append("по языкам: ");
		foreach (KeyValuePair<string, int> kv in langCounts)
		{
			sb.Append(kv.Key);
			sb.Append('=');
			sb.Append(kv.Value);
			sb.Append("  ");
		}
		sb.Append('\n');
		List<int> cps = new List<int>(codeCounts.Keys);
		cps.Sort();
		sb.Append("нет глифов: ");
		foreach (int cp in cps)
		{
			sb.Append(string.Format("U+{0:X4}({1})x{2} ", cp, (char)cp, codeCounts[cp]));
		}
		sb.Append('\n');
		int line = 0;
		foreach (MissingGlyph g in missing)
		{
			if (line >= maxLines)
			{
				sb.Append("  … и ещё ");
				sb.Append(missing.Count - maxLines);
				sb.Append('\n');
				break;
			}
			sb.Append("  ");
			sb.Append(g);
			sb.Append('\n');
			line++;
		}
		return sb.ToString();
	}
}
