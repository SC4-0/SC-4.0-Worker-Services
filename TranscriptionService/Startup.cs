using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

using RabbitMQ.Client;
using RabbitMQHelper;
using System;
using TranscriptionService.Controllers;
using TranscriptionService.SignalR;

namespace TranscriptionService
{
    public class Startup
    {
        string exchange;
        string queue;
        string routingKey;

        int signalrMessageSize;

        string allowedOrigins;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            exchange = Environment.GetEnvironmentVariable("RABBITMQ_TRANSCRIPTION_EXCHANGE") ?? "";
            queue = Environment.GetEnvironmentVariable("RABBITMQ_TRANSCRIPTION_QUEUE");
            routingKey = Environment.GetEnvironmentVariable("RABBITMQ_TRANSCRIPTION_ROUTING_KEY") ?? "";

            signalrMessageSize = int.Parse(Environment.GetEnvironmentVariable("SIGNALR_MAXIMUM_RECEIVE_MESSAGE_SIZE") ?? "32000");

            allowedOrigins = Environment.GetEnvironmentVariable("UI_URL") ?? "";
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IConnectionProvider, ConnectionProvider>();
            services.AddSingleton<ISubscriber>(x => new Subscriber(x.GetService<IConnectionProvider>(), exchange, queue, routingKey, ExchangeType.Topic));

            services.AddSignalR(o => {
                o.MaximumReceiveMessageSize = signalrMessageSize;
            });

            services.AddControllers();
            services.AddCors();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "TranscriptionService", Version = "v1" });
            });

            services.AddHostedService<TranscriptionConsumer>();
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
                .WithOrigins(allowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader();
            });

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<SignalRHub>("/Transcription");
                endpoints.MapControllers();
            });
        }
    }
}
