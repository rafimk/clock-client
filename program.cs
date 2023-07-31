using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Core.Serialization;
using Azure;
using Azure.AI.Language.Conversations;
using Newtonsoft.Json;

// Import namespaces

namespace clock_client
{
    class Program
    {

        static async Task Main(string[] args)
        {
            try
            {
                // Get config settings from AppSettings
                IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
                IConfigurationRoot configuration = builder.Build();
                //Guid lsAppId = Guid.Parse(configuration["LSAppID"]);
                string predictionEndpoint = configuration["LSPredictionEndpoint"];
                string predictionKey = configuration["LSPredictionKey"];
                string openAIcompletionEndpoint = configuration["OpenAI:CompletionEndpoint"];
                string openAIapiKey = configuration["OpenAI:APIKey"];

                // Create a client for the Language service model
                Uri endpoint = new Uri(predictionEndpoint);
                AzureKeyCredential credential = new AzureKeyCredential(predictionKey);

                ConversationAnalysisClient client = new ConversationAnalysisClient(endpoint, credential);

                // Get user input (until they enter "quit")
                string userText = "";
                while (userText.ToLower() != "quit")
                {
                    Console.WriteLine("\nEnter some text ('quit' to stop)");
                    userText = Console.ReadLine();
                    if (userText.ToLower() != "quit")
                    {

                        var topIndentWithPredictionResponse = await GetTopIndentWithPrediction(userText, client);
                        var jsonString = topIndentWithPredictionResponse.ConversationPrediction.ToString();

                        IntentObject intentObject = JsonConvert.DeserializeObject<IntentObject>(jsonString);

                        // Find the intent with the maximum confidence score
                        IntentData maxConfidenceIntent = null;
                        foreach (IntentData intent in intentObject.Intents)
                        {
                            if (maxConfidenceIntent == null || intent.ConfidenceScore > maxConfidenceIntent.ConfidenceScore)
                            {
                                maxConfidenceIntent = intent;
                            }
                        }

                        // Output the result
                        if (maxConfidenceIntent != null)
                        {
                            Console.WriteLine($"Maximum Confidence Score: {maxConfidenceIntent.ConfidenceScore}");
                            Console.WriteLine($"Category: {maxConfidenceIntent.Category}");
                        }
                        else
                        {
                            Console.WriteLine("No intents found.");
                        }


                        switch (topIndentWithPredictionResponse.TopIntent)
                        {
                            case "GetTime":
                                var location = "local";
                                // Check for a location entity
                                foreach (dynamic entity in topIndentWithPredictionResponse.ConversationPrediction.Entities)
                                {
                                    if (entity.Category == "Location")
                                    {
                                        //Console.WriteLine($"Location Confidence: {entity.ConfidenceScore}");
                                        location = entity.Text;
                                    }
                                }
                                // Get the time for the specified location
                                string timeResponse = GetTime(location);
                                Console.WriteLine(timeResponse);
                                break;
                            case "GetDay":
                                var date = DateTime.Today.ToShortDateString();
                                // Check for a Date entity
                                foreach (dynamic entity in topIndentWithPredictionResponse.ConversationPrediction.Entities)
                                {
                                    if (entity.Category == "Date")
                                    {
                                        //Console.WriteLine($"Location Confidence: {entity.ConfidenceScore}");
                                        date = entity.Text;
                                    }
                                }
                                // Get the day for the specified date
                                string dayResponse = GetDay(date);
                                Console.WriteLine(dayResponse);
                                break;
                            case "GetDate":
                                var day = DateTime.Today.DayOfWeek.ToString();
                                // Check for entities            
                                // Check for a Weekday entity
                                foreach (dynamic entity in topIndentWithPredictionResponse.ConversationPrediction.Entities)
                                {
                                    if (entity.Category == "Weekday")
                                    {
                                        //Console.WriteLine($"Location Confidence: {entity.ConfidenceScore}");
                                        day = entity.Text;
                                    }
                                }
                                // Get the date for the specified day
                                string dateResponse = GetDate(day);
                                Console.WriteLine(dateResponse);
                                break;
                            default:
                                // Some other intent (for example, "None") was predicted
                                Console.WriteLine("Try asking me for the time, the day, or the date.");
                                string noneResponse = await GetChatGptResponse(userText, openAIcompletionEndpoint, openAIapiKey);
                                Console.WriteLine(noneResponse);
                                break;
                        }

                        Console.WriteLine("Try asking me for the time, the day, or the date.");
                        string gptResponse = await GetChatGptResponse(userText, openAIcompletionEndpoint, openAIapiKey);
                        Console.WriteLine(gptResponse);

                    }

                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static string GetTime(string location)
        {
            var timeString = "";
            var time = DateTime.Now;

            /* Note: To keep things simple, we'll ignore daylight savings time and support only a few cities.
               In a real app, you'd likely use a web service API (or write  more complex code!)
               Hopefully this simplified example is enough to get the the idea that you
               use LU to determine the intent and entities, then implement the appropriate logic */

            switch (location.ToLower())
            {
                case "local":
                    timeString = time.Hour.ToString() + ":" + time.Minute.ToString("D2");
                    break;
                case "london":
                    time = DateTime.UtcNow;
                    timeString = time.Hour.ToString() + ":" + time.Minute.ToString("D2");
                    break;
                case "sydney":
                    time = DateTime.UtcNow.AddHours(11);
                    timeString = time.Hour.ToString() + ":" + time.Minute.ToString("D2");
                    break;
                case "new york":
                    time = DateTime.UtcNow.AddHours(-5);
                    timeString = time.Hour.ToString() + ":" + time.Minute.ToString("D2");
                    break;
                case "nairobi":
                    time = DateTime.UtcNow.AddHours(3);
                    timeString = time.Hour.ToString() + ":" + time.Minute.ToString("D2");
                    break;
                case "tokyo":
                    time = DateTime.UtcNow.AddHours(9);
                    timeString = time.Hour.ToString() + ":" + time.Minute.ToString("D2");
                    break;
                case "delhi":
                    time = DateTime.UtcNow.AddHours(5.5);
                    timeString = time.Hour.ToString() + ":" + time.Minute.ToString("D2");
                    break;
                default:
                    timeString = "I don't know what time it is in " + location;
                    break;
            }

            return timeString;
        }

        static string GetDate(string day)
        {
            string date_string = "I can only determine dates for today or named days of the week.";

            // To keep things simple, assume the named day is in the current week (Sunday to Saturday)
            DayOfWeek weekDay;
            if (Enum.TryParse(day, true, out weekDay))
            {
                int weekDayNum = (int)weekDay;
                int todayNum = (int)DateTime.Today.DayOfWeek;
                int offset = weekDayNum - todayNum;
                date_string = DateTime.Today.AddDays(offset).ToShortDateString();
            }
            return date_string;

        }

        static string GetDay(string date)
        {
            // Note: To keep things simple, dates must be entered in US format (MM/DD/YYYY)
            string day_string = "Enter a date in MM/DD/YYYY format.";
            DateTime dateTime;
            if (DateTime.TryParse(date, out dateTime))
            {
                day_string = dateTime.DayOfWeek.ToString();
            }

            return day_string;
        }

        static async Task<TopIndentWithPredictionResponse> GetTopIndentWithPrediction(string userText, ConversationAnalysisClient client)
        {
            // Call the Language service model to get intent and entities
            var projectName = "Clock";
            var deploymentName = "production";
            var data = new
            {
                analysisInput = new
                {
                    conversationItem = new
                    {
                        text = userText,
                        id = "1",
                        participantId = "1",
                    }
                },
                parameters = new
                {
                    projectName,
                    deploymentName,
                    // Use Utf16CodeUnit for strings in .NET.
                    stringIndexType = "Utf16CodeUnit",
                },
                kind = "Conversation",
            };
            // Send request
            Response response = await client.AnalyzeConversationAsync(RequestContent.Create(data));
            dynamic conversationalTaskResult = response.Content.ToDynamicFromJson(JsonPropertyNames.CamelCase);
            dynamic conversationPrediction = conversationalTaskResult.Result.Prediction;
            Console.WriteLine(userText);
            var topIntent = "";
            if (conversationPrediction.Intents[0].ConfidenceScore > 0.5)
            {
                topIntent = conversationPrediction.TopIntent;
            }

            return new TopIndentWithPredictionResponse
            {
                TopIntent = topIntent,
                ConversationPrediction = conversationPrediction
            };
        }

        static async Task<string> GetChatGptResponse(string input, string chatGptApiUrl, string apiKey)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

                var request = new
                {
                    model = "text-davinci-003",
                    prompt = input,
                    temperature = 0.7,
                    max_tokens = 150
                };

                var content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

                var httpResponse = await httpClient.PostAsync(chatGptApiUrl, content);
                var responseContent = await httpResponse.Content.ReadAsStringAsync();

                return responseContent;
            }
        }
    }
}