namespace OnParTicketApp.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialMigrate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CategoryDTO",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        Slug = c.String(),
                        Sorting = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.OrderDetailsDTO",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        OrderId = c.Int(nullable: false),
                        UserId = c.Int(nullable: false),
                        ProductId = c.Int(nullable: false),
                        Quantity = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.OrderDTO", t => t.OrderId)
                .ForeignKey("dbo.ProductDTO", t => t.ProductId)
                .ForeignKey("dbo.UserDTO", t => t.UserId)
                .Index(t => t.OrderId)
                .Index(t => t.UserId)
                .Index(t => t.ProductId);
            
            CreateTable(
                "dbo.OrderDTO",
                c => new
                    {
                        OrderId = c.Int(nullable: false, identity: true),
                        UserId = c.Int(nullable: false),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.OrderId)
                .ForeignKey("dbo.UserDTO", t => t.UserId)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.UserDTO",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        FirstName = c.String(),
                        LastName = c.String(),
                        EmailAddress = c.String(),
                        Username = c.String(),
                        Password = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.ProductDTO",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        Slug = c.String(),
                        Description = c.String(),
                        Price = c.Decimal(nullable: false, precision: 18, scale: 2),
                        CategoryName = c.String(),
                        CategoryId = c.Int(nullable: false),
                        ImageName = c.String(),
                        PdfName = c.String(),
                        ReservationDate = c.DateTime(),
                        Verified = c.Int(nullable: false),
                        UserId = c.Int(nullable: false),
                        IsSold = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CategoryDTO", t => t.CategoryId)
                .ForeignKey("dbo.UserDTO", t => t.UserId)
                .Index(t => t.CategoryId)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.PageDTO",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Title = c.String(),
                        Slug = c.String(),
                        Body = c.String(),
                        Sorting = c.Int(nullable: false),
                        HasSideBar = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.PdfDTO",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        ContentType = c.String(),
                        Data = c.Binary(),
                        PdfType = c.Int(nullable: false),
                        ProductId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.ProductDTO", t => t.ProductId)
                .Index(t => t.ProductId);
            
            CreateTable(
                "dbo.PhotoDTO",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        ContentType = c.String(),
                        Data = c.Binary(),
                        photoType = c.Int(nullable: false),
                        ProductId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.ProductDTO", t => t.ProductId)
                .Index(t => t.ProductId);
            
            CreateTable(
                "dbo.RoleDTO",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.SidebarDTO",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Body = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.UserRoleDTO",
                c => new
                    {
                        UserId = c.Int(nullable: false),
                        RoleId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.UserId, t.RoleId })
                .ForeignKey("dbo.RoleDTO", t => t.RoleId)
                .ForeignKey("dbo.UserDTO", t => t.UserId)
                .Index(t => t.UserId)
                .Index(t => t.RoleId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.UserRoleDTO", "UserId", "dbo.UserDTO");
            DropForeignKey("dbo.UserRoleDTO", "RoleId", "dbo.RoleDTO");
            DropForeignKey("dbo.PhotoDTO", "ProductId", "dbo.ProductDTO");
            DropForeignKey("dbo.PdfDTO", "ProductId", "dbo.ProductDTO");
            DropForeignKey("dbo.OrderDetailsDTO", "UserId", "dbo.UserDTO");
            DropForeignKey("dbo.OrderDetailsDTO", "ProductId", "dbo.ProductDTO");
            DropForeignKey("dbo.ProductDTO", "UserId", "dbo.UserDTO");
            DropForeignKey("dbo.ProductDTO", "CategoryId", "dbo.CategoryDTO");
            DropForeignKey("dbo.OrderDetailsDTO", "OrderId", "dbo.OrderDTO");
            DropForeignKey("dbo.OrderDTO", "UserId", "dbo.UserDTO");
            DropIndex("dbo.UserRoleDTO", new[] { "RoleId" });
            DropIndex("dbo.UserRoleDTO", new[] { "UserId" });
            DropIndex("dbo.PhotoDTO", new[] { "ProductId" });
            DropIndex("dbo.PdfDTO", new[] { "ProductId" });
            DropIndex("dbo.ProductDTO", new[] { "UserId" });
            DropIndex("dbo.ProductDTO", new[] { "CategoryId" });
            DropIndex("dbo.OrderDTO", new[] { "UserId" });
            DropIndex("dbo.OrderDetailsDTO", new[] { "ProductId" });
            DropIndex("dbo.OrderDetailsDTO", new[] { "UserId" });
            DropIndex("dbo.OrderDetailsDTO", new[] { "OrderId" });
            DropTable("dbo.UserRoleDTO");
            DropTable("dbo.SidebarDTO");
            DropTable("dbo.RoleDTO");
            DropTable("dbo.PhotoDTO");
            DropTable("dbo.PdfDTO");
            DropTable("dbo.PageDTO");
            DropTable("dbo.ProductDTO");
            DropTable("dbo.UserDTO");
            DropTable("dbo.OrderDTO");
            DropTable("dbo.OrderDetailsDTO");
            DropTable("dbo.CategoryDTO");
        }
    }
}
