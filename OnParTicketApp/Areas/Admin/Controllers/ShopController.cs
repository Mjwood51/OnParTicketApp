using OnParTicketApp.Models.Data;
using OnParTicketApp.Models.ViewModels.Shop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using PagedList;
using System.Data.SqlClient;
using System.Configuration;
using Dapper;
using System.Data;
using OnParTicketApp.Areas.Admin.Models.ViewModels.Shop;
using Microsoft.AspNet.Identity;

namespace OnParTicketApp.Areas.Admin.Controllers
{

    public class ShopController : Controller
    {
        
        private SqlConnection con;
        private string constr;

        private void DbConnection()
        {
            constr = ConfigurationManager.ConnectionStrings["TicketAppDB"].ToString();
            con = new SqlConnection(constr);
        }

        // GET: Admin/Shop/Categories
        [HttpGet]
        public ActionResult Categories()
        {
            //Declare a list of models
            List<CategoryVM> categoryVMList;

            using (TicketAppDB db = new TicketAppDB())
            {
                //Init list
                categoryVMList = db.Categories
                                .ToArray()
                                .OrderBy(x => x.Sorting)
                                .Select(x => new CategoryVM(x))
                                .ToList();
            }


            //Return view with list
            return View(categoryVMList);
        }

        // POST: Admin/Shop/AddNewCategory
        [HttpPost]
        public string AddNewCategory(string catName)
        {
            //Declare id
            string id;

            using (TicketAppDB db = new TicketAppDB())
            {
                //Check that the category name is unique
                if (db.Categories.Any(x => x.Name == catName))
                    return "titletaken";

                //Init DTO
                CategoryDTO dto = new CategoryDTO();

                //Add to DTO
                dto.Name = catName;
                dto.Slug = catName.Replace(" ", "-").ToLower();
                dto.Sorting = 100;

                //Save DTO
                db.Categories.Add(dto);
                db.SaveChanges();

                //Get the id
                id = dto.Id.ToString();
            }
            //Return id
            return id;
        }

        // POST:  Admin/Shop/ReorderCategories
        [HttpPost]
        public void ReorderCategories(int[] id)
        {
            using (TicketAppDB db = new TicketAppDB())
            {
                //Set initial count
                int count = 1;

                //Declare CategoryDTO
                CategoryDTO dto;

                //Set sorting for each page
                foreach (var catId in id)
                {
                    dto = db.Categories.Find(catId);
                    dto.Sorting = count;

                    db.SaveChanges();

                    count++;
                }
            }

        }

        // GET:  Admin/Shop/DeleteCategory/id
        public ActionResult DeleteCategory(int id)
        {
            using (TicketAppDB db = new TicketAppDB())
            {
                //Get the page
                CategoryDTO dto = db.Categories.Find(id);
                List<ProductDTO> prod = db.Products.Where(x=>x.CategoryId == id).ToList();
                if (prod != null)
                {
                    foreach (ProductDTO pr in prod)
                    {
                        if(db.OrderDetails.Any(x=>x.ProductId == pr.Id))
                        { 
                            OrderDetailsDTO dte = db.OrderDetails.Where(x => x.ProductId == pr.Id).FirstOrDefault();
                            OrderDTO ord = db.Orders.Where(x => x.OrderId == dte.OrderId).FirstOrDefault();
                            db.OrderDetails.Remove(dte);
                            db.Orders.Remove(ord);
                        }

                        db.Products.Remove(pr);
                    }
                } 
                //Remove the category
                db.Categories.Remove(dto);

                //Save
                db.SaveChanges();
            }
            TempData["SM"] = "You have deleted a category!";

            //Redirect
            return RedirectToAction("Categories");
        }

        // POST:  Admin/Shop/RenameCategory
        [HttpPost]
        public string RenameCategory(string newCatName, int id)
        {

            using (TicketAppDB db = new TicketAppDB())
            {
                //Check category name is unique
                if (db.Categories.Any(x => x.Name == newCatName))
                {
                    return "titletaken";
                }

                //Get DTO
                CategoryDTO dto = db.Categories.Find(id);

                //Edit DTO
                dto.Name = newCatName;
                dto.Slug = newCatName.Replace(" ", "-").ToLower();

                //Save
                db.SaveChanges();
            }

            //Return
            return "ok";
        }

