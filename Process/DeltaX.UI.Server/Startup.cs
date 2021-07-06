using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting; 
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DeltaX.Modules.RealTimeRpcWebSocket;
using Microsoft.Extensions.Options;

public class Startup
{  
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IOptions<UIConfiguration> options)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        if (env.IsDevelopment() || options.Value.UseSwagger)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "DeltaX.UI.Server"));
        }

        // HTTP Cors
        var configuration = app.ApplicationServices.GetService<IOptions<UIConfiguration>>();
        var origins = configuration?.Value.CorsUrls
            ?? new[] { "http://127.0.0.1:8080", "https://127.0.0.1:8081", "http://localhost:8080" };

        app.UseCors(x => x
            .WithOrigins(origins)
            .AllowAnyMethod()
            .AllowCredentials()
            .AllowAnyHeader());

        // serve wwwroot
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseFileServer();

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });

        // // WS Cors
        // var webSocketOptions = new WebSocketOptions()
        // {
        //     KeepAliveInterval = TimeSpan.FromSeconds(120),
        // };
        // Array.ForEach(origins, o => webSocketOptions.AllowedOrigins.Add(o));
        // app.UseWebSockets(webSocketOptions);
        app.UseWebSockets();
        app.UseRealTimeWebSocketBridge("/rt");

    }
}
