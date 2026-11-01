using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Unity.Mathematics;

public static class RadarAtlasKey
{
	public static string Of(RadarScene scene, double frequencyHz, RadarAtlasSpec spec)
	{
		return Of(scene, frequencyHz, spec, null);
	}

	public static string Of(RadarScene scene, double frequencyHz, RadarAtlasSpec spec, RadarCoating coating)
	{
		if (scene == null)
		{
			throw new ArgumentNullException("scene");
		}
		if (spec == null)
		{
			throw new ArgumentNullException("spec");
		}
		using (SHA256 sha = SHA256.Create())
		{
			using (CryptoStream stream = new CryptoStream(Stream.Null, sha, CryptoStreamMode.Write))
			{
				using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8))
				{
					writer.Write(Encoding.ASCII.GetBytes("RADATLSKEY"));
					writer.Write(1);
					writer.Write(scene.Vertices.Length);
					for (int i = 0; i < scene.Vertices.Length; i++)
					{
						double3 v = scene.Vertices[i];
						writer.Write(v.x);
						writer.Write(v.y);
						writer.Write(v.z);
					}
					writer.Write(scene.Triangles.Length);
					for (int j = 0; j < scene.Triangles.Length; j++)
					{
						writer.Write(scene.Triangles[j]);
					}
					writer.Write(frequencyHz);
					writer.Write(spec.DeltaDeg);
					writer.Write(spec.SamplesPerSide);
					writer.Write(spec.Seed);
					byte[] tag = Encoding.UTF8.GetBytes(spec.SolverTag);
					writer.Write(tag.Length);
					writer.Write(tag);
					if (coating != null && !coating.IsMetal)
					{
						byte[] canonicalBytes = coating.CanonicalBytes();
						writer.Write(canonicalBytes.Length);
						writer.Write(canonicalBytes);
					}
					writer.Flush();
					stream.FlushFinalBlock();
					return RadarMapModelRegistry.Hex(sha.Hash);
				}
			}
		}
	}
}
