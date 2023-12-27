declare @status bit = 0; 
select @status = cast(1 as bit) 
	from sys.databases 
	where name = @name 
		and user_access_desc = 'MULTI_USER' 
		and state_desc = 'ONLINE'; 
select @status;