        // GET:  Admin/Shop/Products
        public ActionResult Products(int? page, int? catId)
        {
            //Declare a list of ProductVM
            List<ProductVM> listOfProductVM;

            //Set page number
            var pageNumber = page ?? 1;

            using (TicketAppDB db = new TicketAppDB())
            {
                //Get seller name
                //Init the list
                listOfProductVM = db.Products.ToArray()
                                  .Where(x => catId == null || catId == 0 || x.CategoryId == catId)
                                  .Select(x => new ProductVM(x))
                                  .ToList();

                //Populate categories select list
                ViewBag.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");

                //Set selected category
                ViewBag.SelectedCat = catId.ToString();

            }


            //Set pagination
            var onePageOfProducts = listOfProductVM.ToPagedList(pageNumber, 3);
            ViewBag.OnePageOfProducts = onePageOfProducts;

            //Return view with list
            return View(listOfProductVM);
        }

        [HttpGet]
        public FileResult DownloadPdf(int id)
        {
            List<PdfDTO> objPdfs = GetPDFList();
            var PDFById = (from PC in objPdfs
                           where PC.ProductId.Equals(id)
                           select new { PC.Name, PC.Data }).ToList().FirstOrDefault();
            return File(PDFById.Data, "application/pdf", PDFById.Name);

        }

        [HttpGet]
        public FileResult DownloadPhoto(int id)
        {
            List<PhotoDTO> objPhoto = GetPhotoList();
            var PhotoById = (from PH in objPhoto
                             where PH.ProductId.Equals(id)
                             select new { PH.Name, PH.Data }).ToList().FirstOrDefault();
            return File(PhotoById.Data, "image/png", PhotoById.Name);

        }

        [HttpGet]
        public PartialViewResult PDFDetails()
        {
            List<PdfDTO> pdfList = GetPDFList();
            return PartialView("PDFDetails", pdfList);
        }

        [HttpGet]
        public PartialViewResult PhotoDetails()
        {
            List<PhotoDTO> pdfList = GetPhotoList();
            return PartialView("PhotoDetails", pdfList);
        }

        [HttpGet]
        public ActionResult GetImage(int id)
        {
            List<PhotoDTO> obPhoto = GetPhotoList();
            var photo = (from IM in obPhoto
                         where IM.ProductId.Equals(id)
                         select IM.Data).FirstOrDefault();


            return File(photo, "image/jpg");
        }

        private List<PhotoDTO> GetPhotoList()
        {
            List<PhotoDTO> photoList = new List<PhotoDTO>();
            DbConnection();
            con.Open();
            photoList = SqlMapper.Query<PhotoDTO>(con, "GetPhotoDetails", commandType: CommandType.StoredProcedure).ToList();
            con.Close();
            return photoList;
        }

        private List<PdfDTO> GetPDFList()
        {
            List<PdfDTO> pdfList = new List<PdfDTO>();
            DbConnection();
            con.Open();
            pdfList = SqlMapper.Query<PdfDTO>(con, "GetPDFDetails", commandType: CommandType.StoredProcedure).ToList();
            con.Close();
            return pdfList;
        }

        private void SavePDFDetails(PdfDTO objPdf)
        {
            DynamicParameters pdfParam = new DynamicParameters();
            pdfParam.Add("@Name", objPdf.Name);
            pdfParam.Add("@Data", objPdf.Data);
            DbConnection();
            con.Open();
            con.Execute("AddPDFDetails", pdfParam, commandType: System.Data.CommandType.StoredProcedure);
            con.Close();
        }

        private void SavePhotoDetails(PhotoDTO objPhoto)
        {
            DynamicParameters photoParam = new DynamicParameters();
            photoParam.Add("@Name", objPhoto.Name);
            photoParam.Add("@Data", objPhoto.Data);
            DbConnection();
            con.Open();
            con.Execute("AddPhotoDetails", photoParam, commandType: System.Data.CommandType.StoredProcedure);
            con.Close();
        }

        // GET: Admin/Shop/EditProduct/id
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

