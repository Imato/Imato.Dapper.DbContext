create temp table tt_cmd (hostname text);
copy tt_cmd from program 'hostname';
select * from tt_cmd;
drop table tt_cmd;