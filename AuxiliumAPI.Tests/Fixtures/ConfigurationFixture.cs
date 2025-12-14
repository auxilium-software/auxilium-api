using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuxiliumAPI.Tests.Fixtures
{
    public class ConfigurationFixture : IDisposable
    {
        public IConfiguration Configuration { get; private set; }

        public ConfigurationFixture()
        {
            var configPath = Environment.GetEnvironmentVariable("AUXILIUM_CONFIG_PATH")
                ?? Environment.GetEnvironmentVariable("AUXILIUM_TEST_CONFIG_PATH")
                ?? "\\\\files.wraitheon.net\\Projects\\Auxilium\\aux3-test.yaml";

            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory());

            if (File.Exists(configPath))
            {
                configBuilder.AddYamlFile(configPath, optional: false);
            }
            else
            {
                throw new FileNotFoundException($"Configuration file not found at: {configPath}.  Set the `AUXILIUM_CONFIG_PATH` or `AUXILIUM_TEST_CONFIG_PATH` environment variable.");
            }

            Configuration = configBuilder.Build();
        }

        public void Dispose() { }
    }
}
