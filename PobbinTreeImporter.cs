using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace GemGuide;

public class PobbinTreeImporter
{
    private static readonly Regex MaxRollUrlRegex = new Regex(@"^https://pobb\.in/(?<pobId>[^/]+)/?$", RegexOptions.Compiled);

    public async Task<string> GetPobCode(string url, CancellationToken cancellationToken)
    {
        if (MaxRollUrlRegex.Match(url.Trim()) is not { Success: true } match)
        {
            throw new Exception($"url '{url}' does not match expected regex {MaxRollUrlRegex}");
        }

        var groupId = match.Groups["pobId"].Value;
        var dataUrl = $"https://pobb.in/{groupId}/raw";
        var dataString = await new HttpClient().GetStringAsync(dataUrl, cancellationToken);
        return dataString;
    }

    public bool IsMatch(string url) => MaxRollUrlRegex.IsMatch(url.Trim());
}