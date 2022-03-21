using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;

namespace SpamKiller
{
    internal class Program
    {
        // Get the token.
#if DEBUG
        private static readonly string tokenPath = ConfigurationManager.AppSettings["testingTokenFile"] ?? throw new Exception("Missing bot token.");
#else
        private static readonly string tokenPath = ConfigurationManager.AppSettings["productionTokenFile"] ?? throw new Exception("Missing bot token.");
#endif

        static void Main(string[] args) => runMain(args).Wait();

        private static async Task runMain(string[] args)
        {
            // Read the token.
            string token = await File.ReadAllTextAsync(tokenPath);

            // Create the bot.
            MainBot bot = new(token);

            // Log the bot in.
            await bot.LoginAndStartAsync();

            await Task.Delay(-1);
        }
    }
}
