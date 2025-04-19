using System;
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

    public async Task<string> GenerateResponseAsync(string userPrompt, string systemPrompt = null)
    {
        var parts = new[]
        {
            new { text = systemPrompt ?? string.Empty },
            new { text = userPrompt }
        };

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = parts
                }
            }
        };

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

        return geminiResponse?.candidates?[0]?.content?.parts?[0]?.text ?? "No response text found.";
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
