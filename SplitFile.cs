using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SplitFile
{
    class SplitFile
    {
        static async Task Main(string[] args)
        {
            var fromPath = args.FirstOrDefault();

            var toSizeText = args.Skip(1).FirstOrDefault() ?? "500 MB";
            var toSize = GetSize(toSizeText);

            await foreach (var toPath in SplitAsync(fromPath, toSize))
            {
                Console.WriteLine(toPath);
            }
        }

        public static int GetSize(string text)
        {
            var match = Regex.Match(text.ToLower(), @"(?<number>\d+)\s*(?<unit>k|m|g)?B?");
            if (!match.Success)
            {
                throw new ArgumentException(nameof(text));
            }

            var numberText = match.Groups["number"].Value;
            var numberValue = int.Parse(numberText);

            var unitText = match.Groups["unit"].Value;
            return unitText switch
            {
                "k" => numberValue * 1024,
                "m" => numberValue * 1024 * 1024,
                "g" => numberValue * 1024 * 1024 * 1024,
                _ => numberValue,
            };
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

            using var fromStream = File.OpenRead(fromPath);

            var buffer = new byte[10 * 1024 * 1024];

            var index = 0;
            while (fromStream.Position < fromStream.Length)
            {
                var toPath = $"{Path.GetFileNameWithoutExtension(fromPath)}.{index++}{Path.GetExtension(fromPath)}";

                using (var toStream = File.OpenWrite(toPath))
                {
                    await CopyAsync(fromStream, toStream, buffer, toSize);
                }

                yield return toPath;
            }
        }

        private static async Task CopyAsync(Stream from, Stream to, byte[] buffer, int limit)
        {
            var count = limit;
            while (count > 0)
            {
                var read = await from.ReadAsync(buffer, 0, Math.Min(buffer.Length, count));

                await to.WriteAsync(buffer, 0, read);

                if (read <= 0)
                {
                    return;
                }

                count -= read;
            }
        }
    }
}
