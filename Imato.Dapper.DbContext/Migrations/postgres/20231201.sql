create or replace function instance_name()
returns text
as $$
declare result text;
begin
	create temp table tt_cmd (hostname text); 
	copy tt_cmd from program 'hostname'; 
	select hostname
		into result
		from tt_cmd; 
	drop table tt_cmd;	
	return result;
end
$$ language plpgsql security definer;


create or replace function iif(
	condition bool,
	value1 anyelement,
	value2 anyelement)
returns anyelement 
language plpgsql
as $$
begin 
	if condition 
		then 
		return value1;
		else 
		return value2;
	end if;	
end
$$;