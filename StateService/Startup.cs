using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using SimpleRabbit;
using StateService.SignalR;
using System;

namespace StateService
{
    public class Startup
    {
        int signalrMessageSize;

        string allowedOrigins;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            signalrMessageSize = int.Parse(Environment.GetEnvironmentVariable("SIGNALR_MAXIMUM_RECEIVE_MESSAGE_SIZE") ?? "32000");

            allowedOrigins = Environment.GetEnvironmentVariable("UI_URL") ?? "";
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IRabbitMQServiceProvider, RabbitMQServiceProvider>();

            services.AddSignalR(o => {
                o.MaximumReceiveMessageSize = signalrMessageSize;
            });

            services.AddControllers();
            services.AddCors();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "StateService", Version = "v1" });
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "StateService v1"));
            }

            app.UseRouting();
            app.UseCors(builder =>
            {
                builder
                .WithOrigins(allowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader();
            });

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<SignalRHub>("/State");
                endpoints.MapControllers();
            });
        }
    }
}
