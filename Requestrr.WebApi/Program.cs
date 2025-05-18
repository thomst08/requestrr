using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Requestrr.WebApi.RequestrrBot;
using Requestrr.WebApi.RequestrrBot.Locale;

namespace Requestrr.WebApi
{
    public class Program
    {
        public static int Port = 4545;
        public static string BaseUrl = string.Empty;

        public static void Main(string[] args)
        {
            string cliBaseUrl = Environment.GetEnvironmentVariable("REQUESTRR_BASE_URL");
            int cliPort = -1;

            if (int.TryParse(Environment.GetEnvironmentVariable("REQUESTRR_PORT"), out int portFromEnv))
            {
                cliPort = portFromEnv;
            }

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--help":
                    case "-h":
                        Console.WriteLine($"Requestrr version: {Language.BuildVersion}");
                        Console.WriteLine("Description:");
                        Console.WriteLine("  A chatbot used to connectservices like Sonarr/Radarr/Overseerr/Ombi to Discord\n");
                        Console.WriteLine("Options:");
                        Console.WriteLine("  -h, --help           Displays the help message and exits the program");
                        Console.WriteLine("  -c, --config-dir     Change the config folder");
                        Console.WriteLine("                       Example: Requestrr.WebApi.exe -c \"C:\\Requestrr\\config\"");
                        Console.WriteLine("                                Requestrr.WebApi -c /opt/Requestrr/config");
                        Console.WriteLine("                                Requestrr.WebApi.exe -c ./config");
                        Console.WriteLine("  -p, --port           Change the port of Requestrr, this will update the config file");
                        Console.WriteLine("                       This allows for the changing of the port used for Requestrr, eg: http://localhost:port");
                        Console.WriteLine("                       Example: Requestrr.WebApi.exe -p 4546");
                        Console.WriteLine("                                Requestrr.WebApi --port 4547");
                        Console.WriteLine("  -u, --base-url       Change the base URL of Requestrr, this will update the config file");
                        Console.WriteLine("                       This allows the changing of the base URL to access Requestrr, eg: http://localhost:4545/baseURL");
                        Console.WriteLine("                       Example: Requestrr.WebApi.exe -u \"/requestrr\"");
                        Console.WriteLine("                                Requestrr.WebApi --base-url \"/\"");
                        Console.WriteLine("                                Requestrr.WebApi.exe -u \"\"");
                        return;
                    case "--config-dir":
                    case "-c":
                        try
                        {
                            SettingsFile.SettingsFolder = args[++i];
                            SettingsFile.CommandLineSettings = true;
                        }
                        catch
                        {
                            Console.WriteLine("Error: Missing argument, config director path missing");
                            return;
                        }
                        break;
                    case "--port":
                    case "-p":
                        try
                        {
                            cliPort = int.Parse(args[++i]);
                            if (cliPort < 0 || cliPort > 65535)
                                throw new Exception("Invalid port number");
                        }
                        catch
                        {
                            Console.WriteLine("Error: Missing argument, port needs to include a number between 0 to 65535");
                            return;
                        }
                        break;
                    case "--base-url":
                    case "-u":
                        try
                        {
                            cliBaseUrl = args[++i];
                            if (cliBaseUrl == "/")
                                cliBaseUrl = string.Empty;
                            else if (cliBaseUrl[cliBaseUrl.Length - 1] == '/')
                                throw new Exception("End slash");
                        }
                        catch (Exception ex)
                        when (ex.Message == "End slash")
                        {
                            Console.WriteLine("Error: Base URL cannot end in a slash '/'");
                            return;
                        }
                        catch
                        {
                            Console.WriteLine("Error: Missing argument, URL is missing");
                            return;
                        }
                        break;
                }
            }

            try
            {
                if (!SettingsFile.CommandLineSettings)
                {
                    var config = new ConfigurationBuilder()
#if DEBUG
                        .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true)
#else
                        .AddJsonFile(CombindPath("appsettings.json"), optional: false, reloadOnChange: true)
#endif
                        .Build();
                    SettingsFile.SettingsFolder = config.GetValue<string>("ConfigFolder");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error reading config folder location.  Using default location.");
                Console.WriteLine(e.Message);
                SettingsFile.SettingsFolder = "./config";
            }

            UpdateSettingsFile();
            SetLanguage();

            if (cliPort != -1 && cliPort != (int)SettingsFile.Read().Port)
            {
                Console.WriteLine("Changing port from cli arguments...");
                SettingsFile.Write(settings =>
                {
                    settings.Port = cliPort;
                });
            }

            if (!string.IsNullOrEmpty(cliBaseUrl) && cliBaseUrl != (string)SettingsFile.Read().BaseUrl)
            {
                if (cliBaseUrl == "/")
                    cliBaseUrl = string.Empty;
                else if (cliBaseUrl.EndsWith("/"))
                {
                    Console.WriteLine("Error: Base URL cannot end in a slash '/'");
                    return;
                }

                Console.WriteLine("Changing base url from environment or CLI arguments...");
                SettingsFile.Write(settings =>
                {
                    settings.BaseUrl = cliBaseUrl;
                });
            }

            Port = (int)SettingsFile.Read().Port;
            BaseUrl = SettingsFile.Read().BaseUrl;

            CreateWebHostBuilder(args).Build().Run();
        }

        private static void UpdateSettingsFile()
        {
            if (!Directory.Exists(SettingsFile.SettingsFolder))
            {
                Console.WriteLine("No config folder found, creating one...");
                Directory.CreateDirectory(SettingsFile.SettingsFolder);
            }

            try
            {
                if (!File.Exists(SettingsFile.FilePath))
                {
                    File.WriteAllText(SettingsFile.FilePath, File.ReadAllText(CombindPath("SettingsTemplate.json")).Replace("[PRIVATEKEY]", Guid.NewGuid().ToString()));
                }
                else
                {
                    SettingsFileUpgrader.Upgrade(SettingsFile.FilePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write to config folder: {ex.Message}");
                throw new Exception($"No config file to load and cannot create one.  Bot cannot start.");
            }


            if (!File.Exists(NotificationsFile.FilePath))
            {
                File.WriteAllText(NotificationsFile.FilePath, File.ReadAllText(CombindPath("NotificationsTemplate.json")));
            }
        }


        /// <summary>
        /// Combinds the pasted in path and connects to the location of the executable
        /// and returns the full path for the directory.
        /// </summary>
        /// <param name="path">String of the path relitive to the executable</param>
        /// <returns>Returns the full path to the file/directory</returns>
        public static string CombindPath(string path)
        {
            return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), path));
        }

        private static void SetLanguage()
        {
            string path = CombindPath($"locales/{SettingsFile.Read().ChatClients.Language}.json");
            Language.Current = JsonConvert.DeserializeObject<Language>(File.ReadAllText(path));
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
#if !DEBUG
                //Used to link local files relitive to the executable, not the executed directory of the user.
                .UseContentRoot(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location))
#endif
                .UseUrls($"http://*:{Port}")
                .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddJsonFile(SettingsFile.FilePath, optional: false, reloadOnChange: true);
            });
    }
}
