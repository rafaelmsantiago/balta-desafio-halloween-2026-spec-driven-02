using LinkShortener.Api.Application;
using LinkShortener.Api.Data;
using LinkShortener.Api.Endpoints;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddDbContext<LinksDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Links")));
builder.Services.AddScoped<ILinkRepository, LinkRepository>();
builder.Services.AddScoped<LinkService>();
builder.Services.AddScoped<PublicHostChecker>();
builder.Services.AddSingleton<IHostResolver, DnsHostResolver>();
builder.Services.AddSingleton<ICodeGenerator, RandomCodeGenerator>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<LinksDbContext>().Database.Migrate();
}

// D-13: documento OpenAPI e interface Scalar só em desenvolvimento.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithTitle("Encurtador de links"));
}

app.MapLinkEndpoints();

app.Run();

public partial class Program;
