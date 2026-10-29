using LinkShortener.Api.Application;
using LinkShortener.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LinkShortener.Tests.Integration;

// Sobe a API sobre um SQLite próprio, sem DNS real e com relógio controlado.
public sealed class LinkShortenerFactory(string connectionString) : WebApplicationFactory<Program>
{
    public FakeHostResolver HostResolver { get; set; } = new("93.184.216.34");

    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 10, 29, 12, 0, 0, TimeSpan.Zero));

    public ICodeGenerator? CodeGenerator { get; set; }

    public string Environment { get; set; } = Environments.Development;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environment);
        builder.UseSetting("ConnectionStrings:Links", connectionString);

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IHostResolver>(_ => HostResolver);
            services.AddSingleton<TimeProvider>(Clock);

            if (CodeGenerator is not null)
                services.AddSingleton(CodeGenerator);
        });
    }
}
