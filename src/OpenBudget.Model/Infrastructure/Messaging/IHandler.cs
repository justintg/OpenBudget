namespace OpenBudget.Model.Infrastructure.Messaging
{
    public interface IHandler<TMessage>
    {
        void Handle(TMessage message);
    }
}
