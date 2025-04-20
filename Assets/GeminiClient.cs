using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public class GeminiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;

    public GeminiClient(string apiKey)
    {
        _httpClient = new HttpClient();
        _endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash-thinking-exp-01-21:generateContent?key={apiKey}";
    }

    public async Task<GeminiResponse> GenerateResponseAsync(List<GeminiMessage> conversation, string systemPrompt = null)
    {
        var contents = new List<object>();

        foreach (var message in conversation)
        {
            var partsList = new List<object>();
            foreach (var part in message.Parts)
            {
                partsList.Add(new { text = part.text });
            }

            contents.Add(new
            {
                role = message.Role,
                parts = partsList
            });
        }

        var requestBody = new Dictionary<string, object>
        {
            { "contents", contents }
        };

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            requestBody["system_instruction"] = new
            {
                parts = new List<object>
                {
                    new { text = systemPrompt }
                }
            };
        }

        var json = JsonConvert.SerializeObject(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(_endpoint, content);

        if (!response.IsSuccessStatusCode)
        {
            var errorDetails = await response.Content.ReadAsStringAsync();
            throw new Exception($"Gemini API Error: {response.StatusCode} - {errorDetails}");
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        var geminiResponse = JsonConvert.DeserializeObject<GeminiResponse>(responseJson);

        return geminiResponse;
    }
}

[Serializable]
public class GeminiResponse
{
    public Candidate[] candidates;
}

[Serializable]
public class Candidate
{
    public Content content;
}

[Serializable]
public class Content
{
    public Part[] parts;
    public string role;
}

[Serializable]
public class Part
{
    public string text;
}

[Serializable]
public class GeminiMessage
{
    public string Role; // e.g., "user", "model", "system"
    public List<Part> Parts;
}