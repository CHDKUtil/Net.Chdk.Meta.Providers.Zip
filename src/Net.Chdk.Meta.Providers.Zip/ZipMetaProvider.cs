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
        private IBootMetaProvider BootProvider { get; }

        protected ZipMetaProvider(IBootMetaProvider bootProvider, ILogger logger)
        {
            BootProvider = bootProvider;
            Logger = logger;
        }

        protected IEnumerable<T> GetItems(string path, string productName)
        {
            var bootFileName = BootProvider.GetFileName(productName);
            using (var stream = File.OpenRead(path))
            {
                var name = Path.GetFileName(path);
                return GetItems(stream, name, bootFileName);
            }
        }

        private IEnumerable<T> GetItems(Stream stream, string name, string bootFileName)
        {
            using (var zip = new ZipFile(stream))
            {
                return GetItems(zip, name, bootFileName).ToArray();
            }
        }

        private IEnumerable<T> GetItems(ZipFile zip, string name, string bootFileName)
        {
            Logger.LogInformation("Enter {0}", name);
            foreach (ZipEntry entry in zip)
            {
                var items = GetItems(zip, entry, bootFileName);
                foreach (var item in items)
                    yield return item;
                yield return GetItem(zip, name, entry, bootFileName);
            }
            Logger.LogInformation("Exit {0}", name);
        }

        private IEnumerable<T> GetItems(ZipFile zip, ZipEntry entry, string bootFileName)
        {
            if (!entry.IsFile)
                return Enumerable.Empty<T>();

            var ext = Path.GetExtension(entry.Name);
            if (!".zip".Equals(ext, StringComparison.OrdinalIgnoreCase))
                return Enumerable.Empty<T>();

            var name = Path.GetFileName(entry.Name);
            using (var stream = zip.GetInputStream(entry))
            {
                return GetItems(stream, name, bootFileName);
            }
        }

        private T GetItem(ZipFile zip, string name, ZipEntry entry, string bootFileName)
        {
            if (!entry.IsFile)
                return null;

            if (!bootFileName.Equals(entry.Name, StringComparison.OrdinalIgnoreCase))
                return null;

            return DoGetItem(zip, name, entry);
        }

        protected abstract T DoGetItem(ZipFile zip, string name, ZipEntry entry);
    }
}
