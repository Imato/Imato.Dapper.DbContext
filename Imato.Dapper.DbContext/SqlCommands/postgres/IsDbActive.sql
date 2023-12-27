select result 
	from (select true as result 
					from pg_database d 
					where d.datname = @name 
					union all 
					select false) t 
	order by 1 desc limit 1;