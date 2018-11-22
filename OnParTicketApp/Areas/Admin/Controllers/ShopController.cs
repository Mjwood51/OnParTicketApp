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

                //Remove the category
                db.Categories.Remove(dto);

                //Save
                db.SaveChanges();
            }


            //Redirect
            return RedirectToAction("Categories" +
                "");
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

        // GET:  Admin/Shop/AddProduct
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
        [HttpPost]
        public ActionResult AddProduct(ProductVM model, HttpPostedFileBase uploadPDF, HttpPostedFileBase uploadPhoto)
        {
            //Check model state
            if(!ModelState.IsValid)
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
                if(db.Products.Any(x=>x.Name == model.Name))
                {
                    model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
                    ModelState.AddModelError("", "That product name is taken!");
                    return View(model);
                }
                
            }
            // Declare product id
            int id;
            string pdfName = System.IO.Path.GetFileName(uploadPDF.FileName);
            //Init and save product DTO
            using (TicketAppDB db = new TicketAppDB())
            {
                ProductDTO product = new ProductDTO();

                if (uploadPDF != null && uploadPhoto != null && uploadPDF.ContentLength > 0 && uploadPhoto.ContentLength > 0)
                {
                    var invoice = new PdfDTO
                    {
                        Name = pdfName,
                        PdfType = PDFType.Invoice,
                        ContentType = uploadPDF.ContentType,
                        ProductId = model.Id
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



                    var photo = new PhotoDTO
                    {
                        Name = System.IO.Path.GetFileName(uploadPhoto.FileName),
                        photoType = photoType.Picture,
                        ContentType = uploadPhoto.ContentType,
                        ProductId = model.Id
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
                    model.Pdfs = new List<PdfDTO> { invoice };

                    db.Pdfs.Add(invoice);
                    db.Photos.Add(photo);

                    product.Name = model.Name;
                    product.Slug = model.Name.Replace(" ", "-").ToLower();
                    product.Description = model.Description;
                    product.Price = model.Price;
                    product.ReservationDate = model.ReservationDate;
                    product.Verified = 0;
                    product.CategoryId = model.CategoryId;
                    CategoryDTO catDTO = db.Categories.FirstOrDefault(x => x.Id == model.CategoryId);
                    product.CategoryName = catDTO.Name;

                    db.Products.Add(product);
                    db.SaveChanges();
                }
                else
                {
                    product.Name = model.Name;
                    product.Slug = model.Name.Replace(" ", "-").ToLower();
                    product.Description = model.Description;
                    product.Price = model.Price;
                    product.CategoryId = model.CategoryId;
                    CategoryDTO catDTO = db.Categories.FirstOrDefault(x => x.Id == model.CategoryId);
                    product.CategoryName = catDTO.Name;

                    db.Products.Add(product);
                    db.SaveChanges();
                }

                //Get the id
                id = product.Id;
            }

            


            //Set TempData message
            TempData["SM"] = "You have added a listing!";

            #region Upload Image

            //Create necessary directories
            var originalDirectory = new DirectoryInfo(string.Format("{0}Images\\Uploads", Server.MapPath(@"\")));

            
            var pathString1 = Path.Combine(originalDirectory.ToString(), "Products");
            var pathString2 = Path.Combine(originalDirectory.ToString(), "Products\\ " + id.ToString());
            var pathString3 = Path.Combine(originalDirectory.ToString(), "Products\\ " + id.ToString() + "\\Thumbs");
            var pathString4 = Path.Combine(originalDirectory.ToString(), "Products\\ " + id.ToString() + "\\Gallery");
            var pathString5 = Path.Combine(originalDirectory.ToString(), "Products\\ " + id.ToString() + "\\Gallery\\Thumbs");

            if (!Directory.Exists(pathString1))
                Directory.CreateDirectory(pathString1);

            if (!Directory.Exists(pathString2))
                Directory.CreateDirectory(pathString2);

            if (!Directory.Exists(pathString3))
                Directory.CreateDirectory(pathString3);

            if (!Directory.Exists(pathString4))
                Directory.CreateDirectory(pathString4);

            if (!Directory.Exists(pathString5))
                Directory.CreateDirectory(pathString5);
           
                //Check if a file was uploaded
                if (uploadPhoto != null && uploadPhoto.ContentLength > 0)
                {
                    //Get file extension
                    string ext = uploadPhoto.ContentType.ToLower();

                    //Verify extension
                    if (ext != "image/jpg" &&
                        ext != "image/jpeg" &&
                        ext != "image/pjpeg" &&
                        ext != "image/gif" &&
                        ext != "image/png" &&
                        ext != "image/x-png")
                    {
                        using (TicketAppDB db = new TicketAppDB())
                        {
                            model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
                            ModelState.AddModelError("", "That image was not uploaded - wrong image extension.");
                            return View(model);
                        }
                    }



                    //Init image name
                    string imageName = uploadPhoto.FileName;


                    //Save image and pdf names to DTO
                    using (TicketAppDB db = new TicketAppDB())
                    {
                        ProductDTO dto = db.Products.Find(id);
                        dto.PdfName = pdfName;
                        dto.ImageName = imageName;
                        db.SaveChanges();
                    }

                    //Set original and thumb image paths
                    var path = string.Format("{0}\\{1}", pathString2, imageName);
                    var path2 = string.Format("{0}\\{1}", pathString3, imageName);

                    //Save original
                    uploadPhoto.SaveAs(path);

                    //Create and save thumb
                    WebImage img = new WebImage(uploadPhoto.InputStream);
                    img.Resize(200, 200);
                    img.Save(path2);

                }

            #endregion

            //Redirect
            return RedirectToAction("AddProduct");
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
    }

        
}