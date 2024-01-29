namespace MottuApi.Data
{
    public record addUrlRequest(string createdBy , bool isProtected, String password, string Url, string shortUrl);
}
