using System;
using System.Web;
using Newtonsoft.Json;

namespace Ripple.WebForms.Sample
{
    public sealed class StatusHandler : IHttpHandler
    {
        public bool IsReusable => true;
        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/json";
            context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            context.Response.Cache.SetNoStore();
            try
            {
                if (WebApplication.Runtime == null) throw new InvalidOperationException(WebApplication.StartupError ?? "The sample is not running.");
                context.Response.Write(JsonConvert.SerializeObject(WebApplication.Runtime.Snapshot()));
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 503;
                context.Response.TrySkipIisCustomErrors = true;
                context.Response.Write(JsonConvert.SerializeObject(new { Error = DemoRuntime.Describe(ex) }));
            }
        }
    }
}
