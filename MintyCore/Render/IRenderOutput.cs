namespace MintyCore.Render;

public interface IRenderOutputWrapper<TOutput> : IRenderOutputWrapper
{
    public TOutput GetConcreteOutput();
}

public interface IRenderOutputWrapper
{
    public object GetOutput();
}