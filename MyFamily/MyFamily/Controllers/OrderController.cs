using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using MyFamily.Models;

namespace MyFamily.Controllers
{
    [CustomAuthorize]  // Require authentication for all Order operations
    public class OrderController : Controller
    {
        private MyDbContext db = new MyDbContext();

        // Helper method to remove Vietnamese diacritics
        private string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            string nfdText = text.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();

            foreach (char c in nfdText)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        // GET: Order
        public ActionResult Index(string search = "", string filterStatus = "")
        {
            var orders = db.Orders
                .Include("Customer")
                .Include("OrderDetails.Product")
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            // Filter by search (both customer name and product name) - case insensitive and diacritic insensitive
            if (!string.IsNullOrWhiteSpace(search))
            {
                string normalizedSearch = RemoveDiacritics(search).ToLower();
                
                orders = orders.Where(o => 
                    (o.Customer != null && RemoveDiacritics(o.Customer.Name).ToLower().Contains(normalizedSearch)) || 
                    (o.OrderDetails != null && o.OrderDetails.Any(d => d.Product != null && RemoveDiacritics(d.Product.ProductName).ToLower().Contains(normalizedSearch)))
                ).ToList();
            }

            // Filter by status
            if (!string.IsNullOrWhiteSpace(filterStatus))
            {
                if (Enum.TryParse<OrderStatus>(filterStatus, out OrderStatus status))
                {
                    orders = orders.Where(o => o.Status == status).ToList();
                }
            }

            // Store filter values in ViewBag
            ViewBag.Search = search;
            ViewBag.FilterStatus = filterStatus;

            return View(orders);
        }

        // GET: Order/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return HttpNotFound();
            }

            Order order = db.Orders
                .Include("Customer")
                .Include("OrderDetails.Product")
                .FirstOrDefault(o => o.OrderId == id);

            if (order == null)
            {
                return HttpNotFound();
            }

            // Check if order is completed
            ViewBag.IsCompleted = (order.Status == OrderStatus.DeliveredPaid);

