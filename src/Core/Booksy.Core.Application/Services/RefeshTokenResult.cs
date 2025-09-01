namespace Booksy.Core.Application.Services
{
    public class RefeshTokenResult
    {
        public IReadOnlyList<string>? Roles { get; set; }
        public string? NewRefreshToken { get; set; }
    }
}
