using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SplitFile
{
    class SplitFile
    {
        static async Task Main(string[] args)
        {
            var fromPath = args.FirstOrDefault();

            var toSizeText = args.Skip(1).FirstOrDefault() ?? (50 * 1024 * 1024).ToString();
            var toSize = int.Parse(toSizeText);

            await foreach (var toPath in SplitAsync(fromPath, toSize))
            {
                Console.WriteLine(toPath);
            }
        }

        public static async IAsyncEnumerable<string> SplitAsync(string fromPath, int toSize)
        {
            if (string.IsNullOrEmpty(fromPath))
            {
                throw new ArgumentException(nameof(fromPath));
            }

            if (toSize <= 0)
            {
                throw new ArgumentException(nameof(toSize));
            }

            await foreach (var toPath in SplitAsync(fromPath,
                $"{Path.GetFileNameWithoutExtension(fromPath)}.{{0}}{Path.GetExtension(fromPath)}",
                toSize))
            {
                yield return toPath;
            }
        }
        private static async IAsyncEnumerable<string> SplitAsync(string fromPath, string toPattern, int toSize)
        {
            using var fromStream = File.OpenRead(fromPath);
            var buffer = new byte[10 * 1024 * 1024];
            var index = 0;
            while (true)
            {
                var toPath = string.Format(toPattern, index++);
                using (var toStream = File.OpenWrite(toPath))
                {
                    await CopyAsync(fromStream, toStream, buffer, toSize);
                }

                yield return toPath;

                if (fromStream.Position >= fromStream.Length)
                {
                    yield break;
                }
            }
        }
        
        private static async Task CopyAsync(Stream from, Stream to, byte[] buffer, int limit)
        {
            var count = limit;
            while (count > 0)
            {
                var read = await from.ReadAsync(buffer, 0, Math.Min(buffer.Length, count));

                await to.WriteAsync(buffer, 0, read);

                if (read < count)
                {
                    return;
                }

                count -= read;
            }
        }
    }
}
