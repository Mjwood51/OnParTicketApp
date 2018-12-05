using OnParTicketApp.Models.Data;
using OnParTicketApp.Models.ViewModels.Account;
using PagedList;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace OnParTicketApp.Controllers
{
    public class AccountController : Controller
    {
        private SqlConnection con;
        private string constr;

        private void DbConnection()
        {
            constr = ConfigurationManager.ConnectionStrings["TicketAppDB"].ToString();
            con = new SqlConnection(constr);
        }
        // GET: Account
        public ActionResult Index()
        {
            return Redirect("~/account/login");
        }

        // GET: /account/login
        [HttpGet]
        public ActionResult Login()
        {
            // Confirm user is not logged in

            string username = User.Identity.Name;

            if (!string.IsNullOrEmpty(username))
                return RedirectToAction("user-profile");

            // Return view
            return View();
        }

        // POST: /account/login
        [HttpPost]
        public ActionResult Login(LoginUserVM model)
        {
            // Check model state
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check if the user is valid

            bool isValid = false;

            using (TicketAppDB db = new TicketAppDB())
            {
                if (db.Users.Any(x => x.Username.Equals(model.Username) && x.Password.Equals(model.Password)))
                {
                    isValid = true;
                }

                List<ProductDTO> prodList = db.Products.ToList();

                OrderDTO order = new OrderDTO();
                if (prodList != null)
                {
                    foreach (ProductDTO prod in prodList)
                    {
                        if (prod.ReservationDate < DateTime.Now.Date.AddDays(-1))
                        {
                            OrderDetailsDTO detail = db.OrderDetails.Where(x => x.ProductId == prod.Id).FirstOrDefault();
                            if (detail != null)
                            {
                                order = db.Orders.Where(x => x.OrderId == detail.OrderId).FirstOrDefault();
                                db.Orders.Remove(order);
                                db.OrderDetails.Remove(detail);
                            }
                            PhotoDTO photo = db.Photos.Where(x => x.ProductId == prod.Id).FirstOrDefault();
                            PdfDTO pdf = db.Pdfs.Where(x => x.ProductId == prod.Id).FirstOrDefault();
                            if (photo != null)
                            {
                                db.Pdfs.Remove(pdf);
                                db.Photos.Remove(photo);
                            }

                            db.Products.Remove(prod);
                            db.SaveChanges();
                        }
                    }
                }
               
            }

            if (!isValid)
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View(model);
            }
            else
            {
                FormsAuthentication.SetAuthCookie(model.Username, model.RememberMe);
                return Redirect(FormsAuthentication.GetRedirectUrl(model.Username, model.RememberMe));
            }
        }

        // GET: /account/create-account
        [ActionName("create-account")]
        [HttpGet]
        public ActionResult CreateAccount()
        {
            return View("CreateAccount");
        }

        // POST: /account/create-account
        [ActionName("create-account")]
        [HttpPost]
        public ActionResult CreateAccount(UserVM model)
        {
            // Check model state
            if (!ModelState.IsValid)
            {
                return View("CreateAccount", model);
            }

            // Check if passwords match
            if (!model.Password.Equals(model.ConfirmPassword))
            {
                ModelState.AddModelError("", "Passwords do not match.");
                return View("CreateAccount", model);
            }

            using (TicketAppDB db = new TicketAppDB())
            {
                // Make sure username is unique
                if (db.Users.Any(x => x.Username.Equals(model.Username)))
                {
                    ModelState.AddModelError("", "Username " + model.Username + " is taken.");
                    model.Username = "";
                    return View("CreateAccount", model);
                }

                // Create userDTO
                UserDTO userDTO = new UserDTO()
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    EmailAddress = model.EmailAddress,
                    Username = model.Username,
                    Password = model.Password
                };

                // Add the DTO
                db.Users.Add(userDTO);

                // Save
                db.SaveChanges();

                // Add to UserRolesDTO
                int id = userDTO.Id;

                UserRoleDTO userRolesDTO = new UserRoleDTO()
                {
                    UserId = id,
                    RoleId = 2
                };

                db.UserRoles.Add(userRolesDTO);
                db.SaveChanges();
            }

            // Create a TempData message
            TempData["SM"] = "You are now registered and can login.";

            // Redirect
            return Redirect("~/account/login");
        }

        // GET: /account/Logout
        [Authorize]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return Redirect("~/account/login");
        }

        [Authorize]
        public ActionResult UserNavPartial()
        {
            // Get username
            string username = User.Identity.Name;

            // Declare model
            UserNavPartialVM model;

            using (TicketAppDB db = new TicketAppDB())
            {
                // Get the user
                UserDTO dto = db.Users.FirstOrDefault(x => x.Username == username);

                // Build the model
                model = new UserNavPartialVM()
                {
                    FirstName = dto.FirstName,
                    LastName = dto.LastName
                };
            }

            // Return partial view with model
            return PartialView(model);
        }

        // GET: /account/user-profile
        [HttpGet]
        [ActionName("user-profile")]
        [Authorize]
        public ActionResult UserProfile()
        {
            // Get username
            string username = User.Identity.Name;

            // Declare model
            UserProfileVM model;

            using (TicketAppDB db = new TicketAppDB())
            {
                // Get user
                UserDTO dto = db.Users.FirstOrDefault(x => x.Username == username);

                // Build model
                model = new UserProfileVM(dto);
            }

            // Return view with model
            return View("UserProfile", model);
        }

        // POST: /account/user-profile
        [HttpPost]
        [ActionName("user-profile")]
        [Authorize]
        public ActionResult UserProfile(UserProfileVM model)
        {
            // Check model state
            if (!ModelState.IsValid)
            {
                return View("UserProfile", model);
            }

            // Check if passwords match if need be
            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                if (!model.Password.Equals(model.ConfirmPassword))
                {
                    ModelState.AddModelError("", "Passwords do not match.");
                    return View("UserProfile", model);
                }
            }

            using (TicketAppDB db = new TicketAppDB())
            {
                // Get username
                string username = User.Identity.Name;

                // Make sure username is unique
                if (db.Users.Where(x => x.Id != model.Id).Any(x => x.Username == username))
                {
                    ModelState.AddModelError("", "Username " + model.Username + " already exists.");
                    model.Username = "";
                    return View("UserProfile", model);
                }

                // Edit DTO
                UserDTO dto = db.Users.Find(model.Id);

                dto.FirstName = model.FirstName;
                dto.LastName = model.LastName;
                dto.EmailAddress = model.EmailAddress;
                dto.Username = model.Username;

                if (!string.IsNullOrWhiteSpace(model.Password))
                {
                    dto.Password = model.Password;
                }

                // Save
                db.SaveChanges();
            }

            // Set TempData message
            TempData["SM"] = "You have edited your profile!";

            // Redirect
            return Redirect("~/account/user-profile");
        }

        //Get: //acount/Products
        [Authorize(Roles = "User")]
        public ActionResult Products()
        {
            List<ProductsForUserVM> productsForUser = new List<ProductsForUserVM>();

            using (TicketAppDB db = new TicketAppDB())
            {
                //Get user id
                UserDTO user = db.Users.Where(x => x.Username == User.Identity.Name).FirstOrDefault();
                int userId = user.Id;

                //Init List of OrderVM
                List<ProductVM> products = db.Products.Where(x => x.User.Id == userId).ToArray().Select(x => new ProductVM(x)).ToList();

                foreach (var product in products)
                {
                    productsForUser.Add(new ProductsForUserVM()
                    {
                        ProductId = product.Id,
                        Name = product.Name,
                        Price = product.Price,
                        CategoryName = product.CategoryName,
                        ReservationDate = product.ReservationDate,
                        Verified = product.Verified
                    });
                }
            }
                
            return View(productsForUser);
        }

        [HttpGet]
        public ActionResult EditProduct(int id)
        {
            // Declare productVM
            ProductVM model;

            using (TicketAppDB db = new TicketAppDB())
            {
                // Get the product
                ProductDTO dto = db.Products.Find(id);

                // Make sure product exists
                if (dto == null)
                {
                    return Content("That product does not exist.");
                }

                // init model
                model = new ProductVM(dto);

                // Make a select list
                model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
            }

            // Return view with model
            return View(model);
        }

        [HttpPost]
        public ActionResult EditProduct(ProductVM model, HttpPostedFileBase uploadPhoto, HttpPostedFileBase uploadPDF)
        {
            // Get product id
            int id = model.Id;

            // Populate categories select list and gallery images
            using (TicketAppDB db = new TicketAppDB())
            {
                model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
            }
            //model.GalleryImages = Directory.EnumerateFiles(Server.MapPath("~/Images/Uploads/Products/" + id + "/Gallery/Thumbs"))
            //                                    .Select(fn => Path.GetFileName(fn));

            // Check model state
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Make sure product name is unique
            using (TicketAppDB db = new TicketAppDB())
            {
                if (db.Products.Where(x => x.Id != id).Any(x => x.Name == model.Name))
                {
                    ModelState.AddModelError("", "That product name is taken!");
                    return View(model);
                }
            }

            using (TicketAppDB db = new TicketAppDB())
            {
                if (uploadPhoto != null && uploadPhoto.ContentLength > 0)
                {
                    var deleteCommand = "DELETE FROM tblPhoto WHERE ProductId = " + id + ";";
                    DbConnection();
                    using (SqlCommand cmd = new SqlCommand(deleteCommand, con))
                    {
                        con.Open();
                        cmd.ExecuteNonQuery();
                        con.Close();
                    }
                    var photo = new PhotoDTO
                    {
                        Name = System.IO.Path.GetFileName(uploadPhoto.FileName),
                        photoType = photoType.Picture,
                        ContentType = uploadPhoto.ContentType,
                        ProductId = id
                    };

                    string photoext = Path.GetExtension(photo.Name);
                    var strings = new List<string> { ".png", ".jpeg", ".gif", ".jpg" };
                    bool contains = strings.Contains(photoext, StringComparer.OrdinalIgnoreCase);
                    if (!contains)
                    {
                        model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
                        ModelState.AddModelError("", "That photo was not uploaded - wrong image extension.");
                        return View(model);
                    }
                    using (var reader2 = new System.IO.BinaryReader(uploadPhoto.InputStream))
                    {
                        photo.Data = reader2.ReadBytes(uploadPhoto.ContentLength);
                    }

                    model.Photos = new List<PhotoDTO> { photo };
                    db.Photos.Add(photo);
                    db.SaveChanges();
                }
            }

            using (TicketAppDB db = new TicketAppDB())
            {
                if (uploadPDF != null && uploadPDF.ContentLength > 0)
                {
                    var deleteCommand = "DELETE FROM tblPdf WHERE ProductId = " + id + ";";
                    DbConnection();
                    using (SqlCommand cmd = new SqlCommand(deleteCommand, con))
                    {
                        con.Open();
                        cmd.ExecuteNonQuery();
                        con.Close();
                    }
                    var invoice = new PdfDTO
                    {
                        Name = System.IO.Path.GetFileName(uploadPDF.FileName),
                        PdfType = PDFType.Invoice,
                        ContentType = uploadPDF.ContentType,
                        ProductId = id
                    };
                    string pdfext = Path.GetExtension(invoice.Name);

                    if (!pdfext.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
                        ModelState.AddModelError("", "That pdf was not uploaded - wrong Pdf extension.");
                        return View(model);
                    }
                    using (var reader = new System.IO.BinaryReader(uploadPDF.InputStream))
                    {
                        invoice.Data = reader.ReadBytes(uploadPDF.ContentLength);
                    }

                    model.Pdfs = new List<PdfDTO> { invoice };
                    db.Pdfs.Add(invoice);
                    db.SaveChanges();
                }
            }


            PdfDTO pdfs = new PdfDTO();
            PhotoDTO images = new PhotoDTO();
            string pdfsName;
            string imagesName;
            using (TicketAppDB db = new TicketAppDB())
            {
                pdfs = db.Pdfs.Where(x => x.ProductId == id).FirstOrDefault();
                pdfsName = pdfs.Name;
                images = db.Photos.Where(x => x.ProductId == id).FirstOrDefault();
                imagesName = pdfs.Name;
            }
            if (uploadPDF != null)
            {
                pdfsName = uploadPDF.FileName;
            }

            if (uploadPhoto != null)
            {
                imagesName = uploadPhoto.FileName;
            }
            // Update product
            using (TicketAppDB db = new TicketAppDB())
            {
                ProductDTO dto = db.Products.Find(id);
                UserDTO user = db.Users.Where(x => x.Username == User.Identity.Name).FirstOrDefault();
                dto.Name = model.Name;
                dto.Slug = model.Name.Replace(" ", "-").ToLower();
                dto.Description = model.Description;
                dto.ReservationDate = model.ReservationDate;
                dto.Verified = model.Verified;
                dto.PdfName = pdfsName;
                dto.ImageName = imagesName;
                dto.Price = model.Price;
                dto.CategoryId = model.CategoryId;
                dto.UserId = user.Id;

                CategoryDTO catDTO = db.Categories.FirstOrDefault(x => x.Id == model.CategoryId);
                dto.CategoryName = catDTO.Name;

                db.SaveChanges();
            }

            // Set TempData message
            TempData["SM"] = "You have edited a listing!";


            // Redirect
            return RedirectToAction("EditProduct");
        }

        // GET: Admin/Shop/DeleteProduct/id
        public ActionResult DeleteProduct(int id)
        {
            // Delete product from DB
            using (TicketAppDB db = new TicketAppDB())
            {
                ProductDTO dto = db.Products.Find(id);
                db.Products.Remove(dto);

                db.SaveChanges();
            }

            // Delete product folder
            var originalDirectory = new DirectoryInfo(string.Format("{0}Images\\Uploads", Server.MapPath(@"\")));
            string pathString = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString());

            if (Directory.Exists(pathString))
                Directory.Delete(pathString, true);

            // Redirect
            return RedirectToAction("Products");
        }

        // GET: /account/Orders
        [Authorize(Roles = "User")]
        public ActionResult Orders()
        {
            // Init list of OrdersForUserVM
            List<OrdersForUserVM> ordersForUser = new List<OrdersForUserVM>();

            using (TicketAppDB db = new TicketAppDB())
            {
                // Get user id
                UserDTO user = db.Users.Where(x => x.Username == User.Identity.Name).FirstOrDefault();
                int userId = user.Id;

                // Init list of OrderVM
                List<OrderVM> orders = db.Orders.Where(x => x.UserId == userId).ToArray().Select(x => new OrderVM(x)).ToList();

                // Loop through list of OrderVM
                foreach (var order in orders)
                {
                    // Init products dict
                    Dictionary<string, int> productsAndQty = new Dictionary<string, int>();

                    // Declare total
                    decimal total = 0m;

                    // Init list of OrderDetailsDTO
                    List<OrderDetailsDTO> orderDetailsDTO = db.OrderDetails.Where(x => x.OrderId == order.OrderId).ToList();

                    //Init Seller Name
                    string seller = "";

                    ProductDTO product = new ProductDTO();
                    // Loop though list of OrderDetailsDTO
                    foreach (var orderDetails in orderDetailsDTO)
                    {
                        // Get product
                        product = db.Products.Where(x => x.Id == orderDetails.ProductId).FirstOrDefault();

                        // Get product price
                        decimal price = product.Price;

                        //Get seller name
                        seller = product.User.Username;

                        // Get product name
                        string productName = product.Name;

                        // Add to products dict
                        productsAndQty.Add(productName, orderDetails.Quantity);

                        // Get total
                        total += orderDetails.Quantity * price;
                    }

                    // Add to OrdersForUserVM list
                    ordersForUser.Add(new OrdersForUserVM()
                    {
                        OrderNumber = order.OrderId,
                        Total = total,
                        SellerName = seller,
                        ProductsAndQty = productsAndQty,
                        CreatedAt = order.CreatedAt
                    });
                }

            }

            // Return view with list of OrdersForUserVM
            return View(ordersForUser);
        }
    }
}