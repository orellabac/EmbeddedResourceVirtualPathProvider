using System.Web.Hosting;
using ResourceVirtualPathProvider;
using TestResourceLibrary;

namespace TestWebProject.App_Code
{
    public class RegisterVirtualPathProvider
    {
        public static void AppInitialize()
        {
            HostingEnvironment.RegisterVirtualPathProvider(new ResourceVirtualPathProvider.Vpp()
            {
                {typeof (Marker).Assembly, @"..\TestResourceLibrary"},

            });
        }
    }
}