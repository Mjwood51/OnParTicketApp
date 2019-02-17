using OnParTicketApp.Controllers;
using OnParTicketApp.Models.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace OnParTicketApp
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        protected void Application_BeginRequest()
        {
            var loadbalancerReceivedSslRequest = string.Equals(Request.Headers["X-Forwarded-Proto"], "https");
            var serverReceivedSslRequest = Request.IsSecureConnection;

            if (loadbalancerReceivedSslRequest || serverReceivedSslRequest) return;

            UriBuilder uri = new UriBuilder(Context.Request.Url);
            if (!uri.Host.Equals("localhost"))
            {
                uri.Port = 443;
                uri.Scheme = "https";
                Response.Redirect(uri.ToString());
            }
        }

        public class IPrincipalModelBinder : IModelBinder
        {
            public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
            {
                if (controllerContext == null)
                {
                    throw new ArgumentNullException("controllerContext");
                }
                if (bindingContext == null)
                {
                    throw new ArgumentNullException("bindingContext");
                }
                IPrincipal p = controllerContext.HttpContext.User;
                return p;
            }
        }

        protected void Application_AuthenticateRequest()
        {
            // Check if user is logged in
            if (User == null) { return; }

            // Get username
            string username = Context.User.Identity.Name;

            // Declare array of roles
            string[] roles = null;

            using (TicketAppDB db = new TicketAppDB())
            {
                // Populate roles
                UserDTO dto = db.Users.FirstOrDefault(x => x.Username == username);

                roles = db.UserRoles.Where(x => x.UserId == dto.Id).Select(x => x.Role.Name).ToArray();
            }

            // Build IPrincipal object
            IIdentity userIdentity = new GenericIdentity(username);
            IPrincipal newUserObj = new GenericPrincipal(userIdentity, roles);

            // Update Context.User
            Context.User = newUserObj;
        }
    }
}
