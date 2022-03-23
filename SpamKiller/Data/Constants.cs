using System;
using System.Configuration;

namespace SpamKiller.Data
{
    /// <summary> Constant values needed for random things. </summary>
    public static class Constants
    {
        #region Constants
        /// <summary> The name to use for reporter roles. </summary>
        public const string ReporterRoleName = "Scam Reporter";

        /// <summary> Liru's Discord id. </summary>
        public static readonly ulong LiruId = ulong.Parse(ConfigurationManager.AppSettings["liruId"]);

#if DEBUG
        /// <summary> How long reporters have to wait between bans. </summary>
        public static readonly TimeSpan BanWaitTime = new (0, int.Parse(ConfigurationManager.AppSettings["testingBanTimeMinutes"]), 0);

        /// <summary> How long reporters have to wait between unbans. </summary>
        public static readonly TimeSpan UnbanWaitTime = new (0, int.Parse(ConfigurationManager.AppSettings["testingUnbanTimeMinutes"]), 0);
#else   
        /// <summary> How long reporters have to wait between bans. </summary>
        public static readonly TimeSpan BanWaitTime = new (0, int.Parse(ConfigurationManager.AppSettings["productionBanTimeMinutes"]), 0);

        /// <summary> How long reporters have to wait between unbans. </summary>
        public static readonly TimeSpan UnbanWaitTime = new (0, int.Parse(ConfigurationManager.AppSettings["productionUnbanTimeMinutes"]), 0);
#endif
        #endregion
    }
}