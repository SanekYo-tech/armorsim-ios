using System.Collections.Generic;
using UnityEngine;

public sealed class PreparedModel
{
	public readonly List<ModelPart> parts;

	public List<ModelSourceFile> sources;

	public string contentSha;

	public ModelImportSettings settings;

	public Bounds bounds;

	public PreparedModel()
	{
		parts = new List<ModelPart>();
	}

	public Vector3 centreShiftM;
}
