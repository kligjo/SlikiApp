using Scrutor;

namespace UmbracoCms.Web.Infrastructure.DependencyInjection;

public class ScopedAttribute : ServiceDescriptorAttribute
{
    public ScopedAttribute() : base(null, ServiceLifetime.Scoped) { }
    public ScopedAttribute(Type serviceType) : base(serviceType, ServiceLifetime.Scoped) { }
}
