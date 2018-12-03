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
                //Get products of user
                List<ProductDTO> listings = db.Products.Where(x => x.UserId == id).ToList();
                List<OrderDetailsDTO> userDetails = db.OrderDetails.Where(x => x.UserId == id).ToList();
                List<OrderDTO> orders = db.Orders.Where(x => x.UserId == id).ToList();
                //Init List of prod details
                foreach (ProductDTO prod in listings)
                {
                    if(prod != null)
                    {
                        userDetails.Add(db.OrderDetails.Where(x => x.ProductId == prod.Id).FirstOrDefault());
                        PdfDTO pdf = db.Pdfs.Where(x => x.ProductId == prod.Id).FirstOrDefault();
                        PhotoDTO photo = db.Photos.Where(x => x.ProductId == prod.Id).FirstOrDefault();
                        db.Pdfs.Remove(pdf);
                        db.Photos.Remove(photo);
                    }
                }

                foreach(OrderDetailsDTO det in userDetails)
                {
                    if(det != null)
                    {
                        orders.Add(db.Orders.Where(x => x.OrderId == det.OrderId).FirstOrDefault());
                        foreach (OrderDTO or in orders)
                        {
                            if (or != null)
                            {
                                db.Orders.Remove(or);
                            }
                        }
                        db.OrderDetails.Remove(det);
                    }
                }

                foreach (ProductDTO prod in listings)
                {
                        if (prod != null)
                        {
                            db.Products.Remove(prod);
                        }
                }     
               
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