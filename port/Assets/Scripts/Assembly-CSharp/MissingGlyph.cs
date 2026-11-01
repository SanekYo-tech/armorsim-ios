public struct MissingGlyph
{
	public string key;

	public string language;

	public int codepoint;

	public string context;

	public char Ch => (char)codepoint;

	public override string ToString()
	{
		return string.Format("{0} [{1}] U+{2:X4} '{3}'  …{4}…", key, language, codepoint, Ch, context);
	}
}
