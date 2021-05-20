using Blazored.Modal;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using TwitchChatBot.Client.Filters;
using TwitchChatBot.Client.Hubs;
using TwitchChatBot.Client.Models.Options;
using TwitchChatBot.Client.Services;

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
            services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme).AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAdB2C"));

            // Adds support for Controllers + Default Views for Microsoft.Identity.Web (for AzureADB2C Auth)
            services.AddControllersWithViews()
                .AddMicrosoftIdentityUI();
            
            services.AddAuthorization(options =>
            {
                // By default, all incoming requests will be authorized according to the default policy
                options.FallbackPolicy = options.DefaultPolicy;
            });


            // Adds Razor Pages support
            services.AddRazorPages();

            // Adds ServerSideBlazor
            services.AddServerSideBlazor().AddMicrosoftIdentityConsentHandler();
            // Adds Authentication
            //services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();          

            // Add Services to IOC
            services.AddSingleton<IStorageService, AzureTableStorageService>();
            services.AddSingleton<ITwitchService, TwitchService>();

            // Add options for reading settings
            services.Configure<TwitchOptions>(Configuration.GetSection("Twitch"));
            services.Configure<OAuthOptions>(Configuration.GetSection("OAuth"));
            services.Configure<TableStorageOptions>(Configuration.GetSection("TableStorage"));
            services.Configure<BotOptions>(Configuration.GetSection("Bot"));

            // Adds IdentityModel for TokenManagement
            // Adds a Twitch HttpClient
            services.AddAccessTokenManagement(opts =>
            {
                // TODO: Bind the scope to the Oauth:Scopes options
                opts.Client.Clients.Add("twitch", new ClientCredentialsTokenRequest
                {
                    Address = Configuration["OAuth:TokenUrl"],
                    ClientId = Configuration["OAuth:ClientId"],
                    ClientSecret = Configuration["OAuth:ClientSecret"],
                    GrantType = "client_credentials",
                    //Scope = "channel:read:subscriptions&user:manage:blocked_users&user:read:blocked_users&user:read:follows&chat:edit&chat:read"
                    Scope= "user:read:email moderation:read channel:read:subscriptions"
                });
            });
            services.AddHttpClient<ITwitchHttpClient, TwitchHttpClient>(opts =>
            {
                opts.BaseAddress = new System.Uri(Configuration["Twitch:Urls:ApiUrl"]);
                opts.DefaultRequestHeaders.Add("Client-ID", Configuration["OAuth:ClientId"]);
            }).AddClientAccessTokenHandler(tokenClientName: "twitch");
            services.AddHttpContextAccessor();

            

            // The following line enables Application Insights telemetry collection.
            //services.AddApplicationInsightsTelemetry(Configuration);

            // Add Telerik blazor
            //services.AddTelerikBlazor();
            services.AddBlazoredModal();

            // Add TwitchSubscriptionService
            services.AddHostedService<TwitchSubscriptionService>();
            
            // Add TwitchWebhookRequestFilter
            services.AddScoped<TwitchRequestFilter>();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
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
