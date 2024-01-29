using Npgsql;
using MottuApi.Models;
using Microsoft.Extensions.Configuration;
using System.Data;
using Microsoft.EntityFrameworkCore;
using MottuApi.Data;
using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text.RegularExpressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using Microsoft.CodeAnalysis.Differencing;
using System.Security.Cryptography;
using System.Net;

namespace MottuApi.Controllers
{
    public class UrlShortnerRoutes
    {
        private static Random random = new Random();

        public static void GenerateShortUrl(WebApplication app)
        {
            // Defining the queues for later integration with messaging systems.
            Queue<ShortUrlData> queueInsert = new Queue<ShortUrlData>();
            Queue<ShortUrlData> queueDelete = new Queue<ShortUrlData>();

            // Default prefix for all routes
            var routesURL = app.MapGroup(prefix: "shortUrl");

            /*Definition of the method for creating shortened URLs
             example of body to be sent:
            {
            "createdBy": "WendelBianchini",
            "isProtected": false, 
            "password": "",
            "Url": "http://google.com",
            "shortUrl": ""
            }
            */
            routesURL.MapPost(pattern: "", handler: async (addUrlRequest Request, AppDbContext Context, [FromHeader] string? Authorization) =>
            {
                // Checks if authorization is filled and call method that validates the parameters.
                if (Authorization != null && Authorization.StartsWith("Basic"))
                {
                    string authUser = "";
                    bool isValid = VerifyCredential(Context, Authorization, ref authUser);

                    if (isValid)
                    {
                        // Call method that validate if all required fields are filles
                        // this metode return an ErrorCode and a Response in validateFields
                        var validateFields = VerifyFields(Request, Context);

                        if (validateFields.ErrorCode > 200)
                        {
                            return Results.BadRequest(validateFields.Response);
                        }

                        // Call method that generate the shortened version of URL
                        // " Can be provide a custom name for the shortened UR on the field 'shortUrl'"
                        // this metode return an ErrorCode and a Response in validateFields
                        var GenerateShortUrl = await GenerateShortURL(Request, Context);

                        if (GenerateShortUrl.ErrorCode > 200)
                        {
                            return Results.Conflict(GenerateShortUrl.Response);
                        }

                        // Assembles the object and inserts it into the database.
                        var NewUrl = new ShortUrlData(0, authUser, DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc), Request.Url, "http://chr.dc/" + GenerateShortUrl.Response);
                        await Context.Urls.AddAsync(NewUrl);
                        await Context.SaveChangesAsync();

                        // Insert in  the queue for later integration with messaging systems.
                        queueInsert.Enqueue(NewUrl);

                        return Results.Ok(NewUrl);
                    }
                    else
                    {
                        throw new UnauthorizedAccessException("Favor verificar credenciais!");
                    }
                }
                else
                {
                    throw new UnauthorizedAccessException("Favor verificar credenciais!");
                }
            });
            /*Get Urls, can be used query parameters to filter selection
            Query example:
            /shortUrl?createdBy=WendelBianchini&
            Id=2&StartDate="2024-01-28T01:40:07.498689Z"&
            EndDate="2024-01-28T01:44:07.498689Z"&
            CompleteUrl=www.google.com
            */
            routesURL.MapGet(pattern: "", handler: async (AppDbContext Context, [AsParameters] SearchCriteria criteria, [FromHeader] string? Authorization) =>
            {
                // Checks if authorization is filled and calls method that validates the parameters.
                if (Authorization != null && Authorization.StartsWith("Basic"))
                {
                    string authUser = "";
                    bool isValid = VerifyCredential(Context, Authorization, ref authUser);

                    if (isValid)
                    {
                        var listUrlAllFields = await Context.Urls.ToListAsync();

                        /* Verify if have any query field in URL to filter select
                         Can be filtered by
                            *Id
                            *createdBy
                            *StartDate
                            *EndDate
                            *CompleteUrl
                        */
                        string Fields = "";
                        String QueryFields = VerifyQueryURI(criteria);

                        if (QueryFields != "")
                        {
                            QueryFields = "listUrlAllFields => " + QueryFields;
                            listUrlAllFields = await Context.Urls.Where(QueryFields).ToListAsync();
                        }

                        var listUrlResponse = (from l in listUrlAllFields
                                               select new
                                               {
                                                   Id = l.Id,
                                                   ShortUrl =  l.ShortUrl,
                                                   Hits = l.Hits,
                                                   CreatedBy = l.CreatedBy,
                                                   CreatedDate = l.CreatedDate,
                                                   Url = l.Url
                                               }).ToList();

                        return listUrlResponse;
                    }
                    else
                    {
                        throw new UnauthorizedAccessException("Favor verificar credenciais!");
                    }
                }
                else
                {
                    throw new UnauthorizedAccessException("Favor verificar credenciais!");
                }

            });
            //  "Checks if the provided shortened URL is valid and displays the full URL
            //  if the URL is invalid, the return is null."
            //"You can provide only the 'Shortened URL ID' (tNuZA) or the complete URL (http://chr.dc/tNuZA)."
            routesURL.MapGet(pattern: "/validar", handler: async (AppDbContext Context, [AsParameters] VerifyShortUrl criteria, [FromHeader] string? Authorization) =>
            {
                // Checks if authorization is filled and calls method that validates the parameters.
                if (Authorization != null && Authorization.StartsWith("Basic"))
                {
                    string authUser = "";
                    bool isValid = VerifyCredential(Context, Authorization, ref authUser);

                    if (isValid)
                    {
                        // Validate and get only "Shortned url Id"
                        if (criteria.shortUrl != null)
                        {
                            if (criteria.shortUrl.Contains("dc/"))
                            {
                                criteria.shortUrl = criteria.shortUrl.Substring(criteria.shortUrl.IndexOf("dc/") + 3);

                            }
                            Regex rgx = new Regex("[^a-zA-Z0-9 -]");
                            criteria.shortUrl = rgx.Replace(criteria.shortUrl, "");

                            criteria.shortUrl = "http://chr.dc/" + criteria.shortUrl;
                            // Validate if exists in DB
                            var Exists = await Context.Urls
                            .AnyAsync(Url => Url.ShortUrl == criteria.shortUrl);

                            if (Exists)
                            {
                                var listUrlAllFields = await Context.Urls.Where(listUrlAllFields => listUrlAllFields.ShortUrl == criteria.shortUrl).ToListAsync();
                                
                                // increment Hits count every in select
                                foreach (var tom in listUrlAllFields.Where(w => w.ShortUrl == criteria.shortUrl))
                                {
                                    tom.Hits = tom.Hits + 1;
                                }
                                await Context.SaveChangesAsync();
                                return listUrlAllFields;
                            }
                        }
                        var listUrlAllField = await Context.Urls.Where(listUrlAllFields => listUrlAllFields.ShortUrl == criteria.shortUrl).ToListAsync();
                        return listUrlAllField;
                    }
                    else
                    {
                        throw new UnauthorizedAccessException("Favor verificar credenciais!");
                    }
                }
                else
                {
                    throw new UnauthorizedAccessException("Favor verificar credenciais!");
                }
            });
            //Delete URL by ID
            //If ID is invalid return error
            routesURL.MapDelete(pattern: "/{IdUrl:int}", handler: async (int IdUrl, AppDbContext Context, [FromHeader] string? Authorization) =>
            {
                // Checks if authorization is filled and calls method that validates the parameters.
                if (Authorization != null && Authorization.StartsWith("Basic"))
                {
                    string authUser = "";
                    bool isValid = VerifyCredential(Context, Authorization, ref authUser);

                    if (isValid)
                    {
                        //Verify if Id exists
                        var Exists = await Context.Urls
                        .AnyAsync(Url => Url.Id == IdUrl);

                        if (Exists)
                        {
                            var listUrlAllFields = await Context.Urls.ToListAsync();

                            // Get and delete data from DB
                            var toRemove = listUrlAllFields.Where(listUrlAllFields => listUrlAllFields.Id == IdUrl);
                            foreach (var item in toRemove)
                            {
                                Context.Urls.Remove(item);
                                Context.SaveChanges();
                                queueDelete.Enqueue(item);
                            }
                            return Results.Ok("Url Excluida com sucesso!");

                        }
                        return Results.NotFound("url com id " + IdUrl + " não encontrada, favor verificar!");
                    }
                    else
                    {
                        throw new UnauthorizedAccessException("Favor verificar credenciais!");
                    }
                }
                else
                {
                    throw new UnauthorizedAccessException("Favor verificar credenciais!");
                }
            });
        }

