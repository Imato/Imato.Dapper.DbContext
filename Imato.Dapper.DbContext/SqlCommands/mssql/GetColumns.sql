select name 
	from sys.columns c 
	where c.object_id = object_id(tableName) 
		and c.is_computed = 0 order by 1;