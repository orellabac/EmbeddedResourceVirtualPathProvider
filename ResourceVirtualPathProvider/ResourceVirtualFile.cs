using System.IO;
using System.Web;
using System.Web.Hosting;

namespace ResourceVirtualPathProvider
{
    class ResourceVirtualFile : VirtualFile
    {
        readonly Resource _resource;
        readonly ResourceCacheControl _cacheControl;

        public ResourceVirtualFile(string virtualPath, Resource resource, ResourceCacheControl cacheControl)
            : base(virtualPath)
        {
            _resource = resource;
            _cacheControl = cacheControl;
        }

        public override Stream Open()
        {
            if (_cacheControl != null)
            {
                HttpContext.Current.Response.Cache.SetCacheability(_cacheControl.Cacheability);
                HttpContext.Current.Response.Cache.AppendCacheExtension("max-age=" + _cacheControl.MaxAge);
            }
            return _resource.GetStream();
        }
    }
}