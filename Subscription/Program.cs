using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Spur;

namespace Subscription
{
    public class Program
    {
        private static readonly HttpClient _httpClient = new();

        public static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintHelp();
                return;
            }

            var path = Path.Combine(Directory.GetCurrentDirectory(), "../Spur/appsettings");
            var config = new ConfigurationBuilder()
                .AddJsonFile(path + ".json")
                .AddJsonFile(path + ".Development.json", true)
                .Build();
            var section = config.GetSection(nameof(StravaConfig));
            var stravaConfig = section.Get<StravaConfig>() ?? new();

            switch (args[0])
            {
                case "check":
                    await CheckSubscription(
                        stravaConfig.SubscriptionUri!,
                        stravaConfig.ClientId?.ToString()!,
                        stravaConfig.ClientSecret!);
                    break;
                case "create":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Usage: Subscription create <callback url>");
                        break;
                    }
                    await CreateSubscription(
                        stravaConfig.SubscriptionUri!,
                        stravaConfig.ClientId?.ToString()!,
                        stravaConfig.ClientSecret!,
                        args[1],
                        stravaConfig.VerifyToken!);
                    break;
                case "delete":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Usage: Subscription delete <subscription id>");
                        break;
                    }
                    await DeleteSubscription(
                        stravaConfig.SubscriptionUri!,
                        stravaConfig.ClientId?.ToString()!,
                        stravaConfig.ClientSecret!,
                        args[1]);
                    break;
                default:
                    PrintHelp();
                    break;
            }
        }

        private static void PrintHelp() =>
            Console.WriteLine("Usage: Subscription " +
                "[check|create <callback url>|delete <subscription id>]");

        private static async Task CheckSubscription(
            string subscriptionUrl, string clientId, string clientSecret)
        {
            var query = CreateQueryParams(
                ("client_id", clientId),
                ("client_secret", clientSecret));

            var url = $"{subscriptionUrl}?{query}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            Console.WriteLine($"{(int)response.StatusCode} {response.StatusCode}");
            var body = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(body))
                Console.WriteLine(body);
        }

        private static async Task CreateSubscription(
            string subscriptionUrl, string clientId, string clientSecret,
            string callbackUrl, string verifyToken
        )
        {
            var formData = new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "callback_url", callbackUrl },
                { "verify_token", verifyToken },
            });

            var response = await _httpClient.PostAsync(subscriptionUrl, formData);
            response.EnsureSuccessStatusCode();

            Console.WriteLine($"{(int)response.StatusCode} {response.StatusCode}");
            var body = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(body))
                Console.WriteLine(body);
        }

        private static async Task DeleteSubscription(
            string subscriptionUrl, string clientId, string clientSecret, string subscriptionId)
        {
            var query = CreateQueryParams(
                ("client_id", clientId),
                ("client_secret", clientSecret));

            var url = $"{subscriptionUrl}/{subscriptionId}?{query}";
            var response = await _httpClient.DeleteAsync(url);
            response.EnsureSuccessStatusCode();

            Console.WriteLine($"{(int)response.StatusCode} {response.StatusCode}");
            var body = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(body))
                Console.WriteLine(body);
        }

        private static string CreateQueryParams(params (string, string)[] @params)
        {
            var queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
            foreach (var t in @params)
            {
                queryString.Add(t.Item1, t.Item2);
            }
            return queryString.ToString() ?? string.Empty;
        }
    }
}
