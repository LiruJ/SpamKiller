using System;
using System.Configuration;

namespace SpamKiller.Data
{
    public static class Constants
    {
        #region Constants
        public const string ReporterRoleName = "Scam Reporter";

        public static readonly ulong LiruId = ulong.Parse(ConfigurationManager.AppSettings["liruId"]);

#if DEBUG
        public static readonly TimeSpan BanWaitTime = new (0, int.Parse(ConfigurationManager.AppSettings["testingBanTimeMinutes"]), 0);
        public static readonly TimeSpan UnbanWaitTime = new (0, int.Parse(ConfigurationManager.AppSettings["testingUnbanTimeMinutes"]), 0);
#else   
        public static readonly TimeSpan BanWaitTime = new (0, int.Parse(ConfigurationManager.AppSettings["productionBanTimeMinutes"]), 0);
        public static readonly TimeSpan UnbanWaitTime = new (0, int.Parse(ConfigurationManager.AppSettings["productionUnbanTimeMinutes"]), 0);
#endif
        #endregion
    }
}
