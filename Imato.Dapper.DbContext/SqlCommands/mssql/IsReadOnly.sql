select top 1 cast(iif(e.value = 'true', 1, 0) as bit) 
	from sys.database_files f
		left join sys.extended_properties 
			e on e.name = 'ReadOnly' and e.class = 0