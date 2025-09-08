using G2.Infrastructure.Model;
using System.Diagnostics;
using System.Text.Json;

namespace G2.Infrastructure.TechnologyDetector
{
    public class Wappalyzer : IWappalyzer
    {
        private string node = "node";
        private string script = "../wappalyzer/src/drivers/npm/cli.js";

        public async Task<WappalyzerResult?> Detect(string url)
        {
            try
            {
                Process process = new()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = node,
                        Arguments = $"\"{script}\" \"{url}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                process.Start();
                
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync(cts.Token);

                var output = await outputTask;
                var error = await errorTask;

                if (!string.IsNullOrWhiteSpace(error))
                {
                    Console.WriteLine(error);
                    return null;
                }

                Console.WriteLine(output);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                WappalyzerResult result = JsonSerializer.Deserialize<WappalyzerResult>(output, options);
                return result;
            } catch (Exception)
            {
                return null;
            }
        }
    }
}