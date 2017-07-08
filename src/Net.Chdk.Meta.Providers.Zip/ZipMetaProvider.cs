using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Net.Chdk.Meta.Providers.Zip
{
    public abstract class ZipMetaProvider<T>
        where T : class
    {
        protected ILogger Logger { get; }

        private string FileName { get; }

        protected ZipMetaProvider(IBootMetaProvider bootProvider, ILogger logger)
        {
            Logger = logger;
            FileName = bootProvider.FileName;
        }

        protected IEnumerable<T> GetItems(string path)
        {
            using (var stream = File.OpenRead(path))
            {
                var name = Path.GetFileName(path);
                return GetItems(stream, name);
            }
        }

        private IEnumerable<T> GetItems(Stream stream, string name)
        {
            using (var zip = new ZipFile(stream))
            {
                return GetItems(zip, name).ToArray();
            }
        }

        private IEnumerable<T> GetItems(ZipFile zip, string name)
        {
            Logger.LogInformation("Enter {0}", name);
            foreach (ZipEntry entry in zip)
            {
                var items = GetItems(zip, entry);
                foreach (var item in items)
                    yield return item;
                yield return GetItem(zip, name, entry);
            }
            Logger.LogInformation("Exit {0}", name);
        }

        private IEnumerable<T> GetItems(ZipFile zip, ZipEntry entry)
        {
            if (!entry.IsFile)
                return Enumerable.Empty<T>();

            var ext = Path.GetExtension(entry.Name);
            if (!".zip".Equals(ext, StringComparison.OrdinalIgnoreCase))
                return Enumerable.Empty<T>();

            var name = Path.GetFileName(entry.Name);
            using (var stream = zip.GetInputStream(entry))
            {
                return GetItems(stream, name);
            }
        }

        private T GetItem(ZipFile zip, string name, ZipEntry entry)
        {
            if (!entry.IsFile)
                return null;

            if (!FileName.Equals(entry.Name, StringComparison.OrdinalIgnoreCase))
                return null;

            return DoGetItem(zip, name, entry);
        }

        protected abstract T DoGetItem(ZipFile zip, string name, ZipEntry entry);
    }
}
