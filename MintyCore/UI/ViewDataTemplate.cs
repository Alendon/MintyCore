using Avalonia.Controls.Templates;

namespace MintyCore.UI;

public class ViewDataTemplate : IDataTemplate
{
    public View? Build(object? param)
    {
        if (param is View view)
        {
            return view;
        }

        return null;
    }

    public bool Match(object? data)
    {
        return data is View;
    }
}