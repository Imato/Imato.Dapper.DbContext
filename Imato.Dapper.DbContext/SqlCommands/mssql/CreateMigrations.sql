if object_id('dbo.db_migrations') is null 
	create table dbo.db_migrations 
	(id varchar(255) not null primary key, 
	date datetime not null);