using System;
using Unity.Mathematics;
using UnityEngine;

public static class RadarBuiltinModels
{
	public const double Yf23LengthMeters = 20.54;

	public const string Yf23Resource = "RadarYF23";

	public const string Yf23NameKey = "radar.model.yf23";

	public static byte[] ReadYf23Resource()
	{
		TextAsset textAsset = Resources.Load<TextAsset>(Yf23Resource);
		if (textAsset == null)
		{
			throw new InvalidOperationException("Missing built-in RadarYF23 mesh");
		}
		return textAsset.bytes;
	}

	public static AeroMeshData ParseYf23(byte[] bytes)
	{
		AeroMeshData mesh = AeroImporter.Parse(bytes, ".stl");
		RadarOrientation orientation = RadarOrientation.Identity;
		orientation = orientation.RotateX();
		orientation = orientation.RotateZ();
		orientation = orientation.RotateZ();
		Vector3[] rotated = new Vector3[mesh.Vertices.Length];
		for (int i = 0; i < rotated.Length; i++)
		{
			Vector3 vector = mesh.Vertices[i];
			double3 p = orientation.Apply(new double3(vector.x, vector.y, vector.z));
			rotated[i] = new Vector3((float)p.x, (float)p.y, (float)p.z);
		}
		AeroMeshData aeroMeshData = new AeroMeshData(rotated, mesh.Triangles);
		aeroMeshData.OriginalSize = new Vector3(mesh.OriginalSize.x, mesh.OriginalSize.z, mesh.OriginalSize.y);
		aeroMeshData.DegenerateTriangles = mesh.DegenerateTriangles;
		return aeroMeshData;
	}

	public static RadarModel Yf23Model(byte[] bytes)
	{
		AeroMeshData data = ParseYf23(bytes);
		return RadarModelImport.FromAero(data, RadarLengthUnit.CustomLength, Yf23LengthMeters, RadarOrientation.Identity);
	}
}
