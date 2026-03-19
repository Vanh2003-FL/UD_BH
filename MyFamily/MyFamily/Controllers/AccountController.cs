using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using MyFamily.Models;

namespace MyFamily.Controllers
{
    public class AccountController : Controller
    {
        private MyDbContext db = new MyDbContext();

        // GET: Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl = "")
        {
            // Check if redirected from unauthorized action
            if (Request.QueryString["unauthorized"] == "true")
            {
                ViewBag.Message = "Vui lòng đăng nhập để tiếp tục";
            }
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public ActionResult Login(string username, string password, bool rememberMe = false, string returnUrl = "")
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập tên đăng nhập và mật khẩu";
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            // Find user in database
            var user = db.Users.FirstOrDefault(u => u.Username == username);
            
            if (user != null && user.Password == password)
            {
                // Authentication successful
                // Store format: username|UserId|Role
                FormsAuthentication.SetAuthCookie(user.Username + "|" + user.UserId + "|" + user.Role, rememberMe);
                
                // Redirect to return URL if provided, otherwise go to home
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng";
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }
        }

        // Logout
        [CustomAuthorize]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
