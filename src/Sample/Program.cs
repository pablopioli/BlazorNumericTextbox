using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using PeterLeslieMorris.Blazor.Validation;
using System;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;

namespace Sample
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.DefaultThreadCurrentCulture;

            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("app");
            builder.Services.AddTransient(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            builder.Services.AddFormValidation(config => config.AddFluentValidation(typeof(Program).Assembly));

            await builder.Build().RunAsync();
        }
    }
}
