using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using TwitchChatBot.Client.Areas.Identity;
using TwitchChatBot.Client.Data;
using TwitchChatBot.Client.Hubs;
using TwitchChatBot.Client.Models.Options;
using TwitchChatBot.Client.Services;
using Microsoft.Extensions.Azure;
using Azure.Storage.Queues;
using Azure.Storage.Blobs;
using Azure.Core.Extensions;
using System;
using Blazored.Modal;

namespace TwitchChatBot.Client
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            // Adds Identity database 
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection")));
            services.AddDefaultIdentity<IdentityUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
            })
                .AddEntityFrameworkStores<ApplicationDbContext>();
            services.AddRazorPages();

            // Adds ServerSideBlazor
            services.AddServerSideBlazor();
            // Adds Authentication
            services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();
            // Adds API support for webhooks
            services.AddControllers();

            // Add Services to IOC
            services.AddSingleton<IStorageService, AzureTableStorageService>();
            services.AddSingleton<ITwitchService, TwitchService>();

            // Add options for reading settings
            services.Configure<TwitchOptions>(Configuration.GetSection("Twitch"));
            services.Configure<OAuthOptions>(Configuration.GetSection("OAuth"));
            services.Configure<TableStorageOptions>(Configuration.GetSection("TableStorage"));
            services.Configure<BotOptions>(Configuration.GetSection("Bot"));

            // Adds a Twitch HttpClient
            services.AddHttpClient<TwitchHttpClient>();

            // Add Twitch Authentication
            services.AddAuthentication(opts =>
            {
                opts.DefaultChallengeScheme = "twitch";
            }).AddCookie()
              .AddOpenIdConnect("twitch", opts =>
            {
                opts.Authority = Configuration["OAuth:Authority"];
                opts.ClientId = Configuration["OAuth:ClientId"];
                opts.ClientSecret = Configuration["OAuth:ClientSecret"];

                opts.Scope.Clear();
                var scopes = Configuration.GetSection("OAuth:Scopes").Get<List<string>>();
                foreach (var scope in scopes)
                {
                    opts.Scope.Add(scope);
                }

                opts.ResponseType = Configuration["OAuth:ResponseType"];
                opts.SaveTokens = true;

                opts.GetClaimsFromUserInfoEndpoint = true;

                opts.Events.OnTokenResponseReceived = async ctx =>
                {
                    var twitchService = (ITwitchService)ctx.HttpContext.RequestServices.GetService(typeof(ITwitchService));
                    await twitchService.SetUserAccessToken(ctx.TokenEndpointResponse.AccessToken);
                    await twitchService.SetAppAccessToken(opts.ClientId, opts.ClientSecret);
                };
            });
            services.AddAzureClients(builder =>
            {
                builder.AddBlobServiceClient(Configuration["ConnectionStrings:TableStorage/ConnectionString:blob"], preferMsi: true);
                builder.AddQueueServiceClient(Configuration["ConnectionStrings:TableStorage/ConnectionString:queue"], preferMsi: true);
            });

            // Add Telerik blazor
            //services.AddTelerikBlazor();
            services.AddBlazoredModal();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<ChatHub>("/chathub");
                endpoints.MapControllers();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
    internal static class StartupExtensions
    {
        public static IAzureClientBuilder<BlobServiceClient, BlobClientOptions> AddBlobServiceClient(this AzureClientFactoryBuilder builder, string serviceUriOrConnectionString, bool preferMsi)
        {
            if (preferMsi && Uri.TryCreate(serviceUriOrConnectionString, UriKind.Absolute, out Uri serviceUri))
            {
                return builder.AddBlobServiceClient(serviceUri);
            }
            else
            {
                return builder.AddBlobServiceClient(serviceUriOrConnectionString);
            }
        }
        public static IAzureClientBuilder<QueueServiceClient, QueueClientOptions> AddQueueServiceClient(this AzureClientFactoryBuilder builder, string serviceUriOrConnectionString, bool preferMsi)
        {
            if (preferMsi && Uri.TryCreate(serviceUriOrConnectionString, UriKind.Absolute, out Uri serviceUri))
            {
                return builder.AddQueueServiceClient(serviceUri);
            }
            else
            {
                return builder.AddQueueServiceClient(serviceUriOrConnectionString);
            }
        }
    }
}
