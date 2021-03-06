﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OnParTicketApp.Areas.Admin.Models.ViewModels.Shop
{
    public class OrdersForAdminVM
    {
        public int OrderNumber { get; set; }
        public string BuyerName { get; set; }
        public string SellerName { get; set; }
        public decimal Total { get; set; }
        public Dictionary<string, int> ProductsAndQty { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}