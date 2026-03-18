using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using MyFamily.Models;

namespace MyFamily.Controllers
{
    public class OrderDetailController : Controller
    {
        private MyDbContext db = new MyDbContext();

        // GET: OrderDetail/Create/5 (OrderId=5)
        public ActionResult Create(int? orderId)
        {
            if (orderId == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            Order order = db.Orders.Find(orderId);
            if (order == null)
            {
                return HttpNotFound();
            }

            ViewBag.OrderId = orderId;
            ViewBag.ProductId = new SelectList(db.Products, "ProductId", "ProductName");
            return View();
        }

        // POST: OrderDetail/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "OrderId,ProductId,Quantity,Price")] OrderDetail orderDetail)
        {
            if (ModelState.IsValid)
            {
                db.OrderDetails.Add(orderDetail);
                db.SaveChanges();
                return RedirectToAction("Details", "Order", new { id = orderDetail.OrderId });
            }

            ViewBag.OrderId = orderDetail.OrderId;
            ViewBag.ProductId = new SelectList(db.Products, "ProductId", "ProductName", orderDetail.ProductId);
            return View(orderDetail);
        }

        // GET: OrderDetail/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            OrderDetail orderDetail = db.OrderDetails.Find(id);
            if (orderDetail == null)
            {
                return HttpNotFound();
            }

            ViewBag.ProductId = new SelectList(db.Products, "ProductId", "ProductName", orderDetail.ProductId);
            return View(orderDetail);
        }

        // POST: OrderDetail/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "OrderDetailId,OrderId,ProductId,Quantity,Price")] OrderDetail orderDetail)
        {
            if (ModelState.IsValid)
            {
                db.Entry(orderDetail).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Details", "Order", new { id = orderDetail.OrderId });
            }

            ViewBag.ProductId = new SelectList(db.Products, "ProductId", "ProductName", orderDetail.ProductId);
            return View(orderDetail);
        }

        // GET: OrderDetail/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            OrderDetail orderDetail = db.OrderDetails
                .Include(o => o.Product)
                .FirstOrDefault(o => o.OrderDetailId == id);

            if (orderDetail == null)
            {
                return HttpNotFound();
            }

            return View(orderDetail);
        }

        // POST: OrderDetail/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            OrderDetail orderDetail = db.OrderDetails.Find(id);
            int orderId = orderDetail.OrderId;
            db.OrderDetails.Remove(orderDetail);
            db.SaveChanges();
            return RedirectToAction("Details", "Order", new { id = orderId });
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
