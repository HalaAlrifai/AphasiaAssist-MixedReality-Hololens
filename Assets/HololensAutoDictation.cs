using System;
using System.Collections;
using System.Text;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Windows.Speech;

public class HoloLensAutoDictation : MonoBehaviour
{
    [Header("Interface")]
    public TMP_Text speechText;
    public TMP_Text reformulationText;

    [Header("Gemini")]
    [SerializeField] private string apiKey = "";

    private const string GeminiModel = "gemini-3.1-flash-lite";

    private const string GeminiUrl =
        "https://generativelanguage.googleapis.com/" +
        "v1beta/models/" +
        GeminiModel +
        ":generateContent";

    private DictationRecognizer dictationRecognizer;

    // Phrase actuellement reconnue
    private string fullText = "";

    // Dernière phrase terminée
    private string lastPhrase = "";

    // Historique utilisé comme contexte par Gemini
    private string conversationHistory = "";

    private bool geminiRequestInProgress = false;
    private bool applicationIsClosing = false;
    private bool backClicked = false;
    private void OnEnable()
    {
        speechText.text = "Onenble";

        PhraseRecognitionSystem.Shutdown();
        dictationRecognizer.Start();

    }

    private void Start()
    {
       
        speechText.text = "Préparation de l'écoute...";
        reformulationText.text =
            "La reformulation apparaîtra ici...";

        dictationRecognizer = new DictationRecognizer();

        dictationRecognizer.DictationHypothesis += OnHypothesis;
        dictationRecognizer.DictationResult += OnResult;
        dictationRecognizer.DictationComplete += OnComplete;
        dictationRecognizer.DictationError += OnError;
      



        fullText = "";

        try
        {
            dictationRecognizer.Start();
            speechText.text = "En attente d'une phrase...";
        }
        catch (Exception exception)
        {
            speechText.text =
                "Impossible de démarrer l'écoute.";

            Debug.LogError(
                "Erreur au démarrage de la dictée : " +
                exception.Message
            );
        }
    }

    private void OnHypothesis(string text)
    {
        // Facultatif :
        // afficher le texte provisoire pendant que la personne parle.
        //
        // speechText.text = text;
    }

    private void OnResult(
        string text,
        ConfidenceLevel confidence)
    {
        string recognizedText = text.Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        fullText = text + " ";
        conversationHistory += text + "\n";

        speechText.text = fullText.Trim();
    }

