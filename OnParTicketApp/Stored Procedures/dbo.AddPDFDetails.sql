CREATE PROCEDURE [dbo].[AddPDFDetails]
(
	@Name varchar(255),
	@ContentType varchar(100),
	@Data varBinary(Max),
	@PdfType int,
	@ProductId int

)
as
begin
Set NoCount on
Insert into tblPdf values (@Name,@ContentType,@Data,@PdfType,@ProductId)
END