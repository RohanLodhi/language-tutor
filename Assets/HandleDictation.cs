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

//public class HandleDictation : MonoBehaviour
//{
//    [Header("Services & References")]
//    public AppDictationExperience dictationService;
//    public TTSSpeaker speaker;
//    public Animator vendorAnimator;    // your “Talking” trigger lives here

//    bool playerInRange = false;

//    void Start()
//    {
//        dictationService.DictationEvents
//            .OnFullTranscription.AddListener(OnFullTranscription);

//        speaker.Events
//            .OnPlaybackComplete.AddListener(OnSpeakingCompleted);

//        dictationService.Deactivate();
//    }

//    // **new** public method for your button
//    public void OnSpeakButtonPressed()
//    {
//        if (!dictationService.Active && playerInRange)
//        {
//            vendorAnimator.SetTrigger("Talking");
//            dictationService.Activate();
//        }
//    }

//    private void OnFullTranscription(string text)
//    {
//        dictationService.Deactivate();
//        StartCoroutine(RunGemini(text));
//    }

//    private void OnSpeakingCompleted(TTSSpeaker s, TTSClipData clip)
//    {
//        vendorAnimator.ResetTrigger("Talking");
//        // Optionally re‑enable the button or any prompt you have
//    }

//    private IEnumerator RunGemini(string userPrompt)
//    {
//        string response = null;
//        bool isDone = false;

//        string systemPrompt = "I want all responses to be in Spanish. You are a language tutor who aims to teach Spanish to students by simulating an interaction in the farmer's market. Imagine you are the farmer. First respond in very simple spanish, and then explain if the grammer of the user was incorrect only then explain in English. Keep responses brief, as if you are actually talking to the person. These responses will be fed to a text to speech system directly.";

//        var apiKey = "AIzaSyBEqPf_jpFRAkN2K6URpCVjoon6JkWQCc0";

//        var client = new GeminiClient(apiKey);

//        Task.Run(async () =>
//        {
//            try
//            {
//                response = await client.GenerateResponseAsync(userPrompt, systemPrompt);
//            }
//            catch (Exception ex)
//            {
//                response = "Error: " + ex.Message;
//                dictationService.Activate();
//            }

//            isDone = true;
//        });

//        while (!isDone)
//            yield return null;

//        // ✅ Back on main thread: now do something
//        Debug.Log("Gemini Response:\n" + response);
//        speaker.Speak(response);

//    }

//    private void OnTriggerEnter(Collider other)
//    {
//        if (other.CompareTag("Player"))
//            playerInRange = true;
//    }

//    private void OnTriggerExit(Collider other)
//    {
//        if (other.CompareTag("Player"))
//            playerInRange = false;
//    }
//}

public class HandleDictation : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public AppDictationExperience dictationService;
    public TTSSpeaker speaker;
    public Animator vendorAnimator;   // your "Talking" trigger parameter

    bool playerInRange = false;

    void Start()
    {
        vendorAnimator = GetComponent<Animator>();
        Debug.Log("[AnimatorPrint]" + vendorAnimator.ToString());
        // Hook up events
        dictationService.DictationEvents.OnFullTranscription.AddListener(OnFullTranscription);
        speaker.Events.OnPlaybackComplete.AddListener(OnSpeakingCompleted);

        // Start with dictation off
        dictationService.Deactivate();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            // start listening & talking animation immediately
            vendorAnimator.SetTrigger("Talking");
            dictationService.Activate();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            // stop listening & reset animation
            dictationService.Deactivate();
            vendorAnimator.ResetTrigger("Talking");
        }
    }

    private void OnFullTranscription(string text)
    {
        // got user text → stop mic, send to LLM
        dictationService.Deactivate();
        StartCoroutine(RunGemini(text));
    }

    private void OnSpeakingCompleted(TTSSpeaker s, TTSClipData clip)
    {
        // when TTS finishes, reset talking anim
        //vendorAnimator.ResetTrigger("Talking");

        // if player is still in range, start another listen cycle
        if (playerInRange)
        {
            vendorAnimator.SetTrigger("Talking");
            dictationService.Activate();
        }
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