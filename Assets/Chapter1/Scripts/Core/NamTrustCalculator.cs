namespace DormitoryMystery.Chapter1
{
    public static class NamTrustCalculator
    {
        public static int ApplyChoice(int currentTrust, HardDriveChoice choice)
        {
            return ClampTrust(currentTrust + GetTrustDelta(choice));
        }

        public static int GetTrustDelta(HardDriveChoice choice)
        {
            switch (choice)
            {
                case HardDriveChoice.ReturnIntact:
                    return 30;
                case HardDriveChoice.CopyBeforeReturning:
                    return 0;
                case HardDriveChoice.HideMorisTracking:
                    return -15;
                case HardDriveChoice.ForceNamCooperation:
                    return -30;
                default:
                    return 0;
            }
        }

        public static int ClampTrust(int namTrust)
        {
            if (namTrust < 0)
            {
                return 0;
            }

            if (namTrust > 100)
            {
                return 100;
            }

            return namTrust;
        }

        public static NamRelationshipLevel GetRelationshipLevel(int namTrust)
        {
            int clampedTrust = ClampTrust(namTrust);

            if (clampedTrust <= 29)
            {
                return NamRelationshipLevel.Distrustful;
            }

            if (clampedTrust <= 69)
            {
                return NamRelationshipLevel.Conditional;
            }

            return NamRelationshipLevel.FullTrust;
        }
    }
}