    private void OnComplete(
        DictationCompletionCause cause)
    {
        if (backClicked)
        {
            backClicked = false;
            return;
        }
        if (applicationIsClosing)
        {
            return;
        }

        // Sauvegarder la phrase avant de vider fullText.
        if (!string.IsNullOrWhiteSpace(fullText))
        {
            lastPhrase = fullText.Trim();
        }

        fullText = "";

        speechText.text =
            "En attente d'une nouvelle phrase...";
        if (dictationRecognizer != null)
        {
            try
            {
                PhraseRecognitionSystem.Shutdown();

                if (dictationRecognizer.Status !=
                    SpeechSystemStatus.Running)
                {
                    dictationRecognizer.Start();
                }
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "Impossible de redémarrer la dictée : " +
                    exception.Message
                );
            }
        }
    }

    private void OnError(
        string error,
        int hresult)
    {
        speechText.text =
            "Erreur vocale : " + error;

        Debug.LogError(
            "Erreur de dictée : " + error +
            "\nHResult : " + hresult
        );
    }

    public void OnBackButtonClicked()
    {
        backClicked = true;

        if (dictationRecognizer != null &&
            dictationRecognizer.Status == SpeechSystemStatus.Running)
        {
            dictationRecognizer.Stop();
        }

        PhraseRecognitionSystem.Restart();
    }

    public void OnReformulationButtonClicked()
    {
        if (geminiRequestInProgress)
        {
            reformulationText.text =
                "Une reformulation est déjà en cours.";

            return;
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            reformulationText.text =
                "La clé API Gemini est vide.";

            return;
        }

        /*
         * Si une phrase est en cours, utiliser fullText.
         * Sinon, utiliser la dernière phrase terminée.
         */
        string phraseToReformulate =
            !string.IsNullOrWhiteSpace(fullText)
                ? fullText.Trim()
                : lastPhrase;

        if (string.IsNullOrWhiteSpace(phraseToReformulate))
        {
            reformulationText.text =
                "Aucune phrase à reformuler.";

            return;
        }

        StartCoroutine(
            SendToGeminiWithRetry(
                phraseToReformulate,
                conversationHistory
            )
        );
    }

    private IEnumerator SendToGeminiWithRetry(
        string phraseToReformulate,
        string context)
    {
        geminiRequestInProgress = true;
        reformulationText.text =
            "Reformulation en cours...";

        // Première tentative
        bool success = false;

        yield return StartCoroutine(
            SendToGemini(
                phraseToReformulate,
                context,
                result => success = result
            )
        );

        // Une deuxième tentative en cas d'erreur réseau temporaire.
        if (!success)
        {
            reformulationText.text =
                "Nouvelle tentative de connexion...";

            yield return new WaitForSeconds(2f);

            yield return StartCoroutine(
                SendToGemini(
                    phraseToReformulate,
                    context,
                    result => success = result
                )
            );
        }

        geminiRequestInProgress = false;

        if (!success)
        {
            reformulationText.text =
                "Connexion à Gemini impossible.\n" +
                "Vérifiez le réseau du HoloLens.";
        }
    }

    private IEnumerator SendToGemini(
        string phraseToReformulate,
        string context,
        Action<bool> onFinished)
    {
        string prompt =
    "Tu es un assistant de communication pour faciliter la compréhension pour les personnes aphasiques quand elles parlent ou leur interlocuteur.\n\n" +
    "Contexte de la conversation :\n" +
    context +
    "\n\nPhrase à reformuler :\n" +
    phraseToReformulate +
    "\n\nConsignes obligatoires :\n" +
    "- n'ajouter pas une phrase de debut ou de fin du type \"Voici la reformulation\".\n" +
    "-commencer la reformulation par la phrase reformulée sans texte supplémentaire.\n" +
    "- simplifier le texte pour qu'il soit plus facile à comprendre.\n" +
    "- laisser le moins de mots possibles en laissant les mots clés de la phrase.\n" +
    "- Reformuler uniquement la phrase indiquée.\n" +
    "- Garder exactement le même sens.\n" +
    "- Utiliser des mots simples, concrets et courants.\n" +
    "- Supprimer les formulations abstraites. \n" +
    "- Prendre en compte la segmentation des informations.\n" +
    "- Ne mettre qu'une idée importante par phrase.\n" +
    "- Si la phrase contient plusieurs informations, sépare-les en plusieurs phrases courtes.\n" +
    "- Préférer la tournure active à la tournure passive.\n" +
    "- faire des phrases courtes et simples: sujet/ verbe / complément.\n" +
    "- N'ajoute aucune nouvelle information.\n" +
    "- Bonne reformulation : \"Il faut séparer les informations en petites parties.\"\n";

        GeminiRequestData requestData =
            new GeminiRequestData
            {
                contents = new[]
                {
                    new GeminiContent
                    {
                        parts = new[]
                        {
                            new GeminiPart
                            {
                                text = prompt
                            }
                        }
                    }
                }
            };

        string json = JsonUtility.ToJson(requestData);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request =
               new UnityWebRequest(
                   GeminiUrl,
                   UnityWebRequest.kHttpVerbPOST))
        {
            request.timeout = 30;

            request.uploadHandler =
                new UploadHandlerRaw(jsonBytes);

            request.downloadHandler =
                new DownloadHandlerBuffer();

            request.SetRequestHeader(
                "Content-Type",
                "application/json; charset=utf-8"
            );

            request.SetRequestHeader(
                "x-goog-api-key",
                apiKey
            );

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string details =
                    "Result : " + request.result +
                    "\nHTTP : " + request.responseCode +
                    "\nError : " + request.error +
                    "\nURL : " + request.url +
                    "\nServer : " + request.downloadHandler.text;

                reformulationText.text = details;

                Debug.LogError(details);

                onFinished(false);
                yield break;
            }

            string responseJson =
                request.downloadHandler.text;

            GeminiResponseData response;

            try
            {
                response =
                    JsonUtility.FromJson<GeminiResponseData>(
                        responseJson
                    );
            }
            catch (Exception exception)
            {
                reformulationText.text =
                    "Réponse Gemini illisible.";

                Debug.LogError(
                    "Impossible de lire le JSON Gemini : " +
                    exception.Message +
                    "\nJSON reçu : " +
                    responseJson
                );

                onFinished(false);
                yield break;
            }

            string reformulatedPhrase =
                ExtractGeminiText(response);

            if (string.IsNullOrWhiteSpace(
                reformulatedPhrase))
            {
                reformulationText.text =
                    "Gemini n'a retourné aucun texte.";

                onFinished(false);
                yield break;
            }

            reformulationText.text =
                reformulatedPhrase.Trim();
            sayreformulationTextAloud(reformulationText.text);

            onFinished(true);
        }
    }
    public void RepeatReformulatedTex()
    {
        sayreformulationTextAloud(reformulationText.text);
    }
    public void sayreformulationTextAloud(string text)
    {
       VoiceManager.Instance.SpeakSelectedVoice(text);
    }
  

    private string ExtractGeminiText(
        GeminiResponseData response)
    {
        if (response == null ||
            response.candidates == null ||
            response.candidates.Length == 0)
        {
            return null;
        }

        GeminiCandidate candidate =
            response.candidates[0];

        if (candidate == null ||
            candidate.content == null ||
            candidate.content.parts == null ||
            candidate.content.parts.Length == 0)
        {
            return null;
        }

        return candidate.content.parts[0].text;
    }

    private void OnDestroy()
    {
        applicationIsClosing = true;

        if (dictationRecognizer == null)
        {
            return;
        }

        dictationRecognizer.DictationHypothesis -=
            OnHypothesis;

        dictationRecognizer.DictationResult -=
            OnResult;

        dictationRecognizer.DictationComplete -=
            OnComplete;

        dictationRecognizer.DictationError -=
            OnError;

        if (dictationRecognizer.Status ==
            SpeechSystemStatus.Running)
        {
            dictationRecognizer.Stop();
        }

        dictationRecognizer.Dispose();
        dictationRecognizer = null;
    }

    [Serializable]
    private class GeminiRequestData
    {
        public GeminiContent[] contents;
    }

    [Serializable]
    private class GeminiResponseData
    {
        public GeminiCandidate[] candidates;
    }

    [Serializable]
    private class GeminiCandidate
    {
        public GeminiContent content;
    }

    [Serializable]
    private class GeminiContent
    {
        public GeminiPart[] parts;
    }

    [Serializable]
    private class GeminiPart
    {
        public string text;
    }
}