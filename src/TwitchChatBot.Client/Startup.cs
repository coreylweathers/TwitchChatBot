using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TwitchChatBot.Client.Areas.Identity;
using TwitchChatBot.Client.Hubs;
using TwitchChatBot.Client.Models.Options;
using TwitchChatBot.Client.Services;
using Blazored.Modal;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

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
            // Adds Azure AD B2C
            //services.AddAuthentication(AzureADB2CDefaults.AuthenticationScheme)
               // .AddAzureADB2C(opts => Configuration.Bind("AzureAdB2C", opts));
               //services.AddRazorPages();
               
               services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                   .AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAdB2C"));
               services.AddControllersWithViews()
                   .AddMicrosoftIdentityUI();

               services.AddAuthorization(options =>
               {
                   // By default, all incoming requests will be authorized according to the default policy
                   options.FallbackPolicy = options.DefaultPolicy;
               });

               services.AddRazorPages();
               // Adds ServerSideBlazor
            services.AddServerSideBlazor().AddMicrosoftIdentityConsentHandler();
            // Adds Authentication
            //services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();
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
            services.AddHttpContextAccessor();

            // The following line enables Application Insights telemetry collection.
            services.AddApplicationInsightsTelemetry(Configuration);

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
}
