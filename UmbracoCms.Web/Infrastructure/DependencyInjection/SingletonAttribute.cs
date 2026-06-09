using Scrutor;

namespace UmbracoCms.Web.Infrastructure.DependencyInjection;

public class SingletonAttribute : ServiceDescriptorAttribute
{
    public SingletonAttribute() : base(null, ServiceLifetime.Singleton) { }
    public SingletonAttribute(Type serviceType) : base(serviceType, ServiceLifetime.Singleton) { }
}
