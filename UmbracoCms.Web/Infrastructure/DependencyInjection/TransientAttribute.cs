using Scrutor;

namespace UmbracoCms.Web.Infrastructure.DependencyInjection;

public class TransientAttribute : ServiceDescriptorAttribute
{
    public TransientAttribute() : base(null, ServiceLifetime.Transient) { }
    public TransientAttribute(Type serviceType) : base(serviceType, ServiceLifetime.Transient) { }
}
