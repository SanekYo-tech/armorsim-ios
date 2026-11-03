public struct UltraNode
{
	public string path;

	public UltraNodeKind kind;

	public string note;

	public string Name
	{
		get
		{
			if (path == null)
			{
				return null;
			}
			int num = path.LastIndexOf('/');
			if (num < 0)
			{
				return path;
			}
			return path.Substring(num + 1);
		}
	}

	public string ParentPath
	{
		get
		{
			if (path == null)
			{
				return null;
			}
			int num = path.LastIndexOf('/');
			if (num < 0)
			{
				return null;
			}
			return path.Substring(0, num);
		}
	}
}
