using UnityEngine;
using UnityEngine.Windows.Speech;
using TMPro;

public class HoloLensAutoDictation : MonoBehaviour
{
    public TMP_Text speechText;

    private DictationRecognizer dictationRecognizer;
    private string fullText = "";
    private string conversationHistory = "";

    void Start()
    {
        dictationRecognizer = new DictationRecognizer();

        dictationRecognizer.DictationHypothesis += OnHypothesis;
        dictationRecognizer.DictationResult += OnResult;
        dictationRecognizer.DictationComplete += OnComplete;
        dictationRecognizer.DictationError += OnError;

        speechText.text = "En attente d'une phrase...";
        PhraseRecognitionSystem.Shutdown();
        fullText = "";
        dictationRecognizer.Start();
    }

    private void OnHypothesis(string text)
    {
        //speechText.text = text;
    }

    private void OnResult(string text, ConfidenceLevel confidence)
    {
        conversationHistory  += text + "\n";
        fullText += text + "\n";
        speechText.text = fullText;
    }

    private void OnComplete(DictationCompletionCause cause)
    {
        speechText.text = "En attente d'une phrase...";


        // Restart automatically
        if (dictationRecognizer != null)
        {
            PhraseRecognitionSystem.Shutdown();
            dictationRecognizer.Start();
            speechText.text = "En attente d'une phrase...";
            fullText = "";
        }
    }

    private void OnError(string error, int hresult)
    {
        speechText.text = "Error: " + error;
    
    }

    private void OnDestroy()
    {
        speechText.text = "OnDestroy...";
        if (dictationRecognizer != null)
        {
            if (dictationRecognizer.Status == SpeechSystemStatus.Running)
                dictationRecognizer.Stop();

            dictationRecognizer.Dispose();
        }
    }
    public void OnReformulationButtonClicked()
    {
        string prompt =
            "Tu es un assistant de communication.\n" +
            "Voici le contexte de la conversation :\n" +
            conversationHistory + "\n\n" +
            "Reformule uniquement cette phrase :\n" +
            fullText + "\n\n" +
            "La reformulation doit être courte, claire et simple.\n" +
            "Ne reformule pas tout le contexte.\n" +
            "Réponds uniquement avec la phrase reformulée.";

        // Envoyer prompt à l'IA
    }
}