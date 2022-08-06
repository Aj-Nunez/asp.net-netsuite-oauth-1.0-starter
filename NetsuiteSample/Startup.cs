using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetsuiteSample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();


            services.Configure<NetsuiteOptions>(Configuration.GetSection("Netsuite"));
            services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<NetsuiteOptions>>().Value);

            services.AddHttpClient();

            services.AddTransient<OAuthService>();

            services.AddTransient<INetsuiteService, NetsuiteService>();

            services.AddHttpClient("netsuite")
                .AddHttpMessageHandler((s) =>
                {
                    return new NetsuiteDelegatingHandler(
                        s.GetService<NetsuiteOptions>(),
                        s.GetService<OAuthService>());
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
