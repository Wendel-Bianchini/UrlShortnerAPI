using Microsoft.EntityFrameworkCore;
using System;
using Npgsql;
using Npgsql.Replication;
using System.Data;
using UrlShortnerApi.Models;

namespace MottuApi.Models
{
    public class AppDbContext : DbContext
    {
        
        public AppDbContext(DbContextOptions<AppDbContext> options) 
            : base(options) 
        {

        }
        public DbSet<ShortUrlData> Urls{ get; set; }
        public DbSet<User> Users { get; set; }
    }
}
