using System.Collections.Generic;

public sealed class ImportedModel
{
	public readonly List<ImportedPart> parts;

	public readonly List<ModelSourceFile> sources;

	public string ContentSha;

	public int TriangleCount
	{
		get
		{
			int num = 0;
			foreach (ImportedPart part in parts)
			{
				num += ((part.triangles != null) ? (part.triangles.Length / 3) : 0);
			}
			return num;
		}
	}
}
