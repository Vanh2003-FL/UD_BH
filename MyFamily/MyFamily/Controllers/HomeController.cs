using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MyFamily.Models;

namespace MyFamily.Controllers
{
    public class HomeController : Controller
    {
        private MyDbContext db = new MyDbContext();

        public ActionResult Index()
        {
            var orders = db.Orders
                .Include("Customer")
                .Include("OrderDetails")
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            // Calculate correct metrics
            decimal totalRevenue = orders.Sum(o => o.TotalAmount);
            
            // Calculate unpaid amount based on order status
            // An order is unpaid if Status is NOT DeliveredPaid (both delivered AND paid)
            decimal totalUnpaid = orders
                .Where(o => o.Status != OrderStatus.DeliveredPaid)
                .Sum(o => o.TotalAmount);
            
            int totalCustomers = orders.Select(o => o.CustomerId).Distinct().Count();

            // Store in ViewBag for display
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalDebt = totalUnpaid;
            ViewBag.TotalCustomers = totalCustomers;

            return View(orders);
        }

        public ActionResult Statistics()
        {
            var orders = db.Orders
                .Include("Customer")
                .Include("OrderDetails.Product")
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            // 1. Product Sales by Quantity
            var productSales = orders
                .SelectMany(o => o.OrderDetails)
                .GroupBy(od => od.Product.ProductName)
                .Select(g => new { Product = g.Key, Quantity = g.Sum(od => od.Quantity) })
                .OrderByDescending(x => x.Quantity)
                .ToList();
            
            ViewBag.ProductLabels = productSales.Select(x => x.Product).ToList();
            ViewBag.ProductQuantities = productSales.Select(x => x.Quantity).ToList();

            // 2. Revenue by Product
            var productRevenue = orders
                .SelectMany(o => o.OrderDetails)
                .GroupBy(od => od.Product.ProductName)
                .Select(g => new { Product = g.Key, Revenue = g.Sum(od => od.Price * od.Quantity) })
                .OrderByDescending(x => x.Revenue)
                .ToList();
            
            ViewBag.RevenueLabels = productRevenue.Select(x => x.Product).ToList();
            ViewBag.RevenueAmounts = productRevenue.Select(x => (long)x.Revenue).ToList();

            return View(orders);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Thông Tin Thanh Toán";
            
            // Check if QR code image exists
            string qrPath = Path.Combine(Server.MapPath("~/Uploads"), "qr-code.png");
            ViewBag.QRCodeExists = System.IO.File.Exists(qrPath);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult About(HttpPostedFileBase qrCodeFile)
        {
            ViewBag.Message = "Thông Tin Thanh Toán";

            if (qrCodeFile != null && qrCodeFile.ContentLength > 0)
            {
                try
                {
                    // Create Uploads folder if it doesn't exist
                    string uploadsFolder = Server.MapPath("~/Uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Check file extension
                    string fileExtension = Path.GetExtension(qrCodeFile.FileName).ToLower();
                    if (fileExtension != ".png" && fileExtension != ".jpg" && fileExtension != ".jpeg" && fileExtension != ".gif")
                    {
                        ViewBag.Message = "Chỉ chấp nhận file ảnh (png, jpg, jpeg, gif)";
                        ViewBag.QRCodeExists = System.IO.File.Exists(Path.Combine(uploadsFolder, "qr-code.png"));
                        return View();
                    }

                    // Save file as qr-code.png
                    string filePath = Path.Combine(uploadsFolder, "qr-code.png");
                    
                    // Delete old file if exists
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }

                    // Save new file
                    qrCodeFile.SaveAs(filePath);

                    ViewBag.SuccessMessage = "Tải lên mã QR thành công!";
                    ViewBag.QRCodeExists = true;
                }
                catch (Exception ex)
                {
                    ViewBag.Message = "Lỗi tải lên: " + ex.Message;
                    ViewBag.QRCodeExists = System.IO.File.Exists(Path.Combine(Server.MapPath("~/Uploads"), "qr-code.png"));
                }
            }
            else
            {
                ViewBag.Message = "Vui lòng chọn một ảnh để tải lên";
                ViewBag.QRCodeExists = System.IO.File.Exists(Path.Combine(Server.MapPath("~/Uploads"), "qr-code.png"));
            }

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
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