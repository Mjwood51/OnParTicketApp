﻿using Dapper;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using OnParTicketApp.Models.Data;
using OnParTicketApp.Models.ViewModels.Shop;
using PagedList;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace OnParTicketApp.Controllers
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
        // GET: Shop
        public ActionResult Index()
        {
            return RedirectToAction("Index", "Pages");
        }

        public ActionResult CategoryMenuPartial()
        {
            // Declare list of CategoryVM
            List<CategoryVM> categoryVMList;

            // Init the list
            using (TicketAppDB db = new TicketAppDB())
            {
                categoryVMList = db.Categories.ToArray().OrderBy(x => x.Sorting).Select(x => new CategoryVM(x)).ToList();
            }

            // Return partial with list
            return PartialView(categoryVMList);
        }

        // GET: /shop/category/name
        public ActionResult Category(int? page, int? catId, string name)
        {
            // Declare a list of ProductVM
            List<ProductVM> productVMList;

            //init page list
            //Set page number
            var pageNumber = page ?? 1;

            using (TicketAppDB db = new TicketAppDB())
            {
                // Get category id
                CategoryDTO categoryDTO = db.Categories.Where(x => x.Slug == name).FirstOrDefault();
                catId = categoryDTO.Id;

                // Init the list
                productVMList = db.Products.ToArray().Where(x => x.CategoryId == catId && x.IsSold == false && x.Verified != 0).Select(x => new ProductVM(x)).ToList();

                // Get category name
                var productCat = db.Products.Where(x => x.CategoryId == catId).FirstOrDefault();

                if (productCat != null)
                {
                    ViewBag.CategoryName = productCat.CategoryName;
                }

                //Populate categories select list
                ViewBag.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");

                //Set selected category
                ViewBag.SelectedCat = catId.ToString();
            }

            

            //Set pagination
            var onePageOfProducts = productVMList.ToPagedList(pageNumber, 3);
            ViewBag.OnePageOfProducts = onePageOfProducts;

            // Return view with list
            return View(productVMList);
        }

        public ActionResult Products(int? page, int? catId)
        {
            
            //Declare a list of ProductVM
            List<ProductVM> listOfProductVM;

            //Set page number
            var pageNumber = page ?? 1;

            using (TicketAppDB db = new TicketAppDB())
            {
                //Get current user
                UserDTO user = db.Users.Where(x => x.Username == User.Identity.Name).FirstOrDefault();
                int userId = user.Id;

                //Init the list
                listOfProductVM = db.Products.ToArray()
                                  .Where(x=>x.UserId == userId) 
                                  .Select(x => new ProductVM(x))
                                  .ToList();

                foreach (ProductVM product in listOfProductVM)
                {
                    product.Username = user.Username;
                }

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

        // GET:  Admin/Shop/AddProduct
        [Authorize]
        [HttpGet]
        public ActionResult AddProduct()
        {
            //Init model
            ProductVM model = new ProductVM();

            //Add select list of categories to model
            using (TicketAppDB db = new TicketAppDB())
            {
                model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
            }

            //Return view with model
            return View(model);
        }

        // POST:  Admin/Shop/AddProduct
        [Authorize]
        [HttpPost]
        public ActionResult AddProduct(ProductVM model, HttpPostedFileBase uploadPDF, HttpPostedFileBase uploadPhoto)
        {
            string UserID = User.Identity.Name;
            HttpPostedFileBase photobase = uploadPhoto;
            HttpPostedFileBase pdfbase = uploadPDF;
            //Check model state
            if (!ModelState.IsValid)
            {
                using (TicketAppDB db = new TicketAppDB())
                {
                    model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
                    return View(model);
                }

            }
            //Make sure product name is unique
            using (TicketAppDB db = new TicketAppDB())
            {
                if (db.Products.Any(x => x.Name == model.Name))
                {
                    model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
                    ModelState.AddModelError("", "That product name is taken!");
                    return View(model);
                }

            }

            // Declare product id
            int id;
            string pdfsName = null;
            string imagesName = null;
            //Init image name
            if (uploadPDF != null)
            {
                pdfsName = uploadPDF.FileName;
            }
            if (uploadPhoto != null)
            {
                imagesName= uploadPhoto.FileName;
            }
            string name = "";
            using (TicketAppDB db = new TicketAppDB())
            {
                //Init and save product DTO
                ProductDTO product = new ProductDTO();
                var userId = from p in db.Users
                             where p.Username == UserID
                             select p.Id;
                product.Name = model.Name;
                name = model.Name;
                product.Slug = model.Name.Replace(" ", "-").ToLower();
                product.Description = model.Description;
                product.Price = model.Price;
                product.ReservationDate = model.ReservationDate;
                product.Verified = model.Verified;
                product.PdfName = pdfsName;
                product.ImageName = imagesName;
                product.CategoryId = model.CategoryId;
                CategoryDTO catDTO = db.Categories.FirstOrDefault(x => x.Id == model.CategoryId);
                product.CategoryName = catDTO.Name;
                product.UserId = userId.First();
                product.IsSold = false;
                

                db.Products.Add(product);
                db.SaveChanges();

                //Get the id
                id = product.Id;
            }

            using (TicketAppDB db = new TicketAppDB())
            {
                if (uploadPhoto != null && uploadPhoto.ContentLength > 0)
                {
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

            //Set TempData message
            TempData["SM"] = "You have added listing: '" + name + "'!";

            //Redirect
            return RedirectToAction("AddProduct");
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
        public ActionResult EditProduct(ProductVM model, HttpPostedFileBase uploadPhoto, HttpPostedFileBase uploadPDF, int id)
        {
            // Get product id
            id = model.Id;

            // Populate categories select list and gallery images
            using (TicketAppDB db = new TicketAppDB())
            {
                model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
            }

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
                imagesName = images.Name;
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
            string product = "";
            using (TicketAppDB db = new TicketAppDB())
            {
                ProductDTO dto = db.Products.Find(id);
                UserDTO user = db.Users.Where(x => x.Username == User.Identity.Name).FirstOrDefault();
                dto.Name = model.Name;
                product = model.Name;
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
            TempData["SM"] = "You have edited " + product + "'!";


            // Redirect
            return RedirectToAction("Products", "Shop");
        }

        // GET: Admin/Shop/DeleteProduct/id
        public ActionResult DeleteProduct(int id)
        {
            string product = "";
            // Delete product from DB
            using (TicketAppDB db = new TicketAppDB())
            {
                ProductDTO dto = db.Products.Find(id);
                PdfDTO pdf = db.Pdfs.Where(x => x.ProductId == id).FirstOrDefault();
                product = dto.Name;
                PhotoDTO photo = db.Photos.Where(x => x.ProductId == id).FirstOrDefault();
                //Determine if product is an order
                if (db.OrderDetails.Any(x => x.ProductId == id))
                {
                    OrderDetailsDTO dte = db.OrderDetails.Where(x => x.ProductId == id).FirstOrDefault();
                    OrderDTO ord = db.Orders.Where(x => x.OrderId == dte.OrderId).FirstOrDefault();
                    db.OrderDetails.Remove(dte);
                    db.Orders.Remove(ord);      
                }
                db.Pdfs.Remove(pdf);
                db.Photos.Remove(photo);
                db.Products.Remove(dto);

                db.SaveChanges();
            }

            TempData["SM"] = "You have deleted '"+ product +"'!";
            // Redirect
            return RedirectToAction("Products", "Shop");
        }

        // GET: /shop/product-details/name
        [ActionName("product-details")]
        public ActionResult ProductDetails(string name)
        {
            // Declare the VM and DTO
            ProductVM model;
            ProductDTO dto;

            // Init product id
            int id = 0;

            using (TicketAppDB db = new TicketAppDB())
            {
                // Check if product exists
                if (!db.Products.Any(x => x.Slug.Equals(name)))
                {
                    return RedirectToAction("Index", "Shop");
                }

                // Init productDTO
                dto = db.Products.Where(x => x.Slug == name).FirstOrDefault();

                // Get id
                id = dto.Id;

                // Init model
                model = new ProductVM(dto);
            }

            // Return view with model
            return View("ProductDetails", model);
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
    }
}