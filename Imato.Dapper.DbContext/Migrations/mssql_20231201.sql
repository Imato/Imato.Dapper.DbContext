use master
go

create proc sp_print
	@message varchar(max),
	@level varchar(25) = 'log'
as
begin
	set nocount on;

	declare @msg varchar(max) =
		format(getdate(), 'yyyy-MM-dd HH:mm:ss') + ' ' + @level + ': ' + @message;

	raiserror (@msg, 10, 1) with nowait;

end
go