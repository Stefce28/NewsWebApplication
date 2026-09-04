using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
namespace NewsWebApp.Configuration;

public class ControllerAuthorizationConvention : IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        var namespaceName = controller.ControllerType.Namespace;

        if (namespaceName == null)
            return;

        if (namespaceName.Contains(".Controllers.Private"))
        {
            controller.Filters.Add(new AuthorizeFilter());
        }
        else if (namespaceName.Contains(".Controllers.Public"))
        {
            controller.Filters.Add(new AllowAnonymousFilter());
        }
    }
}
