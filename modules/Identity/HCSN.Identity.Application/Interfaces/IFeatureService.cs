using HCSN.Identity.Domain.Entities;

namespace HCSN.Identity.Application.Interfaces
{
    public interface IFeatureService
    {
        bool IsFeatureAvailableForNewUsers(string feature, Tenant tenant);
        List<string> GetEnabledFeatures(Tenant tenant);
        bool IsBuiltInFeature(string feature);
    }
}
