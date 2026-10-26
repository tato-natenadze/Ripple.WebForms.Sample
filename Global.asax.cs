using System;
using System.Web;

namespace Ripple.WebForms.Sample
{
    public class WebApplication : HttpApplication
    {
        public static DemoRuntime Runtime { get; private set; }
        public static string StartupError { get; private set; }

        protected void Application_Start(object sender, EventArgs e)
        {
            try { Runtime = new DemoRuntime(Server.MapPath("~/App_Data")); }
            catch (Exception ex) { StartupError = "Cannot open the local sample data. " + DemoRuntime.Describe(ex); }
        }

        protected void Application_End(object sender, EventArgs e) { Runtime?.Stop(); }
    }
}
