using UnityEngine;
using Meta.WitAi.Dictation;
using Meta.WitAi;
using Meta.WitAi.Events;
using Oculus.Voice.Dictation;
using Meta.WitAi.TTS.Utilities;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Meta.WitAi.TTS.Data;
using System;
using UnityEditor.Compilation;
using UnityEditor.PackageManager;
using System.Collections;



public class HandleDictation : MonoBehaviour
{
    public AppDictationExperience dictationService;
    public TTSSpeaker speaker;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //dictationService = GetComponent<AppDictationExperience>();
        dictationService = GetComponent<AppDictationExperience>();
        if (dictationService == null )
        {
            Debug.LogError("[AudioTranscription] DictationService not found");
            return;
        }
        if (speaker == null)
        {
            Debug.LogError("[AudioTranscription] Speaker not found");
            return;
        }


        dictationService.DictationEvents.OnFullTranscription.AddListener(OnFullTranscription);
        // Update the event listener to use the corrected method.
        speaker.Events.OnPlaybackComplete.AddListener(OnSpeakingCompleted);
        //dictationService.DictationEvents.OnPartialTranscription.AddListener(OnPartialTranscription);



        dictationService.Activate();

        Debug.Log("[AudioTranscription] Activated");
    }

    private void OnFullTranscription(string text)
    {
        Debug.Log("[AudioTranscription] Full: " + text);
        dictationService.Deactivate();
        StartCoroutine(RunGemini(text));

    }

    // Fix for CS1503: The issue is that the method `OnSpeakingCompleted` does not match the expected signature of the event listener.
    // The `OnPlaybackComplete` event expects a method with the signature `void MethodName(TTSSpeaker speaker, TTSClipData clipData)`.
    // Update the `OnSpeakingCompleted` method to match the expected signature.

    private void OnSpeakingCompleted(TTSSpeaker speaker, TTSClipData clipData)
    {
        Debug.Log("[AudioTranscription] Speaking completed");
        dictationService.Activate();
    }

    private IEnumerator RunGemini(string userPrompt)
    {
        string response = null;
        bool isDone = false;

        string systemPrompt = "I want all responses to be in Spanish. You are a language tutor who aims to teach Spanish to students by simulating an interaction in the farmer's market. Imagine you are the farmer. First respond in very simple spanish, and then explain if the grammer of the user was incorrect only then explain in English. Keep responses brief, as if you are actually talking to the person. These responses will be fed to a text to speech system directly.";

        var apiKey = "AIzaSyBEqPf_jpFRAkN2K6URpCVjoon6JkWQCc0";

        var client = new GeminiClient(apiKey);

        Task.Run(async () =>
        {
            try
            {
                response = await client.GenerateResponseAsync(userPrompt, systemPrompt);
            }
            catch (Exception ex)
            {
                response = "Error: " + ex.Message;
                dictationService.Activate();
            }

            isDone = true;
        });

        while (!isDone)
            yield return null;

        // ✅ Back on main thread: now do something
        Debug.Log("Gemini Response:\n" + response);
        speaker.Speak(response);

    }

}
