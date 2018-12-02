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
    }

}