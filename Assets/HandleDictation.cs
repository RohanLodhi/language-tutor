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
using System.Collections.Generic;
using TMPro;


public class HandleDictation : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public AppDictationExperience dictationService;
    public TTSSpeaker speaker;
    public Animator vendorAnimator;   // your "Talking" trigger parameter
    public TextMeshProUGUI subtitles;

    private List<GeminiMessage> conversationHistory = new();
    bool playerInRange = false;
    bool isProcessing = false;

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
            dictationService.Activate();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            dictationService.Deactivate();
            // stop listening & reset animation
        }
    }

    private void OnFullTranscription(string text)
    {
        // got user text → stop mic, send to LLM
        isProcessing = true;
        dictationService.Deactivate();
        Debug.Log(text);
        StartCoroutine(RunGemini(text));
    }

    private void OnSpeakingCompleted(TTSSpeaker s, TTSClipData clip)
    {
        // when TTS finishes, reset talking anim
        //vendorAnimator.ResetTrigger("Talking");

        // if player is still in range, start another listen cycle
        Debug.Log("Speaking completed");
        if (playerInRange)
        {
            vendorAnimator.ResetTrigger("Talking");
            dictationService.Activate();
            isProcessing = false;
        }
    }

    //private void Update()
    //{
    //    if (OVRInput.Get(OVRInput.Button.Two))
    //    {
    //        Debug.Log("Button pressed");
    //    }
    //    // if player is in range, not already processing, and button is held down, start listening
    //    if (playerInRange && !isProcessing && OVRInput.Get(OVRInput.Button.One))
    //    {
    //        if (!dictationService.isActiveAndEnabled)
    //        {
    //            dictationService.Activate();
    //            Debug.Log("Dictation activated");
    //        }
    //    }
    //    else
    //    {
    //        if (dictationService.isActiveAndEnabled)
    //        {
    //            dictationService.Deactivate();
    //            Debug.Log("Dictation deactivated");
    //        }
    //    }
    //}

    private IEnumerator RunGemini(string userPrompt)
    {
        string response = null;
        bool isDone = false;

        string systemPrompt = "I want all responses to be in Spanish. You are a language tutor who aims to teach Spanish to students by simulating an interaction in the farmer's market. Imagine you are the farmer. First respond in very simple spanish, and then explain if the grammar of the user was incorrect only then explain in English. Keep responses brief, as if you are actually talking to the person. You should not speak more than one or two sentences. At max 250 characters. These responses will be fed to a text to speech system directly. Do not have markdown elements in the response. After each response, translate the phrase you said in english. Separate the translation from the spanish text and english explanation if any using the following character '|'. Example:  'quiero comprar verduras | I want to buy vegetables'";

        var apiKey = "AIzaSyBEqPf_jpFRAkN2K6URpCVjoon6JkWQCc0";

        var client = new GeminiClient(apiKey);

        var spanishText = "";
        var translation = "";

        Task.Run(async () =>
        {

            try
            {
                conversationHistory.Add(new GeminiMessage
                {
                    Role = "user",
                    Parts = new List<Part>
                    {
                        new() { text = userPrompt }
                    }
                });

                var geminiResponse = await client.GenerateResponseAsync(conversationHistory, systemPrompt);
                response = geminiResponse?.candidates?[0]?.content?.parts?[0]?.text ?? "No response text found.";

                Debug.Log("Original Response" + response);

                if (response != "No response text found.")
                {
                    var split = response.Split("|");
                    if (split.Length == 1)
                    {
                        spanishText = split[0].Trim();
                    }
                    else if (split.Length >= 2)
                    {
                        spanishText = split[0].Trim();
                        translation = split[1].Trim();
                    }

                    conversationHistory.Add(new GeminiMessage
                    {
                        Role = "model",
                        Parts = new List<Part>
                    {
                        new() { text = spanishText}
                    }
                    });
                }
                else
                {
                    translation = response;
                }
            }
            catch (Exception ex)
            {
                translation = "Some error occurred, please try again.";
                Debug.LogError("Gemini API Error: " + ex.Message);
            }

            isDone = true;
        });

        while (!isDone)
            yield return null;

        Debug.Log("Gemini Response:\n" + spanishText);
        subtitles.text = translation;
        try
        {
            Debug.Log("Speaking Now");
            vendorAnimator.SetTrigger("Talking");
            if (spanishText.Length > 0)
            {
                speaker.Speak(spanishText);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error playing TTS: " + ex.Message);
        }
    }
}