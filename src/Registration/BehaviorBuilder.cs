namespace AnAspect.Mediator.Registration;


//todo implement this buider in MediatorConfiguration.AddBehavior
public sealed class BehaviorBuilder
{
    private readonly Type _type;
    private readonly Type? _requestType;
    private readonly Type? _responseType;
    private int _order;
    private string[]? _groups;

    internal BehaviorBuilder(Type type, Type? requestType, Type? responseType)
    {
        _type = type;
        _requestType = requestType;
        _responseType = responseType;
    }

    public BehaviorBuilder WithOrder(int order) { _order = order; return this; }
    public BehaviorBuilder InGroup(string group)
    {
        if (_groups == null)
            _groups = [group];
        else
        {
            _groups = [.._groups, group];
        }

        return this;
    }

    internal BehaviorConfig Build() => new(_type, _order, _groups, _requestType, _responseType, IsOpenGeneric: false);
}
