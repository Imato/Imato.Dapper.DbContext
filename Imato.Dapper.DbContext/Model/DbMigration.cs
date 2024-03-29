﻿using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Imato.Dapper.DbContext
{
    [Table("db_migrations")]
    public class DbMigration
    {
        [ExplicitKey]
        public string Id { get; set; } = null!;

        public DateTime Date { get; set; }
    }
}