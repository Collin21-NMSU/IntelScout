using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;
using Spectre.Console;

class IntelScout
{
    private static readonly HttpClient client = new HttpClient();

    static async Task Main(string[] args)
    {
        //banner for title
        AnsiConsole.Write(new FigletText("Intel Scout").LeftJustified().Color(Color.Red));

        string apiKey = Environment.GetEnvironmentVariable("ABUSEIPD_API_Key") ?? "API_KEY_HERE";

        if (apiKey == "API_KEY_HERE")
        {
            AnsiConsole.MarkupLine("[bold red]Warning:[/] Please set your AbuseIPD API key in the code or enviornment variables.");
        }

        //determine target IPs based on either CLI arguments or txt file)
        List<string> targetIps = new List<string>();

        if (args.Length > 0 && args[0] == "--file" && args.Length > 1)
        {
            string filePath = args[1];
            if (File.Exists(filePath))
            {
                targetIps = File.ReadAllLines(filePath)
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Select(line => line.Trim())
                    .ToList();
                AnsiConsole.MarkupLine($"[green]Loaded {targetIps.Count} targets from {filePath}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[bold red]Error:[/] File not found: {filePath}");
                return;
            }
        }
        else if (args.Length > 0)
        {
            //taking cli args
            targetIps = args.ToList();
        }
        else
        {
            targetIps = new List<string> { "1.1.1.1", "8.8.8.8", "185.220.101.5" };
            AnsiConsole.MarkupLine("[dim]No arguments provide. Using sample IPs...[/]");
        }

        //display table creation
        var table = new Table();
        table.AddColumn("[cyan]IP Address[/]");
        table.AddColumn("[magenta]Country[/]");
        table.AddColumn("ISP");
        table.AddColumn(new TableColumn("Reports").RightAligned());
        table.AddColumn(new TableColumn("Abuse Score").RightAligned());

        await AnsiConsole.Status()
            .StartAsync("Querying threat intelligence data", async ctx =>
            {
                foreach (string ip in targetIps)
                {
                    ctx.Status($"Checking IP: {ip}...");
                    var result = await CheckIpAsync(ip, apiKey);

                    string scoreMarkup;
                    if (result.Score > 50)
                        scoreMarkup = $"[bold red]{result.Score}%[/]";
                    else if (result.Score < 15)
                        scoreMarkup = $"[yellow]{result.Score}%[/]";
                    else
                        scoreMarkup = $"[green]{result.Score}%[/]";

                    table.AddRow(
                        result.Ip,
                        result.Country,
                        Markup.Escape(result.Isp),
                        result.Reports.ToString(),
                        scoreMarkup
                    );
                }
            });
        AnsiConsole.WriteLine();
        AnsiConsole.Write(table);
    }

    private static async Task<IpResult> CheckIpAsync(string ip, string apiKey)
    {
        string url = $"https://api.abuseipdb.com/api/v2/check?ipAddress={ip}&maxAgeInDays=90";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Accept", "application/json");
        request.Headers.Add("Key", apiKey);

        try
        {
            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                string jsonString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonString);
                var data = doc.RootElement.GetProperty("data");

                return new IpResult
                {
                    Ip = ip,
                    Score = data.GetProperty("abuseConfidenceScore").GetInt32(),
                    Country = data.GetProperty("countryCode").GetString() ?? "N/A",
                    Isp = data.GetProperty("isp").GetString() ?? "Unknown",
                    Reports = data.GetProperty("totalReports").GetInt32()
                };

            }
        }
        catch (Exception)
        {
            //fallback for demo or error states
        }

        return new IpResult { Ip = ip, Score = 0, Country = "ERR", Isp = "Connection Failed", Reports = 0 };

    }

}

public class IpResult
{
    public string Ip { get; set; } = string.Empty;
    public int Score { get; set; }
    public string Country { get; set; } = string.Empty;
    public string Isp { get; set; } = string.Empty;
    public int Reports { get; set; }
}