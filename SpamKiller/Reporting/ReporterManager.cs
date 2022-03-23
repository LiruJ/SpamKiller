using Discord.WebSocket;
using SpamKiller.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SpamKiller.Reporting
{
    /// <summary> Manages the database of reporters. </summary>
    public class ReporterManager
    {
        #region Dependencies
        private readonly DataContext context;
        #endregion

        #region Constructors
        public ReporterManager(DataContext context)
        {
            this.context = context;
        }
        #endregion

        #region Reporter Functions
        /// <summary> Gets the <see cref="ScamReporter"/> associated with the given <paramref name="user"/>, or <c>null</c> if the user is not a reporter in the server. </summary>
        /// <param name="user"> The user to check. </param>
        /// <returns> The <see cref="ScamReporter"/> data of the given <paramref name="user"/>, or <c>null</c> if the user is not a reporter in the server. </returns>
        public async Task<ScamReporter> GetScamReporterAsync(SocketGuildUser user)
            => await context.ScamReporters.FirstOrDefaultAsync(x => x.UserId == user.Id && x.ServerId == user.Guild.Id);
        #endregion
    }
}