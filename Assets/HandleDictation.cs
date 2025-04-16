using UnityEngine;
using Meta.WitAi.Dictation;
using Meta.WitAi;
using Meta.WitAi.Events;
using Oculus.Voice.Dictation;



public class HandleDictation : MonoBehaviour
{
    public AppDictationExperience dictationService;
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

        dictationService.DictationEvents.OnFullTranscription.AddListener(OnFullTranscription);
        //dictationService.DictationEvents.OnPartialTranscription.AddListener(OnPartialTranscription);

        dictationService.Activate();

        Debug.Log("[AudioTranscription] Activated");
    }
    private void OnFullTranscription (string text)
    {
        Debug.Log("[AudioTranscription] Full: " + text);
        dictationService.Deactivate();

        dictationService.Activate();
    }

}
