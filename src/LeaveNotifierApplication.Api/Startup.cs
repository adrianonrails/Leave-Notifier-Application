﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using LeaveNotifierApplication.Data;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using LeaveNotifierApplication.Data.Models;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Swashbuckle.AspNetCore.Swagger;
using LeaveNotifierApplication.Api.Filters;
using Microsoft.Extensions.PlatformAbstractions;
using System.IO;

namespace LeaveNotifierApplication.Api
{
    public class Startup
    {
        private IHostingEnvironment _env;
        public IConfigurationRoot _config { get; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("config.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"config.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            _config = builder.Build();

            _env = env;
        }


        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add the application wide config file
            services.AddSingleton(_config);

            // Add the db stuffs
            services.AddDbContext<LeaveNotifierDbContext>(ServiceLifetime.Scoped);
            services.AddScoped<ILeaveNotifierRepository, LeaveNotifierRepository>();
            services.AddTransient<LeaveNotifierDbContextSeedData>();

            // Add our mapper
            services.AddAutoMapper();

            // Add the Identity
            services.AddIdentity<LeaveNotifierUser, IdentityRole>()
                .AddEntityFrameworkStores<LeaveNotifierDbContext>();

            services.Configure<IdentityOptions>(config =>
            {
                config.Cookies.ApplicationCookie.Events = new CookieAuthenticationEvents()
                {
                    OnRedirectToLogin = (ctx) =>
                    {
                        if (ctx.Request.Path.StartsWithSegments("/api") && ctx.Response.StatusCode == 200)
                        {
                            ctx.Response.StatusCode = 401;
                        }

                        return Task.CompletedTask;
                    },

                    OnRedirectToAccessDenied = (ctx) =>
                    {
                        if (ctx.Request.Path.StartsWithSegments("/api") && ctx.Response.StatusCode == 200)
                        {
                            ctx.Response.StatusCode = 403;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

            // Add authorization configs
            services.AddAuthorization(cfg =>
            {
                cfg.AddPolicy("SuperUsers", p => p.RequireClaim("SuperUser", "True"));
            });

            // Add CORS
            services.AddCors();

            // Add framework services.
            services.AddMvc();

            // Register the Swagger generator
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Info
                {
                    Version = "v1",
                    Title = "LeaveNotifier API",
                    Description = "API Usage",
                    TermsOfService = "None"
                });

                // Set comments
                var basePath = PlatformServices.Default.Application.ApplicationBasePath;
                var xmlPath = Path.Combine(basePath, "LeaveNotifierApplication.Api.xml");
                options.IncludeXmlComments(xmlPath);

                options.OperationFilter<SwaggerOperationFilter>();
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app,
            IHostingEnvironment env,
            ILoggerFactory loggerFactory,
            LeaveNotifierDbContextSeedData seeder)
        {
            loggerFactory.AddConsole(_config.GetSection("Logging"));
            loggerFactory.AddDebug();

            // Use the identity
            app.UseIdentity();

            // Middleware for JWT Authentication
            app.UseJwtBearerAuthentication(new JwtBearerOptions()
            {
                AutomaticAuthenticate = true,
                AutomaticChallenge = true,
                TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidIssuer = _config["Tokens:Issuer"],
                    ValidAudience = _config["Tokens:Audience"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Tokens:Key"])),
                    ValidateLifetime = true
                }
            });

            // Use CORS
            app.UseCors(cfg =>
            {
                cfg.AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowAnyOrigin();
            });

            app.UseMvc();

            // Enable swagger file
            app.UseSwagger();

            // Enable the swagger UI (this should be dev only)
            app.UseSwaggerUi(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
            });

            seeder.EnsureSeedData().Wait();
        }
    }
}
