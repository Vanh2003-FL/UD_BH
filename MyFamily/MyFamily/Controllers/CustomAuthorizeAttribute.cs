using System;
using System.Web.Mvc;
using System.Web.Routing;

namespace MyFamily.Controllers
{
    /// <summary>
    /// Custom Authorize attribute that redirects to login with a message
    /// when user is not authenticated
    /// </summary>
    public class CustomAuthorizeAttribute : AuthorizeAttribute
    {
        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            base.OnAuthorization(filterContext);

            // If not authorized (user is not authenticated or not authorized)
            if (filterContext.Result is HttpUnauthorizedResult)
            {
                // Get the current requested URL
                string returnUrl = filterContext.HttpContext.Request.Url.PathAndQuery;

                // Redirect to login with unauthorized flag
                var loginUrl = new UrlHelper(filterContext.RequestContext).Action("Login", "Account", new
                {
                    returnUrl = returnUrl,
                    unauthorized = "true"
                });

                filterContext.Result = new RedirectResult(loginUrl);
            }
        }
    }
}
