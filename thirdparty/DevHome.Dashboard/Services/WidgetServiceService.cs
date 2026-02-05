using System;
using System.Linq;
using System.Threading.Tasks;

namespace DevHome.Dashboard.Services;

public class WidgetServiceService : IWidgetServiceService
{
    public WidgetServiceStates GetWidgetServiceState()
    {
        throw new NotImplementedException();
    }

    public Task<bool> TryInstallingWidgetService()
    {
        throw new NotImplementedException();
    }

    public enum WidgetServiceStates
    {
        MeetsMinVersion,
        NotAtMinVersion,
        NotOK,
        Updating,
        Unknown,
    }

}
