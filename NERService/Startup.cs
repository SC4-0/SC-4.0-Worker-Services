using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using NERService.Controllers;
using NERService.SignalR;
using RabbitMQ.Client;
using RabbitMQHelper;
using System;

namespace NERService
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

            var exchange = Environment.GetEnvironmentVariable("RABBITMQ_TRANSCRIPTION_EXCHANGE") ?? "";
            var queue = Environment.GetEnvironmentVariable("RABBITMQ_TRANSCRIPTION_QUEUE");
            var routingKey = Environment.GetEnvironmentVariable("RABBITMQ_TRANSCRIPTION_ROUTING_KEY") ?? "";

            var signalrMessageSize = Environment.GetEnvironmentVariable("SIGNALR_MAXIMUM_RECEIVE_MESSAGE_SIZE") ?? "32000";

            services.AddSingleton<IConnectionProvider, ConnectionProvider>();
            services.AddSingleton<ISubscriber>(x => new Subscriber(x.GetService<IConnectionProvider>(), exchange, queue, routingKey, ExchangeType.Topic));

            services.AddSignalR(o => {
                o.MaximumReceiveMessageSize = int.Parse(signalrMessageSize);
            });

            services.AddControllers();
            services.AddCors();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "NERService", Version = "v1" });
            });

            services.AddHostedService<NERConsumer>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TranscriptionService v1"));
            }

            app.UseRouting();
            app.UseCors(builder =>
            {
                builder
                .WithOrigins(Environment.GetEnvironmentVariable("UI_URL"))
                .AllowAnyMethod()
                .AllowAnyHeader();
            });

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<SignalRHub>("/NER");
                endpoints.MapControllers();
            });
        }
    }
}