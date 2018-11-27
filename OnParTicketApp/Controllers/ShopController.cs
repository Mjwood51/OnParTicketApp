using Dapper;
using OnParTicketApp.Models.Data;
using OnParTicketApp.Models.ViewModels.Shop;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
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
        public ActionResult Category(string name)
        {
            // Declare a list of ProductVM
            List<ProductVM> productVMList;

            using (TicketAppDB db = new TicketAppDB())
            {
                // Get category id
                CategoryDTO categoryDTO = db.Categories.Where(x => x.Slug == name).FirstOrDefault();
                int catId = categoryDTO.Id;

                // Init the list
                productVMList = db.Products.ToArray().Where(x => x.CategoryId == catId).Select(x => new ProductVM(x)).ToList();

                // Get category name
                var productCat = db.Products.Where(x => x.CategoryId == catId).FirstOrDefault();
                ViewBag.CategoryName = productCat.CategoryName;
            }

            // Return view with list
            return View(productVMList);
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