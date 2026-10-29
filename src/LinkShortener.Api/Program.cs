using LinkShortener.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<LinksDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Links")));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<LinksDbContext>().Database.Migrate();
}

app.Run();

public partial class Program;