                // Get all gallery images
                /*model.GalleryImages = Directory.EnumerateFiles(Server.MapPath("~/Images/Uploads/Products/" + id + "/Gallery/Thumbs"))
                                                .Select(fn => Path.GetFileName(fn));*/
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
                pdfs = db.Pdfs.Where(x=>x.ProductId == id).FirstOrDefault();
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
                dto.Name = model.Name;
                dto.Slug = model.Name.Replace(" ", "-").ToLower();
                dto.Description = model.Description;
                dto.ReservationDate = model.ReservationDate;
                dto.Verified = model.Verified;
                dto.PdfName = pdfsName;
                dto.ImageName = imagesName;
                dto.Price = model.Price;
                dto.CategoryId = model.CategoryId;

                CategoryDTO catDTO = db.Categories.FirstOrDefault(x => x.Id == model.CategoryId);
                //dto.CategoryName = catDTO.Name;

                db.SaveChanges();
            }

            // Set TempData message
            TempData["SM"] = "You have edited a listing!";


            // Redirect
            return RedirectToAction("Products", "Shop");
        }

        // GET: Admin/Shop/DeleteProduct/id
        public ActionResult DeleteProduct(int id)
        {
            // Delete product from DB
            using (TicketAppDB db = new TicketAppDB())
            {
                ProductDTO dto = db.Products.Find(id);
                if (db.OrderDetails.Any(x => x.ProductId == id))
                {
                    OrderDetailsDTO dte = db.OrderDetails.Where(x => x.ProductId == id).FirstOrDefault();
                    OrderDTO ord = db.Orders.Where(x => x.OrderId == dte.OrderId).FirstOrDefault();
                    db.OrderDetails.Remove(dte);
                    db.Orders.Remove(ord);
                }
                db.Products.Remove(dto);

                db.SaveChanges();
            }
            TempData["SM"] = "You have deleted a listing!";

            // Redirect
            return RedirectToAction("Products");
        }


        // GET: Admin/Shop/Orders
        public ActionResult Orders()
        {
            // Init list of OrdersForAdminVM
            List<OrdersForAdminVM> ordersForAdmin = new List<OrdersForAdminVM>();

            using (TicketAppDB db = new TicketAppDB())
            {
                // Init list of OrderVM
                List<OrderVM> orders = db.Orders.ToArray().Select(x => new OrderVM(x)).ToList();

                // Loop through list of OrderVM
                foreach (var order in orders)
                {
                    // Init product dict
                    Dictionary<string, int> productsAndQty = new Dictionary<string, int>();

                    // Declare total
                    decimal total = 0m;

                    // Init list of OrderDetailsDTO
                    List<OrderDetailsDTO> orderDetailsList = db.OrderDetails.Where(X => X.OrderId == order.OrderId).ToList();

                    // Get username of buyer
                    UserDTO user = db.Users.Where(x => x.Id == order.UserId).FirstOrDefault();
                    string username = user.Username;

                    //string buyerName = "";
                    string buyer = "";
                    // Loop through list of OrderDetailsDTO
                    foreach (var orderDetails in orderDetailsList)
                    {
                        // Get product
                        ProductDTO product = db.Products.Where(x => x.Id == orderDetails.ProductId).FirstOrDefault();

                        //Get username of seller
                        UserDTO buyerName = db.Users.Where(x=>x.Username == product.User.Username).FirstOrDefault();
                        buyer = buyerName.Username;
                        // Get product price
                        decimal price = product.Price;

                        // Get product name
                        string productName = product.Name;

                        // Add to product dict
                        productsAndQty.Add(productName, orderDetails.Quantity);

                        // Get total
                        total += orderDetails.Quantity * price;
                    }

                    // Add to ordersForAdminVM list
                    ordersForAdmin.Add(new OrdersForAdminVM()
                    {
                        OrderNumber = order.OrderId,
                        BuyerName = username,
                        SellerName = buyer,
                        Total = total,
                        ProductsAndQty = productsAndQty,
                        CreatedAt = order.CreatedAt
                    });
                }
            }

            // Return view with OrdersForAdminVM list
            return View(ordersForAdmin);
        }


    }

        
}