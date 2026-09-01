using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PetFlowAPI.Data;

namespace PetFlowAPI.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<PetFlowContext>();
            services.RemoveAll<DbContextOptions<PetFlowContext>>();
            services.AddDbContext<PetFlowContext>(options => options.UseInMemoryDatabase("PetFlowIntegrationTests"));
        });
    }
}
