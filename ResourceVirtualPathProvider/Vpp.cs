using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Web;
using System.Web.Caching;
using System.Web.Hosting;
using System.Linq;
using System.Resources;

namespace ResourceVirtualPathProvider
{
    public class Vpp : VirtualPathProvider, IEnumerable
    {
        readonly IDictionary<string, List<Resource>> _resources = new Dictionary<string, List<Resource>>();

        public Vpp(params Assembly[] assemblies)
        {
            Array.ForEach(assemblies, a => Add(a));
            UseResource = er => true;
            UseLocalIfAvailable = resource => true;
            CacheControl = er => null;
        }

        public Func<Resource, bool> UseResource { get; set; }
        public Func<Resource, bool> UseLocalIfAvailable { get; set; }
        public Func<Resource, ResourceCacheControl> CacheControl { get; set; } 

        public void Add(Assembly assembly, string projectSourcePath = null)
        {
            var assemblyName = assembly.GetName().Name;
					  var resourceSource = assembly.GetManifestResourceNames().FirstOrDefault(x => x == assemblyName + ".g.resources");
						if (resourceSource != null)
						{
							var resourceStream = assembly.GetManifestResourceStream(resourceSource);
							if (resourceStream != null)
							{
								var stream = new ResourceReader(resourceStream);
								var resourceNames = (from DictionaryEntry resource in stream select resource.Key.ToString()).ToArray();

								foreach (var res in resourceNames)
								{
									var key = res;

									if (!_resources.ContainsKey(key))
										_resources[key] = new List<Resource>();
									_resources[key].Insert(0, new Resource(assembly, res, projectSourcePath));
								}
							}
						}
        }
 
        public override bool FileExists(string virtualPath)
        {
            return (base.FileExists(virtualPath) || GetResourceFromVirtualPath(virtualPath) != null);
        }

        public override VirtualFile GetFile(string virtualPath)
        {
            //if (base.FileExists(virtualPath)) return base.GetFile(virtualPath);
            var resource = GetResourceFromVirtualPath(virtualPath);
            if (resource != null)
                return new ResourceVirtualFile(virtualPath, resource, CacheControl(resource));
            return base.GetFile(virtualPath);
        }

        public override string CombineVirtualPaths(string basePath, string relativePath)
        {
            var combineVirtualPaths = base.CombineVirtualPaths(basePath, relativePath);
            return combineVirtualPaths;
        }
        public override string GetFileHash(string virtualPath, IEnumerable virtualPathDependencies)
        {
            var fileHash = base.GetFileHash(virtualPath, virtualPathDependencies);
            return fileHash;
        }

        public override string GetCacheKey(string virtualPath)
        {
            var resource = GetResourceFromVirtualPath(virtualPath);
            if (resource != null)
            {
                return (virtualPath + resource.AssemblyName + resource.AssemblyLastModified.Ticks).GetHashCode().ToString();
            }
            return base.GetCacheKey(virtualPath);
        }
        
        public Resource GetResourceFromVirtualPath(string virtualPath)
        {
            var path = VirtualPathUtility.ToAppRelative(virtualPath).TrimStart('~', '/');
            var index = path.LastIndexOf("/", StringComparison.Ordinal);
            if (index != -1)
            {
                var folder = path.Substring(0, index).Replace("-", "_"); //resources with "-"in their folder names are stored as "_".
                path = folder + path.Substring(index);
            }
            var cleanedPath = path;
            var key = (cleanedPath).ToLowerInvariant();
            if (_resources.ContainsKey(key))
            {
                var resource = _resources[key].FirstOrDefault(UseResource);
                if (resource != null && !ShouldUsePrevious(virtualPath, resource))
                {
                    return resource;
                }
            }
            return null;
        }

        public override CacheDependency GetCacheDependency(string virtualPath, IEnumerable virtualPathDependencies, DateTime utcStart)
        {
            var resource = GetResourceFromVirtualPath(virtualPath);
            if (resource != null)
            {
                return resource.GetCacheDependency(utcStart);
            }

            if (DirectoryExists(virtualPath) || FileExists(virtualPath))
            {
                return base.GetCacheDependency(virtualPath, virtualPathDependencies, utcStart);
            }

            return null;
        }

        private bool ShouldUsePrevious(string virtualPath, Resource resource)
        {
            return base.FileExists(virtualPath) && UseLocalIfAvailable(resource);
        }

        
        //public override string GetCacheKey(string virtualPath)
        //{
        //    var resource = GetResourceFromVirtualPath(virtualPath);
        //    if (resource != null) return virtualPath + "blah";
        //    return base.GetCacheKey(virtualPath);
        //}

        public IEnumerator GetEnumerator()
        {
            throw new NotImplementedException("Only got this so that we can use object collection initializer syntax");
        }
    }
}