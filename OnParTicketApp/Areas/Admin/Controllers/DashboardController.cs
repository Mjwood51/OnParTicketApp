using OnParTicketApp.Models.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OnParTicketApp.Areas.Admin.Controllers
{
    [Authorize(Roles ="Admin")]
    public class DashboardController : Controller
    {
        // GET: Admin/Dashboard
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult GetUsers()
        {
            List<UserDTO> users = new List<UserDTO>();
            using (TicketAppDB db = new TicketAppDB())
            {
                List<UserRoleDTO> roles = db.UserRoles.Where(x=>x.RoleId == 2).ToList();
                foreach (UserRoleDTO r in roles)
                {
                    if(db.Users.Any(x=>x.Id == r.UserId))
                    {
                        UserDTO user = db.Users.Where(x => x.Id == r.UserId).FirstOrDefault();
                        users.Add(user);                                 
                        
                    }                  
                }
            }
            return PartialView(users);
        }

        public ActionResult GetUnverified()
        {
            using (TicketAppDB db = new TicketAppDB())
            {
                List<ProductDTO> listings = db.Products.Where(x => x.Verified == 0).ToList();
                return PartialView(listings);
            } 
        }

        public ActionResult DeleteUser(int id)
        {
            using (TicketAppDB db = new TicketAppDB())
            {
                //Get products of user, remove those products
                List<ProductDTO> listings = db.Products.Where(x => x.UserId == id).ToList();
                foreach(ProductDTO prod in listings)
                {
                    db.Products.Remove(prod);
                }

                //Get orderdetails of user
                List<OrderDetailsDTO> details = db.OrderDetails.Where(x => x.UserId == id).ToList();

                //Get orders of user, remove orders and orderdetails, move any ordered products back as an available listing
                if (details.Any())
                {
                    foreach (OrderDetailsDTO order in details)
                    {
                        OrderDTO anOrder = db.Orders.Where(x => x.OrderId == order.OrderId).FirstOrDefault();
                        ProductDTO product = db.Products.Where(x => x.Id == order.ProductId).FirstOrDefault();
                        product.IsSold = false;
                        db.Orders.Remove(anOrder);
                        db.OrderDetails.Remove(order);
                    }
                }

                //Get user and remove the user
                UserDTO user = db.Users.Where(x => x.Id == id).FirstOrDefault();
                string u = user.Username;
                db.Users.Remove(user);

                db.SaveChanges();
                TempData["SM"] = "You have removed " + u + " from the website.";
                return RedirectToAction("Index");
            }
        }
    }

}