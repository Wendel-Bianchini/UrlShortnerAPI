using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MottuApi.Models;
using Newtonsoft.Json;
using Npgsql;
using System;
using System.Configuration;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Security.Principal;
using System.Xml.Linq;
using UrlShortnerApi.Models;

namespace UrlShortnerApi.Migrations
{
    public class InitialState
    {
        public IConfiguration Configuration { get; }

        // Json for initial state for table URL
        static string json = @"[
       {
          ""id"":""23094"",
          ""hits"":5,
          ""url"":""http://globo.com"",
          ""ShortUrl"":""http://chr.dc/9dtr4""
       },
       {
          ""id"":""76291"",
          ""hits"":4,
          ""url"":""http://google.com"",
          ""ShortUrl"":""http://chr.dc/aUx71""
       },
       {
          ""id"":""66761"",
          ""hits"":7,
          ""url"":""http://terra.com.br"",
          ""ShortUrl"":""http://chr.dc/u9jh3""
       },
       {
          ""id"":""70001"",
          ""hits"":1,
          ""url"":""http://facebook.com"",
          ""ShortUrl"":""http://chr.dc/qy61p""
       },
       {
          ""id"":""21220"",
          ""hits"":2,
          ""url"":""http://diariocatarinense.com.br"",
          ""ShortUrl"":""http://chr.dc/87itr""
       },
       {
          ""id"":""10743"",
          ""hits"":0,
          ""url"":""http://uol.com.br"",
          ""ShortUrl"":""http://chr.dc/y81xc""
       },
       {
          ""id"":""19122"",
          ""hits"":2,
          ""url"":""http://chaordic.com.br"",
          ""ShortUrl"":""http://chr.dc/qy5k9""
       },
       {
          ""id"":""55324"",
          ""hits"":4,
          ""url"":""http://youtube.com"",
          ""ShortUrl"":""http://chr.dc/1w5tg""
       },
       {
          ""id"":""70931"",
          ""hits"":5,
          ""url"":""http://twitter.com"",
          ""ShortUrl"":""http://chr.dc/7tmv1""
       },
       {
          ""id"":""87112"",
          ""hits"":2,
          ""url"":""http://bing.com"",
          ""ShortUrl"":""http://chr.dc/9opw2""
       }
    ]"
        ;

        // Verify if database and tables already exists in DB
        // If don't exists they are created
        public static void ValidateInitialState()
        {
            // database connection instance
            var builder = WebApplication.CreateBuilder();
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql(builder.Configuration.GetConnectionString("DBConnection"));
            AppDbContext dbContext = new AppDbContext(optionsBuilder.Options);

            // Create database if not exist
            dbContext.Database.EnsureCreated();

            // Deserialize JSON list and loop on every object
            List<ShortUrlData> Urls = JsonConvert.DeserializeObject<List<ShortUrlData>>(json);

            foreach (ShortUrlData val in Urls)
            {
                var bdUrl = dbContext.Urls.Where(x => x.Url == val.Url).FirstOrDefault();
                if (bdUrl == null)
                {
                    var NewUrl = new ShortUrlData(0, "System", DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc), val.Url, val.ShortUrl);
                    dbContext.Urls.Add(NewUrl);
                    dbContext.SaveChanges();

                }
            }

            // Always insert the user Admin with password admin132 for authentication
            // For demonstration purposes only of hash authentication.
            var bdUser = dbContext.Users.Where(x => x.Username == "Admin").FirstOrDefault();
            if (bdUser == null)
            {
                var NewUset = new User("Admin", "WLZL4Qlq8R+9Q/rOchV1e6IalHBeQqFqvSwVVwM0C+R4W+Gz");
                dbContext.Users.Add(NewUset);
                dbContext.SaveChanges();

            }

            
        }
    }
}
