using System;
using System.Net.Http;
using System.Net;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Threading;

namespace JokeAPICli
{
    class Program
    {
        private static HttpClient client = new HttpClient();
        private static HttpRequestMessage httpRequestMessage;

        /// <summary>
        /// Main method
        /// </summary>
        /// <param name="args">Command line argments, optional, it'll prompt otherwise. The args are: categories, flags, keywords</param>
        static void Main(string[] args)
        {
            string choice = "";
            string url = "";
            // Sort out args
            if (args.Length != 0 && args.Length == 3)
            {
                url = GenerateURL(args[0], args[1], args[2]);
            }
            else if (args.Length != 0 && args.Length != 3)
            {
                throw new ArgumentException("Invalid amount of arguements");
            }
            else
            {
                url = MainMenu();
            }
            
            while (choice.ToLower() != "exit")
            {
                HttpResponseMessage httpResponse = MakeRequest(url);
                Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(httpResponse.Content.ReadAsStringAsync().Result);
                if (json["type"].ToString() == "single")
                {
                    Console.WriteLine("\n" + json["joke"].ToString());
                }
                else if (json["type"].ToString() == "twopart")
                {
                    Console.WriteLine("\n" + json["setup"].ToString());
                    Thread.Sleep(3000);
                    Console.WriteLine("\n" + json["delivery"].ToString());

                }
                Console.Write("\nMore of the same (exit to exit, menu to return to the main menu)?");
                choice = Console.ReadLine();
                if (choice == "menu")
                {
                    if (args.Length == 3)
                    {
                        url = GenerateURL(args[0], args[1], args[2]);
                    }
                    else
                    {
                        url = MainMenu();
                    }
                }

            }
        }

        /// <summary>
        /// Main menu
        /// </summary>
        /// <returns>Generated URL</returns>
        private static string MainMenu()
        {
            // Get categories
            HttpResponseMessage httpResponse = MakeRequest("https://sv443.net/jokeapi/v2/categories?format=json");
            // Get user's choice of categories
            string categoryResult = Choices(httpResponse, "Choose joke category (one or a comma seprated list): ", "categories");
            if (categoryResult == "")
            {
                categoryResult = "Any";
            }
            // Get flags
            httpResponse = MakeRequest("https://sv443.net/jokeapi/v2/flags?format=json");
            Console.WriteLine();
            // Get user's choice of flags
            string flagsResult = Choices(httpResponse, "Choose joke flags to exclude (one or a comma seprated list, enter to exclude none): ", "flags");
            // Get keywords
            Console.Write("Enter keywords to search: ");
            string keywordsResult = Console.ReadLine();
            return GenerateURL(categoryResult, flagsResult, keywordsResult);
        }

        /// <summary>
        /// Generate joke API URL 
        /// </summary>
        /// <returns>Generated URL</returns>
        private static string GenerateURL(string categoryResult, string flagsResult, string keywordsResult)
        {
            // Generate URL
            string baseJokeURL = "https://sv443.net/jokeapi/v2/joke/" + categoryResult + "?format=json";
            if (flagsResult != "")
            {
                baseJokeURL += "&blacklistFlags=" + flagsResult;
            }
            if (keywordsResult != "")
            {
                baseJokeURL += "&contains=" + Uri.EscapeDataString(keywordsResult);
            }
            return baseJokeURL;
        }

        /// <summary>
        /// Make request
        /// </summary>
        /// <param name="requestUri">string representing the URL API</param>
        /// <param name="ContentAcceptType">string representing the content to accept from the API Defaults to application/json</param>
        /// <returns>HttpResponseMessage containing the result</returns>
        public static HttpResponseMessage MakeRequest(string requestUri, string ContentAcceptType="application/json")
        {

            httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(requestUri),
                Headers = {
                        { HttpRequestHeader.Accept.ToString(), ContentAcceptType },
                    },
            };
            return client.SendAsync(httpRequestMessage).Result;
        }

        /// <summary>
        /// Display choices derived from a HttpResponseMessage and return the result
        /// </summary>
        /// <param name="httpResponse">HTTP response message</param>
        /// <param name="question">Question string</param>
        /// <param name="item">Item to select from JSON</param>
        /// <returns>Base64 encoded string</returns>
        public static string Choices(HttpResponseMessage httpResponse, string question, string item)
        {
            if (httpResponse.IsSuccessStatusCode)
            {
                // Turn JSON result into a Dictionary
                Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(httpResponse.Content.ReadAsStringAsync().Result);
                // Strip bad chars from the item we want to look at (we assume this is a JSON list)
                Regex regex = new Regex("[^a-zA-Z,]");
                // Split at the commas
                string[] categories = regex.Replace(json[item].ToString(), "").Split(",");
                for (int i = 0; i < categories.Length; i++)
                {
                    Console.Write(i + 1);
                    Console.WriteLine(" - " + categories[i]);
                }
                Console.Write(question);
                string choice = Console.ReadLine();
                if (choice == "")
                {
                    return "";
                }
                // If there are multiple choices deal with them
                else if (choice.Contains(','))
                {
                    string returnValue = "";
                    for (int i = 0; i < choice.Split(',').Length; i++)
                    {
                        if (i == choice.Split(',').Length - 1)
                        {
                            returnValue += categories[int.Parse(choice.Split(',')[i]) - 1];
                        }
                        else
                        {
                            returnValue += categories[int.Parse(choice.Split(',')[i]) - 1] + ",";
                        }
                    }
                    return returnValue;
                }
                else
                {
                    return categories[int.Parse(choice) - 1];
                }
            }
            else
            {
                throw new HttpRequestException($"HTTP request did not return success. The return code was {httpResponse.StatusCode} ({httpResponse.ReasonPhrase})");
            }
        }
    }
}