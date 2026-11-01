using System.Collections.Generic;

public sealed class ImportedModel
{
	public readonly List<ImportedPart> parts;

	public readonly List<ModelSourceFile> sources;

	public string ContentSha;

	public int TriangleCount => 0;
}
