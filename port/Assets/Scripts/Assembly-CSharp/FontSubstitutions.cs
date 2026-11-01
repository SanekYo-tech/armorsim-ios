using System.Collections.Generic;
using System.Text;

public static class FontSubstitutions
{
	private static readonly Dictionary<char, string> Map;

	static FontSubstitutions()
	{
		Map = new Dictionary<char, string>
		{
			{ '⌀', "Ø" },
			{ '▶', "►" },
			{ '✖', "×" }
		};
	}

	public static string Apply(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return s;
		}
		int num = -1;
		for (int i = 0; i < s.Length; i++)
		{
			if (Map.ContainsKey(s[i]))
			{
				num = i;
				break;
			}
		}
		if (num < 0)
		{
			return s;
		}
		StringBuilder stringBuilder = new StringBuilder(s.Length);
		stringBuilder.Append(s, 0, num);
		for (int j = num; j < s.Length; j++)
		{
			if (Map.TryGetValue(s[j], out string value))
			{
				stringBuilder.Append(value);
			}
			else
			{
				stringBuilder.Append(s[j]);
			}
		}
		return stringBuilder.ToString();
	}
}
