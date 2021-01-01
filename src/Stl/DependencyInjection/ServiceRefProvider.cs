namespace Stl.DependencyInjection
{
    public interface IServiceRefProvider
    {
        ServiceRef GetServiceRef(object service);
    }

    public class ServiceRefProvider : IServiceRefProvider
    {
        public virtual ServiceRef GetServiceRef(object service)
        {
            if (service is IHasServiceRef hsr)
                return hsr.ServiceRef;
            return new ServiceTypeRef(service.GetType());
        }
    }
}
