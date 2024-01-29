using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MottuApi.Models
{
    public class ShortUrlData
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int Hits { get; set; }
        public string Url { get; private set; }
        public string ShortUrl { get; private set; }
        public string CreatedBy { get; private set; }
        public DateTimeOffset CreatedDate { get; private set; }

        public ShortUrlData(int hits, String createdBy, DateTimeOffset createdDate, string url, string shortUrl)
        {
            Hits = hits;
            CreatedBy = createdBy;
            Url = url;
            ShortUrl = shortUrl;
            CreatedDate = createdDate;
        }

    }
}
