using PlayerSignupBlazor.Helpers;
using PlayerSignupBlazor.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace PlayerSignupBlazor
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("app");

            builder.Services
                .AddScoped<IPlayerService, PlayerService>()
                .AddScoped<IAlertService, AlertService>()
                .AddScoped<IHttpService, HttpService>()
                .AddScoped<ILocalStorageService, LocalStorageService>();

            // configure http client
            builder.Services.AddScoped(x => {
                var apiUrl = new Uri(builder.Configuration["apiUrl"]);

                // use fake backend if "mockBackend" is "true" in appsettings.json
                if (builder.Configuration["mockBackend"] == "true")
                {
                    var mockBackendHandler = new FakeBackendHandler(x.GetService<ILocalStorageService>());
                    return new HttpClient(mockBackendHandler) { BaseAddress = apiUrl };
                }

                return new HttpClient() { BaseAddress = apiUrl };
            });

            var host = builder.Build();

            var accountService = host.Services.GetRequiredService<IPlayerService>();
            await accountService.Initialize();

            await host.RunAsync();
        }
    }
}