using SofiaEnVozAlta.Api.Models;

namespace SofiaEnVozAlta.Api.Services;

public interface IEmailService
{
    Task SendContactRequestAsync(ContactRequest request, CancellationToken cancellationToken = default);
}
