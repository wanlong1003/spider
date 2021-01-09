using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Entities
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DefaultDbContext>
    {
        public DefaultDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<DefaultDbContext>();
            builder.UseSqlite($"Data Source=mydb_{DateTime.Now.ToString("yyyyMMddHHmmss")}.db;Version=3;");
            return new DefaultDbContext(builder.Options);
        }
    }
}
