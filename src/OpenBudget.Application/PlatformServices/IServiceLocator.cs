namespace OpenBudget.Application.PlatformServices
{
    public interface IServiceLocator
    {
        TInterface GetInstance<TInterface>();
    }
}
