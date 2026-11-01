using System;

public static class AeroFreeAccess
{
	public const bool Permanent = true;

	public static readonly DateTimeOffset StartsUtc = new DateTimeOffset(2026, 9, 16, 17, 0, 0, TimeSpan.Zero);

	public static readonly DateTimeOffset EndsUtc = new DateTimeOffset(2026, 9, 23, 17, 0, 0, TimeSpan.Zero);

	public static bool IsActiveNow => IsActive(DateTimeOffset.UtcNow);

	public static bool IsActive(DateTimeOffset now)
	{
		if (Permanent)
		{
			return true;
		}
		return now >= StartsUtc && now < EndsUtc;
	}

	public static AeroFacts ReadFacts()
	{
		return new AeroFacts
		{
			implemented = true,
			ownership = Entitlements.State(ContentPack.Aero),
			gateOpen = true,
			gateReason = null,
			demoAvailable = false,
			freeAccess = IsActiveNow
		};
	}
}
