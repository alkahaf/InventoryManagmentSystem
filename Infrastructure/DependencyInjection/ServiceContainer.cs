using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Extension.Identity;
using Infrastructure.DataAccess;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.DependencyInjection
{
    public static class ServiceContainer
    {
        public static IServiceCollection AddInfrastructureService(this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<AppDbContext>(o => o.UseSqlServer(config.GetConnectionString("DefaultConnection")),ServiceLifetime.Scoped);

            services.AddAuthentication(options =>
            {
                options.DefaultScheme=IdentityConstants.ApplicationScheme;
                options.DefaultScheme = IdentityConstants.ExternalScheme;
            }).AddIdentityCookies();
            services.AddIdentityCore<ApplicationUser>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

            services.AddAuthorizationBuilder()
                .AddPolicy("AdministrationPolicy", adp =>
                {
                    adp.RequireAuthenticatedUser();
                    adp.RequireRole("Admin", "Manager");

                }).AddPolicy("UserPolicy", adp =>
                {
                    adp.RequireAuthenticatedUser();
                    adp.RequireRole("User");
                });
            //services.AddCascadingAuthenticationState();
            //services.AddScoped<Application.Interface.Identity.IAccount, Account>();
            //services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(CreateProductHandler).Assembly));
            //services.AddScoped<DataAccess.IDbContextFactory<AppDbContext>, DbContextFactory<AppDbContext>>();
            return services;
        }
    }
}
