CREATE PROCEDURE [dbo].[AddPhotoDetails]
	@Name varchar(255),
	@ContentType varchar(100),
	@Data varBinary(Max),
	@photoType int,
	@ProductId int
	
AS
begin
Set NoCount on
Insert into tblPhoto values(@Name,@ContentType,@Data,@photoType,@ProductId)
End