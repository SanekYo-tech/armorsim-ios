using System;
using System.Collections.Generic;

[Serializable]
public class SceneFile
{
	public int formatVersion;

	public string name;

	public string savedUtc;

	public string appVersion;

	public string vehicleReference;

	public string provenanceNote;

	public string description;

	public bool readOnly;

	public string section;

	public string family;

	public List<SavedObject> objects;

	public SavedProjectile projectile;

	public List<SavedModel> models;

	public SavedRegion region;
}
