using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public static class RadarAtlasStore
{
	private static readonly byte[] Magic = Encoding.ASCII.GetBytes("RADATLS1");

	public static string CacheFolder => Path.Combine(Application.persistentDataPath, "RadarAtlas");

	public static string CachePath(string key)
	{
		return Path.Combine(CacheFolder, key + ".atlas");
	}

	public static byte[] Serialize(RadarAtlas atlas)
	{
		byte[] payload = Payload(atlas);
		atlas.Header.modelSha256 = RadarMapModelRegistry.Hex(RadarMapModelRegistry.Sha256(payload));
		byte[] json = Encoding.UTF8.GetBytes(JsonUtility.ToJson(atlas.Header, true));
		using (MemoryStream stream = new MemoryStream(payload.Length + Magic.Length + json.Length + 4))
		{
			using (BinaryWriter writer = new BinaryWriter(stream))
			{
				writer.Write(Magic);
				writer.Write(json.Length);
				writer.Write(json);
				writer.Write(payload);
				writer.Flush();
				return stream.ToArray();
			}
		}
	}

	private static byte[] Payload(RadarAtlas atlas)
	{
		byte[] payload = new byte[atlas.Spec.CellCount * 44];
		float[][] blocks = new float[3][] { atlas.CovarianceData, atlas.FluctuationData, atlas.MaximumData };
		int offset = 0;
		for (int i = 0; i < blocks.Length; i++)
		{
			Buffer.BlockCopy(blocks[i], 0, payload, offset, blocks[i].Length * 4);
			offset += blocks[i].Length * 4;
		}
		return payload;
	}

	public static RadarAtlas Deserialize(byte[] bytes, string expectedKey)
	{
		if (bytes == null || bytes.Length < Magic.Length + 4)
		{
			throw new RadarAtlasException("atlas file too short — re-bake");
		}
		for (int i = 0; i < Magic.Length; i++)
		{
			if (bytes[i] != Magic[i])
			{
				throw new RadarAtlasException("atlas magic mismatch — re-bake");
			}
		}
		uint jsonLength = BitConverter.ToUInt32(bytes, Magic.Length);
		if (jsonLength > bytes.Length - Magic.Length - 4)
		{
			throw new RadarAtlasException("atlas header length out of range — re-bake");
		}
		RadarAtlasHeader header;
		try
		{
			header = JsonUtility.FromJson<RadarAtlasHeader>(Encoding.UTF8.GetString(bytes, Magic.Length + 4, (int)jsonLength));
		}
		catch (Exception ex)
		{
			throw new RadarAtlasException("atlas header unreadable (" + ex.Message + ") — re-bake");
		}
		if (header == null || header.format != 1)
		{
			throw new RadarAtlasException("atlas format unsupported — re-bake");
		}
		if (expectedKey != null && header.key != expectedKey)
		{
			throw new RadarAtlasException("atlas key mismatch (" + header.key + " ≠ " + expectedKey + ") — re-bake");
		}
		if (!ulong.TryParse(header.seed, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong seed))
		{
			throw new RadarAtlasException("atlas seed unreadable — re-bake");
		}
		RadarAtlasSpec spec = new RadarAtlasSpec(header.deltaDeg, header.samplesPerSide, seed, header.solverTag);
		int cellCount = spec.CellCount;
		int offset = Magic.Length + 4 + (int)jsonLength;
		if (bytes.Length - offset != cellCount * 44)
		{
			throw new RadarAtlasException("atlas payload size mismatch — re-bake");
		}
		using (SHA256 sha = SHA256.Create())
		{
			string hex = RadarMapModelRegistry.Hex(sha.ComputeHash(bytes, offset, bytes.Length - offset));
			if (hex != header.modelSha256)
			{
				throw new RadarAtlasException("atlas payload SHA-256 mismatch — re-bake");
			}
		}
		float[] covariance = new float[cellCount * RadarAtlas.Floats];
		float[] fluctuation = new float[cellCount];
		float[] maximum = new float[cellCount];
		Buffer.BlockCopy(bytes, offset, covariance, 0, covariance.Length * 4);
		Buffer.BlockCopy(bytes, offset + covariance.Length * 4, fluctuation, 0, fluctuation.Length * 4);
		Buffer.BlockCopy(bytes, offset + (covariance.Length + cellCount) * 4, maximum, 0, maximum.Length * 4);
		return new RadarAtlas(spec, header, covariance, fluctuation, maximum);
	}

	public static void Write(string path, RadarAtlas atlas)
	{
		byte[] bytes = Serialize(atlas);
		Directory.CreateDirectory(Path.GetDirectoryName(path));
		string tmp = path + ".tmp";
		File.WriteAllBytes(tmp, bytes);
		if (!File.Exists(path))
		{
			File.Move(tmp, path);
		}
		else
		{
			File.Replace(tmp, path, null);
		}
	}

	public static RadarAtlas Read(string path, string expectedKey)
	{
		return Deserialize(File.ReadAllBytes(path), expectedKey);
	}
}