        //class for URL query search
        public class SearchCriteria
        {
            public string? Id { get; set; }
            public string? createdBy { get; set; }
            public string? StartDate { get; set; }
            public string? EndDate { get; set; }
            public string? CompleteUrl { get; set; }

        }

        // Class for URLShortVerify 
        public class VerifyShortUrl
        {
            public string? shortUrl { get; set; }

        }

        // method that validate if URL is well formed
        static bool ValidateUrlFormed(string url)
        {
            return Uri.IsWellFormedUriString(url, UriKind.Absolute);
        }

        // Method that validate and generate where statement
        public static string VerifyQueryURI(SearchCriteria criteria)
        {
            string whereClause = "";
            int count = 0;

            if (criteria.Id != null)
            {
                whereClause = String.Format("(listUrlAllFields.Id == " + criteria.Id + ")");
                count++;

            }
            if (criteria.createdBy != null)
            {
                if (whereClause != "")
                {
                    whereClause = whereClause + " && ";
                }
                whereClause = whereClause + String.Format("(listUrlAllFields.CreatedBy == \"" + criteria.createdBy.ToString() + "\")");
            }
            if (criteria.StartDate != null)
            {
                if (whereClause != "")
                {
                    whereClause = whereClause + " && ";
                }
                whereClause = whereClause + String.Format("(listUrlAllFields.CreatedDate > " + criteria.StartDate + ")");
                count++;
            }
            if (criteria.EndDate != null)
            {
                if (whereClause != "")
                {
                    whereClause = whereClause + " && ";
                }
                whereClause = whereClause + String.Format("(listUrlAllFields.CreatedDate < " + criteria.EndDate + ")");
                count++;
            }
            if (criteria.CompleteUrl != null)
            {
                if (whereClause != "")
                {
                    whereClause = whereClause + " && ";
                }
                whereClause = whereClause + String.Format("(listUrlAllFields.CompleteUrl == \"" + criteria.CompleteUrl.ToString() + "\")");
                count++;
            }
            return whereClause;
        }

