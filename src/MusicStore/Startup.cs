using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Diagnostics;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using MusicStore.Models;
using Microsoft.Framework.Cache.Memory;

namespace MusicStore
{
    public class Startup
    {
        public Startup()
        {
            //Below code demonstrates usage of multiple configuration sources. For instance a setting say 'setting1' is found in both the registered sources, 
            //then the later source will win. By this way a Local config can be overridden by a different setting while deployed remotely.
            Configuration = new Configuration()
                        .AddJsonFile("config.json")
                        .AddEnvironmentVariables(); //All environment variables in the process's context flow in as configuration values.
        }

        public IConfiguration Configuration { get; private set; }

        public void ConfigureServices(IServiceCollection services)
        {
            //Sql client not available on mono
            var useInMemoryStore = Type.GetType("Mono.Runtime") != null;

            // Add EF services to the services container
            if (useInMemoryStore)
            {
                services.AddEntityFramework(Configuration)
                        .AddInMemoryStore()
                        .AddDbContext<MusicStoreContext>();
            }
            else
            {
                services.AddEntityFramework(Configuration)
                        .AddSqlServer()
                        .AddDbContext<MusicStoreContext>();
            }

            // Add Identity services to the services container
            services.AddIdentity<ApplicationUser, IdentityRole>(Configuration)
                    .AddEntityFrameworkStores<MusicStoreContext>()
                    .AddDefaultTokenProviders()
                    .AddMessageProvider<EmailMessageProvider>()
                    .AddMessageProvider<SmsMessageProvider>();

            services.ConfigureFacebookAuthentication(options =>
            {
                options.AppId = "550624398330273";
                options.AppSecret = "10e56a291d6b618da61b1e0dae3a8954";
            });

            services.ConfigureGoogleAuthentication(options =>
            {
                options.ClientId = "977382855444.apps.googleusercontent.com";
                options.ClientSecret = "NafT482F70Vjj_9q1PU4B0pN";
            });

            services.ConfigureTwitterAuthentication(options =>
            {
                options.ConsumerKey = "9J3j3pSwgbWkgPFH7nAf0Spam";
                options.ConsumerSecret = "jUBYkQuBFyqp7G3CUB9SW3AfflFr9z3oQBiNvumYy87Al0W4h8";
            });

            services.ConfigureMicrosoftAccountAuthentication(options =>
            {
                options.Caption = "MicrosoftAccount - Requires project changes";
                options.ClientId = "000000004012C08A";
                options.ClientSecret = "GaMQ2hCnqAC6EcDLnXsAeBVIJOLmeutL";
            });

            // Add MVC services to the services container
            services.AddMvc();

            //Add all SignalR related services to IoC.
            services.AddSignalR();

            //Add InMemoryCache
            services.AddSingleton<IMemoryCache, MemoryCache>();
        }

        //This method is invoked when KRE_ENV is 'Development' or is not defined
        //The allowed values are Development,Staging and Production
        public void ConfigureDevelopment(IApplicationBuilder app)
        {
            //Display custom error page in production when error occurs
            //During development use the ErrorPage middleware to display error information in the browser
            app.UseErrorPage(ErrorPageOptions.ShowAll);

            // Add the runtime information page that can be used by developers
            // to see what packages are used by the application
            // default path is: /runtimeinfo
            app.UseRuntimeInfoPage();

            Configure(app);
        }

        //This method is invoked when KRE_ENV is 'Staging'
        //The allowed values are Development,Staging and Production
        public void ConfigureStaging(IApplicationBuilder app)
        {
            app.UseErrorHandler("/Home/Error");
            Configure(app);
        }

        //This method is invoked when KRE_ENV is 'Production'
        //The allowed values are Development,Staging and Production
        public void ConfigureProduction(IApplicationBuilder app)
        {
            app.UseErrorHandler("/Home/Error");
            Configure(app);
        }

        public void Configure(IApplicationBuilder app)
        {
            //Configure SignalR
            app.UseSignalR();

            // Add static files to the request pipeline
            app.UseStaticFiles();

            // Add cookie-based authentication to the request pipeline
            app.UseIdentity();

            app.UseFacebookAuthentication();

            app.UseGoogleAuthentication();

            app.UseTwitterAuthentication();

            //The MicrosoftAccount service has restrictions that prevent the use of http://localhost:5001/ for test applications.
            //As such, here is how to change this sample to uses http://ktesting.com:5001/ instead.

            //Edit the Project.json file and replace http://localhost:5001/ with http://ktesting.com:5001/.

            //From an admin command console first enter:
            // notepad C:\Windows\System32\drivers\etc\hosts
            //and add this to the file, save, and exit (and reboot?):
            // 127.0.0.1 ktesting.com

            //Then you can choose to run the app as admin (see below) or add the following ACL as admin:
            // netsh http add urlacl url=http://ktesting:12345/ user=[domain\user]

            //The sample app can then be run via:
            // k web
            app.UseMicrosoftAccountAuthentication();

            // Add MVC to the request pipeline
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "areaRoute",
                    template: "{area:exists}/{controller}/{action}",
                    defaults: new { action = "Index" });

                routes.MapRoute(
                    name: "default",
                    template: "{controller}/{action}/{id?}",
                    defaults: new { controller = "Home", action = "Index" });

                routes.MapRoute(
                    name: "api",
                    template: "{controller}/{id?}");
            });

            //Populates the MusicStore sample data
            SampleData.InitializeMusicStoreDatabaseAsync(app.ApplicationServices).Wait();
        }
    }
}