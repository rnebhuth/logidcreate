// See https://aka.ms/new-console-template for more information

using CommandLine;
using LogIdCreate.Core.Cmd.Executers;
using LogIdCreate.Core.Cmd.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace LogIdCreate.Core.Cmd
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var serviceProvider = InitDependencyInjection();

            var optionTypes = typeof(Program).Assembly.GetTypes().Where(a => a.GetCustomAttributes<VerbAttribute>().Count() > 0).ToArray();
            var parsedArguments = Parser.Default.ParseArguments(args, optionTypes);

            await parsedArguments.WithParsedAsync<InitOptions>(async o =>
            {
                var executer = serviceProvider.GetRequiredService<InitExecuter>();
                await executer.Run(o);
            });

            await parsedArguments.WithParsedAsync<CreateIdsOptions>(async o =>
            {
                var executer = serviceProvider.GetRequiredService<CreateIdExecuter>();
                await executer.Run(o);
            });

            //var runner = new Runner();
            //await runner.RunAsync();

        }

        private static IServiceProvider InitDependencyInjection()
        {
            var configuration = new ConfigurationBuilder()
                 .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                 .AddJsonFile("appsettings.json", false)
                 .Build();

            var serviceCollection = new ServiceCollection()
                .AddLogging()
                .AddSingleton<InitExecuter>()
                .AddSingleton<CreateIdExecuter>()
                .AddSingleton(configuration.Get<LogIdConfig>())
                .AddSingleton<IConfiguration>(configuration);

            return serviceCollection.BuildServiceProvider(); ;
        }
    }
}

