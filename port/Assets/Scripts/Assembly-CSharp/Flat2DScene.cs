using System;
using UnityEngine;

public static class Flat2DScene
{
	public struct Body
	{
		public Vector2[] pos;

		public Vector2[] vel;

		public uint[] mat;

		public float dx;

		public Vector2 min;

		public Vector2 max;
	}

	public static Body Block(int nx, int ny, float dx, Vector2 origin, uint material, Vector2 velocity)
	{
		int count = nx * ny;
		Body b = default(Body);
		b.pos = new Vector2[count];
		b.vel = new Vector2[count];
		b.mat = new uint[count];
		b.dx = dx;
		b.min = origin;
		b.max = new Vector2(nx * dx + origin.x, ny * dx + origin.y);
		for (int row = 0; row < ny; row++)
		{
			for (int col = 0; col < nx; col++)
			{
				int idx = row * nx + col;
				b.pos[idx] = new Vector2((col + 0.5f) * dx + origin.x, (row + 0.5f) * dx + origin.y);
				b.vel[idx] = velocity;
				b.mat[idx] = material;
			}
		}
		return b;
	}

	public static Body Piston(int nx, int ny, float dx, uint material, float v0, int pistonCols)
	{
		Body b = Block(nx, ny, dx, Vector2.zero, material, Vector2.zero);
		for (int i = 0; i < b.pos.Length; i++)
		{
			if (pistonCols * dx > b.pos[i].x)
			{
				b.vel[i] = new Vector2(v0, 0f);
			}
		}
		return b;
	}

	public static Body TwoBlocks(int nx, int ny, float dx, uint matLeft, uint matRight, float speed, float gap)
	{
		Body a = Block(nx, ny, dx, Vector2.zero, matLeft, new Vector2(speed, 0f));
		Body c = Block(nx, ny, dx, new Vector2(nx * dx + gap, 0f), matRight, new Vector2(-speed, 0f));
		Body b = default(Body);
		int count = a.pos.Length + c.pos.Length;
		b.pos = new Vector2[count];
		b.vel = new Vector2[count];
		b.mat = new uint[count];
		b.dx = dx;
		a.pos.CopyTo(b.pos, 0);
		c.pos.CopyTo(b.pos, a.pos.Length);
		a.vel.CopyTo(b.vel, 0);
		c.vel.CopyTo(b.vel, a.vel.Length);
		a.mat.CopyTo(b.mat, 0);
		c.mat.CopyTo(b.mat, a.mat.Length);
		b.min = a.min;
		b.max = c.max;
		return b;
	}

	public static Body TaylorPair(float lengthM, float diameterM, float dx, uint material, float impactSpeed)
	{
		int nx = Convert.ToInt32(lengthM / dx);
		if (nx < 1)
		{
			nx = 1;
		}
		int ny = Convert.ToInt32(diameterM / dx);
		if (ny < 1)
		{
			ny = 1;
		}
		float gap = dx * 2.73f;
		Body rod = Block(nx, ny, dx, Vector2.zero, material, new Vector2(impactSpeed, 0f));
		Body target = Block(nx, ny, dx, new Vector2(nx * dx + gap, 0f), material, new Vector2(-impactSpeed, 0f));
		return Join(rod, target, dx);
	}

	public static Body Crater(int plateNx, int plateNy, int rodNx, int rodNy, float dx, uint plateMat, uint rodMat, float speed)
	{
		Body plate = Block(plateNx, plateNy, dx, Vector2.zero, plateMat, Vector2.zero);
		Body rod = Block(rodNx, rodNy, dx, new Vector2((plateNx - rodNx) * 0.5f * dx, plateNy * dx + dx * 2.73f), rodMat, new Vector2(0f, -speed));
		return Join(plate, rod, dx);
	}

	public static Body TwoMaterials(int nx, int ny, float dx, uint matLeft, uint matRight, float speedLeft, float speedRight, float gap)
	{
		Body a = Block(nx, ny, dx, Vector2.zero, matLeft, new Vector2(speedLeft, 0f));
		Body c = Block(nx, ny + 6, dx, new Vector2(nx * dx + gap, 0.002f), matRight, new Vector2(speedRight, 0f));
		return Join(a, c, dx);
	}

	private static Body Join(Body a, Body c, float dx)
	{
		Body b = default(Body);
		int count = a.pos.Length + c.pos.Length;
		b.pos = new Vector2[count];
		b.vel = new Vector2[count];
		b.mat = new uint[count];
		b.dx = dx;
		a.pos.CopyTo(b.pos, 0);
		c.pos.CopyTo(b.pos, a.pos.Length);
		a.vel.CopyTo(b.vel, 0);
		c.vel.CopyTo(b.vel, a.vel.Length);
		a.mat.CopyTo(b.mat, 0);
		c.mat.CopyTo(b.mat, a.mat.Length);
		b.min = new Vector2(a.min.x > c.min.x ? c.min.x : a.min.x, a.min.y > c.min.y ? c.min.y : a.min.y);
		b.max = new Vector2(a.max.x > c.max.x ? a.max.x : c.max.x, a.max.y > c.max.y ? a.max.y : c.max.y);
		return b;
	}

	public static void Domain(Body b, float margin, out Vector2 min, out Vector2 max)
	{
		min = new Vector2(float.MaxValue, float.MaxValue);
		max = new Vector2(float.MinValue, float.MinValue);
		for (int i = 0; i < b.pos.Length; i++)
		{
			min = new Vector2(b.pos[i].x > min.x ? min.x : b.pos[i].x, b.pos[i].y > min.y ? min.y : b.pos[i].y);
			max = new Vector2(max.x > b.pos[i].x ? max.x : b.pos[i].x, max.y > b.pos[i].y ? max.y : b.pos[i].y);
		}
		min = new Vector2(min.x - margin, min.y - margin);
		max = new Vector2(max.x + margin, max.y + margin);
	}
}
