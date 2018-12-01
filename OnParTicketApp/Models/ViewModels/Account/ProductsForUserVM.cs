using OnParTicketApp.Models.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OnParTicketApp.Models.ViewModels.Account
{
    public class ProductsForUserVM
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string CategoryName { get; set; }
        public DateTime? ReservationDate { get; set; }
        public Verified Verified { get; set; }
        public string Username { get; set; }
    }
}