        // Method that generates random string for shortned url
        public static string RandomUrlIdGenerator(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        // Method for field verification
        public static (string Response, int ErrorCode) VerifyFields(addUrlRequest Request, AppDbContext Context)
        {
            if (Request.createdBy == "")
            {
                return ("Usuario não informado, favor verificar!", 400);
            }

            if (Request.Url == "")
            {

                return ("URL não informada, favor verificar!", 400);
            }

            if ((Request.isProtected == true) && (Request.password == ""))
            {
                return ("Não foi informada a senha para criação da URL protegida, favor verificar!", 400);
            }

            bool urlIsOk = ValidateUrlFormed(Request.Url);

            if (!urlIsOk)
            {
                return ("Url mal formatada, favor verificar!", 400);
            }
            return ("", 200);

        }
        // Task that generate short url,
        // If a custom URL is provided, it will check if it already exists, if true return error
        // If not provided a custom URl it will generate a short url until they aren't exists in DB
        public static async Task<(string Response, int ErrorCode)> GenerateShortURL(addUrlRequest Request, AppDbContext Context)
        {
            var Exists = true;

            string tShortUrl = Request.shortUrl;
            if (tShortUrl == "")
            {
                tShortUrl = RandomUrlIdGenerator(5);

                // Generate a new short Url if exists until generate a that don't exist
                while (Exists)
                {
                    Exists = await Context.Urls
                .AnyAsync(Url => Url.ShortUrl == tShortUrl);
                }
            }
            else
            {
                Exists = await Context.Urls
            .AnyAsync(Url => Url.ShortUrl == tShortUrl);

                if (Exists)
                {
                    return ("Id já cadastrado, favor utilizar um identificador diferente", 409);
                }
            }

            Exists = await Context.Urls
            .AnyAsync(Url => (Url.Url == Request.Url) && (Url.CreatedBy == Request.createdBy));

            if (Exists)
            {
                return ("URL já cadastrada para o usuario informado, favor verificar", 409);
            }
            return (tShortUrl, 200);
        }

        // Method that validate authorization
        // password is encoded on PBKDF2
        // it will get a password from DB already encoded
        // and verify byte by byte
        public static bool VerifyCredential(AppDbContext Context, String headerAuthorization, ref string authUsername)
        {
            // divide username and password from header authorization
            string authorization = headerAuthorization.Substring("Basic ".Length).Trim();

            Encoding encoding = Encoding.GetEncoding("iso-8859-1");
            string usernamePassword = encoding.GetString(Convert.FromBase64String(authorization));
            int index = usernamePassword.IndexOf(':');

            string username = usernamePassword.Substring(0, index);
            string password = usernamePassword.Substring(index + 1);
            
            // Verify if match
            var bdUser = Context.Users.Where(x => (x.Username == username)).FirstOrDefault();
            if (bdUser != null)
            {
                byte[] hashBytes = Convert.FromBase64String(bdUser.Password);
                byte[] salt = new byte[16];
                Array.Copy(hashBytes, 0, salt, 0, 16);
                var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000);
                byte[] hash = pbkdf2.GetBytes(20);
                for (int i = 0; i < 20; i++)
                {
                    if (hashBytes[i + 16] != hash[i])
                    {
                        return false;
                    }
                }
                authUsername = username;
                return true;
            }
            else
            {
                return false;
            }
        }
    }

}
