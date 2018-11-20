using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace OnParTicketApp.Models.Data
{
    [Table("tblPdf")]
    public class PdfDTO
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string ContentType { get; set; }
        public byte[] Data { get; set; }
        public PDFType pdfType { get; set; }
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual ProductDTO Product { get; set; }
    }
}