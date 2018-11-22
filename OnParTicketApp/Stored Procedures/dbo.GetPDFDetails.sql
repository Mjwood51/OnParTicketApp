CREATE PROCEDURE [dbo].[GetPDFDetails]
(
	@Id int = null
)
AS
BEGIN
select Id,Name,Data,ProductId from tblPdf 
where Id=isnull(@Id, Id)
END