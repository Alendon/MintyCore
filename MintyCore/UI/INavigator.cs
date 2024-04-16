using System.Threading.Tasks;
using MintyCore.Utils;

namespace MintyCore.UI;

public interface INavigator
{
    Task NavigateTo(Identification viewId, bool isChild = true);
    
    void Quit();
}