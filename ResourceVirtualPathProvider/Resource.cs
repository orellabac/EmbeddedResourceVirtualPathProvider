using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Web;
using System.Web.Caching;

namespace ResourceVirtualPathProvider
{
    public class Resource
    {
        public Resource(Assembly assembly, string resourcePath, string projectSourcePath)
        {
            AssemblyName = assembly.GetName().Name;
            FileInfo fileInfo = new FileInfo(assembly.Location);
            AssemblyLastModified = fileInfo.LastWriteTime;
            ResourcePath = resourcePath;
            if (!string.IsNullOrWhiteSpace(projectSourcePath))
            {
                var filename = GetFileNameFromProjectSourceDirectory(assembly, resourcePath, projectSourcePath);

                if (filename != null) //means that the source file was found, or a copy was in the web apps folders
                {
                    GetCacheDependency = utcStart => new CacheDependency(filename, utcStart);
                    GetStream = () => File.OpenRead(filename);
                    return;
                }
            }
            GetCacheDependency = utcStart => new CacheDependency(assembly.Location);


						GetStream = () =>
						{
							var assemblyName = assembly.GetName().Name;
							var resourceSource = assemblyName + ".g.resources";
							var resourceStream = assembly.GetManifestResourceStream(resourceSource);
							if (resourceStream != null)
							{
								var stream = new ResourceReader(resourceStream);
								foreach(DictionaryEntry resource in stream)
								{
									if (resource.Key.ToString() == resourcePath)
									{
										return (Stream)resource.Value;
									}
								}
							}
							return null;
						};
        }

        public DateTime AssemblyLastModified { get; private set; }

        public string ResourcePath { get; private set; }

        public Func<Stream> GetStream { get; private set; }
        public Func<DateTime, CacheDependency> GetCacheDependency { get; private set; }

        public string AssemblyName { get; private set; }

        string GetFileNameFromProjectSourceDirectory(Assembly assembly, string resourcePath, string projectSourcePath)
        {
            try
            {
                if (!Path.IsPathRooted(projectSourcePath))
                {
                    projectSourcePath =
                        new DirectoryInfo((Path.Combine(HttpRuntime.AppDomainAppPath, projectSourcePath))).FullName;
                }
                var fileName = Path.Combine(projectSourcePath,resourcePath);


                return GetFileName(fileName);
            }
            catch (Exception ex)
            {
#if DEBUG
                throw;
#else
                Logger.LogWarning("Error reading source files", ex);
                return null;
#endif
            }
        }

        string GetFileName(string possibleFileName)
        {
            var indexOfLastSlash = possibleFileName.LastIndexOf('\\');
            while (indexOfLastSlash > -1)
            {
                if (File.Exists(possibleFileName)) return possibleFileName;
                possibleFileName = ReplaceChar(possibleFileName, indexOfLastSlash, '.');
                indexOfLastSlash = possibleFileName.LastIndexOf('\\');
            }
            return null;
        }


        string ReplaceChar(string text, int index, char charToUse)
        {
            char[] buffer = text.ToCharArray();
            buffer[index] = charToUse;
            return new string(buffer);
        }
    }
}