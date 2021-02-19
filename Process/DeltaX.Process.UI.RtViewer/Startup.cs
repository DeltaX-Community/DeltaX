using DeltaX.Modules.RealTimeRpcWebSocket;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting; 
using Microsoft.Extensions.Hosting;
using System;

namespace DeltaX.Process.UI.RtViewer
{
    public class Startup
    {    
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        { 
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors("DefaultVueCors");

            // serve wwwroot
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseFileServer();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseWebSockets();
            app.UseRealTimeWebSocketBridge("/rt");
        }
    }
}