            return View(order);
        }

        // GET: Order/Create
        public ActionResult Create()
        {
            ViewBag.CustomerId = new SelectList(db.Customers, "CustomerId", "Name");
            ViewBag.ProductId = new SelectList(db.Products, "ProductId", "ProductName");
            ViewBag.AllProducts = db.Products.ToList();
            return View();
        }

        // POST: Order/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(FormCollection collection)
        {
            var order = new Order();

            // Xử lý khách hàng
            string customerIdStr = collection["CustomerId"];
            string customerName = collection["CustomerName"];
            
            int customerId = 0;
            int.TryParse(customerIdStr, out customerId);

            if (!string.IsNullOrEmpty(customerName))
            {
                // Tạo khách hàng mới
                var newCustomer = new Customer { Name = customerName };
                db.Customers.Add(newCustomer);
                db.SaveChanges();
                order.CustomerId = newCustomer.CustomerId;
            }
            else if (customerId > 0)
            {
                // Sử dụng khách hàng đã chọn
                order.CustomerId = customerId;
            }
            else
            {
                // Không chọn khách hàng và không nhập tên mới
                ModelState.AddModelError("", "Vui lòng chọn khách hàng hoặc nhập tên khách hàng mới");
                ViewBag.CustomerId = new SelectList(db.Customers, "CustomerId", "Name");
                ViewBag.ProductId = new SelectList(db.Products, "ProductId", "ProductName");
                ViewBag.AllProducts = db.Products.ToList();
                return View(order);
            }

            order.OrderDate = DateTime.Now;
            order.OrderDetails = new List<OrderDetail>();
            decimal totalAmount = 0;

            // Lấy danh sách những keys có dạng products[X].ProductId
            var productKeys = new System.Collections.Generic.List<string>();
            foreach (string key in collection.Keys)
            {
                if (key.StartsWith("products[") && key.EndsWith("].ProductId"))
                {
                    productKeys.Add(key);
                }
            }

            // Parse từng sản phẩm
            foreach (var key in productKeys)
            {
                // Lấy index từ "products[0].ProductId" -> "0"
                string indexStr = key.Replace("products[", "").Replace("].ProductId", "");
                string prefix = "products[" + indexStr + "]";

                string productIdStr = collection[prefix + ".ProductId"];
                string quantityStr = collection[prefix + ".Quantity"];
                string priceStr = collection[prefix + ".Price"];

                if (!string.IsNullOrEmpty(productIdStr))
                {
                    int productId;
                    decimal quantity;
                    decimal price;

                    if (int.TryParse(productIdStr, out productId) &&
                        decimal.TryParse(quantityStr, out quantity) &&
                        decimal.TryParse(priceStr, out price) &&
                        quantity > 0 && productId > 0)
                    {
                        var product = db.Products.Find(productId);
                        if (product != null)
                        {
                            order.OrderDetails.Add(new OrderDetail
                            {
                                ProductId = productId,
                                Quantity = quantity,
                                Price = price
                            });
                            totalAmount += price * quantity;
                        }
                    }
                }
            }

            // Auto-calculate financial fields
            order.TotalAmount = totalAmount;
            order.PaidAmount = 0;
            order.DebtAmount = totalAmount;
            order.CostAmount = 0;
            order.ProfitAmount = 0;

            db.Orders.Add(order);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // GET: Order/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return HttpNotFound();
            }

            Order order = db.Orders
                .Include("Customer")
                .Include("OrderDetails.Product")
                .FirstOrDefault(o => o.OrderId == id);
            
            if (order == null)
            {
                return HttpNotFound();
            }

            // Prevent editing of completed orders
            if (order.Status == OrderStatus.DeliveredPaid)
            {
                ViewBag.ErrorMessage = "✅ Đơn hàng đã hoàn thành thành công! K được phép chỉnh sửa.";
                return View("ErrorMessage");
            }

            ViewBag.CustomerId = new SelectList(db.Customers, "CustomerId", "Name", order.CustomerId);
            ViewBag.AllProducts = db.Products.ToList();
            return View(order);
        }

        // POST: Order/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, FormCollection collection)
        {
            Order order = db.Orders
                .Include("OrderDetails")
                .FirstOrDefault(o => o.OrderId == id);
            
            if (order == null)
            {
                return HttpNotFound();
            }

            // Prevent editing of completed orders
            if (order.Status == OrderStatus.DeliveredPaid)
            {
                ViewBag.ErrorMessage = "✅ Đơn hàng đã hoàn thành thành công! K được phép chỉnh sửa.";
                return View("ErrorMessage");
            }
            
            if (order == null)
            {
                return HttpNotFound();
            }

            try
            {
                // Track if any modifications were made to products
                bool hasProductModifications = false;
                
                // 1. Xóa sản phẩm bị đánh dấu xóa
                string removedIds = collection["removedDetailIds"];
                if (!string.IsNullOrEmpty(removedIds))
                {
                    hasProductModifications = true;
                    var idsToRemove = removedIds.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries)
                        .Select(id_str => {
                            int parsedId;
                            if (int.TryParse(id_str, out parsedId)) return parsedId;
                            return -1;
                        })
                        .Where(parsed_id => parsed_id > 0)
                        .ToList();

                    foreach (var detailId in idsToRemove)
                    {
                        var detail = order.OrderDetails.FirstOrDefault(d => d.OrderDetailId == detailId);
                        if (detail != null)
                        {
                            db.OrderDetails.Remove(detail);
                            order.OrderDetails.Remove(detail);
                        }
                    }
                }

                // 2. Cập nhật số lượng sản phẩm hiện tại
                // Lấy tất cả keys có dạng orderDetailIds[X]
                var detailIdKeys = collection.Keys.Cast<string>()
                    .Where(k => k.StartsWith("orderDetailIds[") && k.EndsWith("]"))
                    .ToList();

                foreach (var key in detailIdKeys)
                {
                    int orderDetailId;
                    if (!int.TryParse(collection[key], out orderDetailId) || orderDetailId <= 0)
                        continue;

                    // Tìm index tương ứng từ key: "orderDetailIds[0]" -> "quantities[0]"
                    string indexStr = key.Replace("orderDetailIds[", "").Replace("]", "");
                    string quantityKey = "quantities[" + indexStr + "]";

                    decimal newQuantity;
                    string quantityStr = collection[quantityKey] ?? "0";
                    
                    if (!decimal.TryParse(quantityStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out newQuantity))
                    {
                        newQuantity = 0;
                    }

                    var detail = order.OrderDetails.FirstOrDefault(d => d.OrderDetailId == orderDetailId);
                    if (detail != null && newQuantity > 0)
                    {
                        // Check if quantity changed
                        if (detail.Quantity != newQuantity)
                        {
                            hasProductModifications = true;
                        }
                        detail.Quantity = newQuantity;
                    }
                }

                // 3. Thêm sản phẩm mới
                // Lấy tất cả keys có dạng newProducts[X].ProductId
                var newProductKeys = collection.Keys.Cast<string>()
                    .Where(k => k.StartsWith("newProducts[") && k.EndsWith("].ProductId"))
                    .ToList();

                if (newProductKeys.Count > 0)
                {
                    hasProductModifications = true;
                }

                foreach (var key in newProductKeys)
                {
                    string productIdStr = collection[key];
                    if (string.IsNullOrEmpty(productIdStr)) continue;

                    int productId;
                    if (!int.TryParse(productIdStr, out productId) || productId <= 0)
                        continue;

                    // Tìm index tương ứng từ key: "newProducts[0].ProductId" -> index = "0"
                    string indexStr = key.Replace("newProducts[", "").Replace("].ProductId", "");
                    string quantityKey = "newProducts[" + indexStr + "].Quantity";
                    string priceKey = "newProducts[" + indexStr + "].Price";

                    decimal quantity;
                    string quantityStr = collection[quantityKey] ?? "0";
                    if (!decimal.TryParse(quantityStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out quantity) || quantity <= 0)
                    {
                        continue; // Skip if quantity invalid or 0
                    }

                    decimal price;
                    string priceStr = collection[priceKey] ?? "0";
                    if (!decimal.TryParse(priceStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out price))
                    {
                        price = 0;
                    }

                    var newDetail = new OrderDetail
                    {
                        OrderId = order.OrderId,
                        ProductId = productId,
                        Quantity = quantity,
                        Price = price
                    };
                    db.OrderDetails.Add(newDetail);
                    order.OrderDetails.Add(newDetail);
                }

                // 4. Tính lại TotalAmount từ OrderDetails
                decimal newTotalAmount = 0;
                foreach (var detail in order.OrderDetails)
                {
                    newTotalAmount += detail.Price * detail.Quantity;
                }
                order.TotalAmount = newTotalAmount;
                order.DebtAmount = order.TotalAmount - order.PaidAmount;

                // 5. Determine payment status based on TotalAmount vs PaidAmount
                bool isDelivered = order.Status == OrderStatus.DeliveredNotPaid || 
                                   order.Status == OrderStatus.DeliveredPaid;
                
                if (order.PaidAmount >= order.TotalAmount && order.TotalAmount > 0)
                {
                    // Fully paid (or overpaid) - set to paid status
                    // Keep the actual DebtAmount (will be negative if overpaid = refund amount)
                    order.Status = isDelivered ? OrderStatus.DeliveredPaid : OrderStatus.PendingPaid;
                }
                else if (order.PaidAmount > 0)
                {
                    // Partially paid - keep as unpaid status
                    order.Status = isDelivered ? OrderStatus.DeliveredNotPaid : OrderStatus.PendingNotPaid;
                }
                else
                {
                    // Not paid at all - ensure status is unpaid
                    order.Status = isDelivered ? OrderStatus.DeliveredNotPaid : OrderStatus.PendingNotPaid;
                }

                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Log error for debugging
                System.Diagnostics.Debug.WriteLine("Edit Error: " + ex.Message);
                ModelState.AddModelError("", "Có lỗi khi lưu thay đổi: " + ex.Message);
                
                // Reload data and return to edit view
                var editOrder = db.Orders
                    .Include("Customer")
                    .Include("OrderDetails.Product")
                    .FirstOrDefault(o => o.OrderId == id);
                
                if (editOrder != null)
                {
                    ViewBag.AllProducts = db.Products.ToList();
                    return View(editOrder);
                }
                
                return RedirectToAction("Edit", new { id = id });
            }
        }

        // GET: Order/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("Index");
            }

            Order order = db.Orders
                .Include("Customer")
                .FirstOrDefault(o => o.OrderId == id);

            if (order == null)
            {
                return HttpNotFound();
            }

            return View(order);
        }

        // POST: Order/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Order order = db.Orders
                .Include("OrderDetails")
                .FirstOrDefault(o => o.OrderId == id);
            
            if (order != null)
            {
                // Xoá tất cả OrderDetails trước
                if (order.OrderDetails != null && order.OrderDetails.Count > 0)
                {
                    foreach (var detail in order.OrderDetails.ToList())
                    {
                        db.OrderDetails.Remove(detail);
                    }
                }
                
                // Sau đó xoá Order
                db.Orders.Remove(order);
                db.SaveChanges();
            }
            
            return RedirectToAction("Index");
        }

        // POST: Order/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateStatus(int id, string newStatus)
        {
            Order order = db.Orders.Find(id);
            if (order != null)
            {
                try
                {
                    switch (newStatus?.ToLower())
                    {
                        case "delivered":
                            // Mark as delivered
                            if (order.Status == OrderStatus.PendingNotPaid)
                                order.Status = OrderStatus.DeliveredNotPaid;
                            else if (order.Status == OrderStatus.PendingPaid)
                                order.Status = OrderStatus.DeliveredPaid;
                            break;
                        
                        case "paid":
                            // Mark as paid - set PaidAmount = TotalAmount if not already set
                            if (order.Status == OrderStatus.PendingNotPaid)
                            {
                                order.Status = OrderStatus.PendingPaid;
                                order.PaidAmount = order.TotalAmount;
                                order.DebtAmount = 0;
                            }
                            else if (order.Status == OrderStatus.DeliveredNotPaid)
                            {
                                order.Status = OrderStatus.DeliveredPaid;
                                order.PaidAmount = order.TotalAmount;
                                order.DebtAmount = 0;
                            }
                            break;
                    }
                    
                    db.SaveChanges();
                }
                catch
                {
                    // Error updating status
                }
            }
            
            return RedirectToAction("Details", new { id = id });
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
