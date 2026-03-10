using System.Threading.Tasks;

namespace HCSN.Identity.Application.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
    Task SendEmailWithTemplateAsync(
        string to,
        string templateId,
        Dictionary<string, object> templateData
    );
}
