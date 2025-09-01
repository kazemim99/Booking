namespace Booksy.Core.Domain.Application.Services
{
    public interface IApplicationSettings
    {
       public string BaseUrl { get; }
       public string SupportEmail { get; }
    }
}