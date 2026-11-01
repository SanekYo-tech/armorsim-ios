using System;
using System.Collections.Generic;

[Serializable]
public sealed class SuperMeshSource
{
	public List<ModelSourceFile> files;

	public string contentSha;

	public ModelImportSettings settings;

	public int partCount;

	public string StoreKey => null;
}
