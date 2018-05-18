﻿using System;
using System.Text;
using Kodkod.Application;
using Kodkod.Core.AppConsts;
using Kodkod.Core.Entities;
using Kodkod.EntityFramework;
using Kodkod.Web.Api.ActionFilters;
using Kodkod.Web.Api.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Swagger;

namespace Kodkod.Web.Api
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        private readonly SymmetricSecurityKey _signingKey;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
            _signingKey =
                new SymmetricSecurityKey(
                    Encoding.ASCII.GetBytes(_configuration["Authentication:JwtBearer:SecurityKey"]));
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<KodkodDbContext>(options =>
                options.UseSqlServer(_configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<ApplicationUser, ApplicationRole>()
                .AddEntityFrameworkStores<KodkodDbContext>()
                .AddDefaultTokenProviders();

            services.AddSphinxEntityFramework();
            services.AddKodkodApplication();

            services.Configure<JwtTokenConfiguration>(options =>
            {
                options.Issuer = _configuration["Authentication:JwtBearer:Issuer"];
                options.Audience = _configuration["Authentication:JwtBearer:Audience"];
                options.SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
            });

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(jwtBearerOptions =>
            {
                jwtBearerOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateActor = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _configuration["Authentication:JwtBearer:Issuer"],
                    ValidAudience = _configuration["Authentication:JwtBearer:Audience"],
                    IssuerSigningKey = _signingKey
                };
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy(KodkodPolicies.ApiUser,
                    policy =>
                    {
                        policy.RequireClaim(KodkodClaimTypes.ApiUserRole, KodkodClaimValues.ApiAccess);
                    });
            });

            services.AddMvc(options => options.Filters.Add<KodkodDbContextActionFilter>());

            services.AddCors();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "My API", Version = "v1" });
            });

            services.AddTransient<KodkodDbContextActionFilter>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });

            app.UseCors(builder =>
                builder.WithOrigins(_configuration["App:CorsOrigins"]
                    .Split(",", StringSplitOptions.RemoveEmptyEntries)));

            app.UseAuthentication();

            app.UseMvc();
        }
    }
}