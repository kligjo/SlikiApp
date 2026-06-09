namespace UmbracoCms.Web.Helpers.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RemoveAllImplementedBy<TService, TImpl>(this IServiceCollection collection)
    {
        collection.RemoveAll(descriptor => descriptor.GetImplementationType() == typeof(TImpl) && descriptor.ServiceType == typeof(TService));
        return collection;
    }

    private static Type? GetImplementationType(this ServiceDescriptor descriptor)
    {
        return descriptor.ImplementationType
               ?? descriptor.ImplementationInstance?.GetType()
               ?? descriptor.ImplementationFactory?.Method.ReturnType;
    }
}
