using AuxiliumAPI.Common.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using NetEscapades.Configuration.Yaml;

namespace AuxiliumAPI
{
    public class AuxiliumDbContextFactory : IDesignTimeDbContextFactory<AuxiliumDbContext>
    {
        public AuxiliumDbContext CreateDbContext(string[] args)
        {
            var configPath = Environment.GetEnvironmentVariable("AUXILIUM_CONFIG_PATH") ?? "\\\\files.wraitheon.net\\Projects\\Auxilium\\aux3-dev.yaml";

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddYamlFile(configPath, optional: false, reloadOnChange: false)
                .Build();

            var mariaDbHost = configuration["Databases:MariaDB:Host"]           ?? throw new InvalidOperationException("MariaDB Host not found");
            var mariaDbPort = configuration["Databases:MariaDB:Port"]           ?? throw new InvalidOperationException("MariaDB Port not found");
            var mariaDbUsername = configuration["Databases:MariaDB:Username"]   ?? throw new InvalidOperationException("MariaDB Username not found");
            var mariaDbPassword = configuration["Databases:MariaDB:Password"]   ?? throw new InvalidOperationException("MariaDB Password not found");
            var mariaDbDatabase = configuration["Databases:MariaDB:Database"]   ?? throw new InvalidOperationException("MariaDB Database not found");

            var connectionString = $"Server={mariaDbHost};Port={mariaDbPort};Database={mariaDbDatabase};User={mariaDbUsername};Password={mariaDbPassword};CharSet=utf8mb4;";

            var optionsBuilder = new DbContextOptionsBuilder<AuxiliumDbContext>();
            optionsBuilder.UseMySql(
                connectionString,
                ServerVersion.AutoDetect(connectionString)
            );

            return new AuxiliumDbContext(optionsBuilder.Options);
        }
    }
}
