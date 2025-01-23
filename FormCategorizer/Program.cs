using FormCategorizer.Models;
using FormCategorizer.Records;
using FormCategorizer.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace FormCategorizer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            IHostBuilder builder = Host.CreateDefaultBuilder(args)
                                        .ConfigureAppConfiguration((context, config) => 
                                        {
                                            var env = context.HostingEnvironment;

                                            if (env.IsDevelopment())
                                            {
                                                config.AddUserSecrets(Assembly.GetExecutingAssembly());
                                            }
                                        })
                                        .ConfigureServices((hostContext, services) =>
                                        {
                                            IConfiguration config = hostContext.Configuration;
                                            
                                            services.AddHostedService<CategorizationHostService>();

                                            services
                                                .AddOptions<FileReaderSettings>()
                                                .Bind(config.GetRequiredSection("FileReaderSettings"))
                                                .Configure(options =>
                                                {
                                                    var fileExtensionsString = config.GetSection("FileReaderSettings:AllowedImageExtensions").Value;

                                                    if (!string.IsNullOrEmpty(fileExtensionsString))
                                                    {
                                                        options.AllowedImageExtensions = fileExtensionsString.Split(',').ToList();
                                                    }
                                                });

                                                    services
                                                .AddOptions<AzureOpenAISettings>()
                                                .Bind(config.GetRequiredSection("AzureOpenAISettings"));

                                            services
                                                .AddOptions<DocumentIntelligenceSettings>()
                                                .Bind(config.GetRequiredSection("DocumentIntelligenceSettings"));

                                        });

            using IHost host = builder.Build();
            await host.RunAsync();
        }
    }
}