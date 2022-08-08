using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebDavSample.WebDAVServerImpl;

namespace WebDavSample
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            HostingEnvironment = env;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();

            //Adds a WebDAV services to the specified <see cref = "IServiceCollection"/>.
            services.AddWebDav(Configuration, HostingEnvironment);

            //Adds WebSocketsService which notifies client about changes in WebDAV items.
            services.AddSingleton<WebSocketsService>();
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
            app.UseStaticFiles(new StaticFileOptions { ServeUnknownFileTypes = true });

            //Adds built-in Net.Core web sockets middleware.
            app.UseWebSockets();

            //Adds middleware that submits notifications to clients when any item on a WebDAV server is modified using web sockets.
            app.UseWebSocketsMiddleware();

            //Conditional middleware use for server root in case of OPTIONS or PROPFIND request to server root.
            app.UseWhen(context =>
                             {
                                 return !context.Request.Path.StartsWithSegments("/DAV") && (context.Request.Method == "OPTIONS" || context.Request.Method == "PROPFIND");
                             }, webDavApp => webDavApp.UseMiddleware<DavEngineMiddleware>());

            app.UseRouting();

            app.UseAuthorization();

            app.MapWhen(context =>
            {
                return context.Request.Path.StartsWithSegments("/DAV");
            }, webDavApp => webDavApp.UseMiddleware<DavEngineMiddleware>());

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }
        public IWebHostEnvironment HostingEnvironment { get; }
    }
}
