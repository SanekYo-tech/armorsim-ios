public enum WorkshopReadStatus
{
	Ok = 1,
	RefusedNoManifest = 2,
	RefusedNoVersion = 3,
	RefusedNewer = 4,
	RefusedMalformed = 5,
	RefusedFamilyMismatch = 6,
	RefusedNewerPayload = 7
}
