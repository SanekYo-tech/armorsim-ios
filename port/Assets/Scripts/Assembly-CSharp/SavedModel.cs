using System;
using System.Collections.Generic;

[Serializable]
public class SavedModel
{
	public List<ModelSourceFile> files;

	public string contentSha;

	public string units;

	public string up;

	public bool mirrorX;

	public float scale;

	public float posXM;

	public float posYM;

	public float posZM;

	public float angleXDeg;

	public float angleYDeg;

	public List<string> partMaterials;

	public float liveScale;

	public bool edgeClamp;
}
