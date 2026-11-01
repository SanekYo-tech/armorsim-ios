using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class RadarScenarioLaunchGame
{
	public enum LaunchOutcome
	{
		Launched = 0,
		NoLauncher = 1,
		NoContact = 2,
		Reloading = 3,
		Empty = 4,
		InvalidTarget = 5
	}

	public struct TrackedTarget
	{
		public bool Available;

		public Vector2 ObservedPosition;
	}

	public sealed class Projectile
	{
		public int LauncherId { get; internal set; }

		public int ContactNumber { get; internal set; }

		public int TargetId { get; internal set; }

		public Vector2 LaunchPosition { get; internal set; }

		public Vector2 TargetPosition { get; internal set; }

		public Vector2 CurrentPosition { get; internal set; }

		public float LaunchedAt { get; internal set; }

		public float Duration { get; internal set; }

		public bool Complete { get; internal set; }

		public bool Hit { get; internal set; }

		public bool Missed { get; internal set; }

		public bool ContactLost { get; internal set; }

		public float ArrivalAlpha { get; internal set; }
	}

	private sealed class Supply
	{
		public int Ammo;

		public float LastLaunch;
	}

	public const int InitialAmmo = 4;

	public const float ReloadSeconds = 3f;

	public const float AnimationSpeed = 35f;

	public const float TrackedFlightSeconds = 3f;

	public const int MaxProjectiles = 16;

	private readonly Dictionary<int, Supply> supplies;

	private readonly List<Projectile> projectiles;

	private readonly HashSet<int> destroyed;

	private float now;

	private bool resolverWarningLogged;

	public Func<int, TrackedTarget> ResolveTarget { get; set; }

	public IReadOnlyList<Projectile> Projectiles => projectiles;

	public int ActiveCount { get; private set; }

	public int ArrivedCount { get; private set; }

	public int HitCount { get; private set; }

	public int MissedCount { get; private set; }

	public int DestroyedCount => destroyed.Count;

	public RadarScenarioLaunchGame()
	{
		supplies = new Dictionary<int, Supply>();
		projectiles = new List<Projectile>();
		destroyed = new HashSet<int>();
	}

	public bool IsDestroyed(int targetId)
	{
		return destroyed.Contains(targetId);
	}

	public int AmmoRemaining(int launcherId)
	{
		if (supplies.TryGetValue(launcherId, out var value))
		{
			return value.Ammo;
		}
		return InitialAmmo;
	}

	public float CooldownRemaining(int launcherId, float time)
	{
		if (supplies.TryGetValue(launcherId, out var value))
		{
			return Mathf.Max(0f, ReloadSeconds - (time - value.LastLaunch));
		}
		return 0f;
	}

	public bool TryLaunch(RadarScenarioData data, int launcherId, bool hasContact, Vector2 observedTarget, int contactNumber, float time, out LaunchOutcome outcome)
	{
		return TryLaunchInternal(data, launcherId, hasContact, observedTarget, contactNumber, 0, time, out outcome);
	}

	public bool TryLaunchTracked(RadarScenarioData data, int launcherId, Vector2 observedTarget, int contactNumber, int targetId, float time, out LaunchOutcome outcome)
	{
		if (targetId <= 0)
		{
			outcome = LaunchOutcome.NoContact;
			return false;
		}
		return TryLaunchInternal(data, launcherId, true, observedTarget, contactNumber, targetId, time, out outcome);
	}

	private bool TryLaunchInternal(RadarScenarioData data, int launcherId, bool hasContact, Vector2 observedTarget, int contactNumber, int targetId, float time, out LaunchOutcome outcome)
	{
		outcome = LaunchOutcome.NoLauncher;
		if (data == null || data.Stations == null || !data.Stations.Exists((RadarScenarioStation s) => s.Id == launcherId && s.IsDefencePost))
		{
			return false;
		}
		if (!hasContact || contactNumber <= 0 || !RadarScenarioData.InsideMap(observedTarget) || !RadarScenarioData.Finite(time) || now > time)
		{
			outcome = LaunchOutcome.InvalidTarget;
			return false;
		}
		RadarScenarioStation station = data.Stations.Find((RadarScenarioStation s) => s.Id == launcherId && s.IsDefencePost);
		if (!RadarScenarioData.InsideMap(station.Position))
		{
			outcome = LaunchOutcome.InvalidTarget;
			return false;
		}
		if (!supplies.TryGetValue(launcherId, out var value))
		{
			value = new Supply();
			supplies.Add(launcherId, value);
		}
		if (value.Ammo == 0 || projectiles.Count >= MaxProjectiles)
		{
			outcome = LaunchOutcome.Empty;
			return false;
		}
		if (CooldownRemaining(launcherId, time) > 0f)
		{
			outcome = LaunchOutcome.Reloading;
			return false;
		}
		Tick(time);
		if (targetId > 0 && (IsDestroyed(targetId) || !TryObserve(targetId, out var _)))
		{
			outcome = LaunchOutcome.NoContact;
			return false;
		}
		value.Ammo--;
		value.LastLaunch = time;
		Projectile projectile = new Projectile
		{
			LauncherId = launcherId,
			ContactNumber = contactNumber,
			TargetId = targetId,
			LaunchPosition = station.Position,
			TargetPosition = observedTarget,
			CurrentPosition = station.Position,
			LaunchedAt = time,
			Duration = ((targetId > 0) ? TrackedFlightSeconds : Mathf.Max(0.4f, Vector2.Distance(station.Position, observedTarget) / AnimationSpeed))
		};
		projectiles.Add(projectile);
		ActiveCount++;
		outcome = LaunchOutcome.Launched;
		return true;
	}

	public void Tick(float time)
	{
		if (!RadarScenarioData.Finite(time) || now >= time)
		{
			return;
		}
		now = time;
		ActiveCount = 0;
		ArrivedCount = 0;
		HitCount = 0;
		MissedCount = 0;
		foreach (Projectile projectile in projectiles)
		{
			float num = Mathf.Max(0f, time - projectile.LaunchedAt);
			if (!projectile.Complete)
			{
				bool flag = false;
				if (projectile.TargetId > 0 && !IsDestroyed(projectile.TargetId) && TryObserve(projectile.TargetId, out TrackedTarget contact))
				{
					projectile.TargetPosition = contact.ObservedPosition;
					flag = true;
				}
				if (projectile.TargetId > 0 && !flag)
				{
					projectile.ContactLost = true;
				}
				float num2 = Mathf.Clamp01(Mathf.Clamp01(num / projectile.Duration));
				projectile.CurrentPosition = Vector2.Lerp(projectile.CurrentPosition, projectile.TargetPosition, num2);
				if (num >= projectile.Duration)
				{
					projectile.Complete = true;
					if (projectile.TargetId > 0)
					{
						projectile.Hit = flag && !projectile.ContactLost && destroyed.Add(projectile.TargetId);
						projectile.Missed = !projectile.Hit;
					}
				}
			}
			projectile.ArrivalAlpha = (projectile.Complete ? Mathf.Clamp01(1f - (num - projectile.Duration) * 0.5f) : 0f);
			if (!projectile.Complete)
			{
				ActiveCount++;
			}
			else
			{
				ArrivedCount++;
			}
			if (projectile.Hit)
			{
				HitCount++;
			}
			if (projectile.Missed)
			{
				MissedCount++;
			}
		}
	}

	private bool TryObserve(int targetId, out TrackedTarget contact)
	{
		contact = default(TrackedTarget);
		if (ResolveTarget == null)
		{
			return false;
		}
		try
		{
			contact = ResolveTarget(targetId);
			return contact.Available && RadarScenarioData.InsideMap(contact.ObservedPosition);
		}
		catch (Exception ex)
		{
			if (!resolverWarningLogged)
			{
				Debug.LogWarning("[RADAR GAME] Contact observation failed; tracked shot cannot score: " + ex.GetType().FullName);
				resolverWarningLogged = true;
			}
			return false;
		}
	}

	public void Reset()
	{
		supplies.Clear();
		projectiles.Clear();
		destroyed.Clear();
		ActiveCount = 0;
		ArrivedCount = 0;
		HitCount = 0;
		MissedCount = 0;
		now = 0f;
		resolverWarningLogged = false;
	}
}
