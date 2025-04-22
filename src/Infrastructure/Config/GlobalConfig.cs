using Microsoft.Extensions.Configuration;
using System.IO;

namespace Infrastructure.Config
{
    public static class GlobalConfig
    {
        private static readonly IConfigurationRoot _configuration ;

        static GlobalConfig()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            _configuration = builder.Build();
        }

        public static string CestaKuSuboru => _configuration["GlobalSettings:CestaKuSuboru"] ?? "";
    }
}
