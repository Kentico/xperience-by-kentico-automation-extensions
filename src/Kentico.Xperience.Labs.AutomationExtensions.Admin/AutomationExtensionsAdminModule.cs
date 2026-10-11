using Kentico.Xperience.Admin.Base;

[assembly: CMS.RegisterModule(typeof(Kentico.Xperience.Labs.AutomationExtensions.Admin.AutomationExtensionsAdminModule))]

namespace Kentico.Xperience.Labs.AutomationExtensions.Admin;

internal sealed class AutomationExtensionsAdminModule : AdminModule
{
    public AutomationExtensionsAdminModule()
        : base("Kentico.Xperience.Labs.AutomationExtensions.Admin")
    {
    }


    protected override void OnInit()
    {
        base.OnInit();

        RegisterClientModule("kentico", "xperience-labs-automation-extensions-admin");
    }
}
