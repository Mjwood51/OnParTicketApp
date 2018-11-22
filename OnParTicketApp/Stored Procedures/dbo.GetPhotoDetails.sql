CREATE PROCEDURE [dbo].[GetPhotoDetails]
(
	@Id int = null
)	
AS
begin
SELECT Id,Name,Data,ProductId from tblPhoto 
where Id=isnull(@Id,Id)
End