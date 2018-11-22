using OnParTicketApp.Models.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OnParTicketApp.Models.ViewModels.Shop
{
    public class MyDate : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            DateTime _pickedDate = Convert.ToDateTime(value);
            if (_pickedDate >= DateTime.Today)
            {
                return ValidationResult.Success;
            }
            else
            {
                return new ValidationResult(ErrorMessage);
            }
        }
    }

    public class ProductVM
    {
        public ProductVM()
        {

        }

        public ProductVM(ProductDTO row)
        {
            Id = row.Id;
            Name = row.Name;
            Slug = row.Slug;
            Description = row.Description;
            Price = row.Price;
            CategoryName = row.CategoryName;
            CategoryId = row.CategoryId;
            ImageName = row.ImageName;
            PdfName = row.PdfName;
            ReservationDate = row.ReservationDate;
            Verified = row.Verified;
        }


        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string Slug { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public decimal Price { get; set; }
        public string CategoryName { get; set; }
        [Required]
        public int CategoryId { get; set; }
        public string ImageName { get; set; }
        public string PdfName { get; set; }
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [DataType(DataType.Date)]
        [MyDate(ErrorMessage = "Please pick a date no earlier than today")]
        [Required]
        public DateTime? ReservationDate { get; set; }
        public Verified Verified { get; set; }

        public IEnumerable<SelectListItem> Categories { get; set; }
        public IEnumerable<string> GalleryImages { get; set; }
        public IEnumerable<PdfDTO> Pdfs { get; set; }
        public IEnumerable<PhotoDTO> Photos { get; set; }
    }
}