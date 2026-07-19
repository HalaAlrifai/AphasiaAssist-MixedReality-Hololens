using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

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
    private string conversationHistory = "";

    private bool geminiRequestInProgress = false;
    private bool applicationIsClosing = false;
    private bool backClicked = false;
    
    private string[] fullText =
{
    " Salut !Petite info pour Alain et puis Nicolas aussi pour que tu sois informé. C’est les démarches pour essayer de reprendre la conduite d’un véhicule. L’orthophoniste d'Alain, elle m’a dit qu’il y avait le centre Lay St Christophe qui propose un programme qui s’appelle Conduite IRR qui permet de faire une réhabilitation à la conduite avant de passer par le fameux médecin régulateur et donc je les ai appelés. Ils m’ont dit que pour pouvoir bénéficier de ce programme-là, il fallait simplement que le médecin traitant fasse un courrier au centre Lay St Christophe. Pour pouvoir avoir un rendez-vous chez eux. Il faut que le médecin explique les difficultés pour la conduite, enfin donner toutes les infos sur l’état de de santé. Donc voilà on en rediscutera dimanche pour voir comment mettre ça en place",
    "Je viens de vous envoyer le lien que l’orthophoniste m’avait envoyé qui parle de ce fameux programme",
    "Depuis 2023, le centre a un simulateur de conduite. C’est super tu verras, tu pourras t’entrainer comme sur un jeu vidéo ! Apparemment le centre pourrait t’accompagner dans ton projet de reprise de la conduite. En fait ils sont là pour évaluer tes capacités et identifier les éventuels aménagements à faire sur le véhicule. Pour ton cas je ne pense pas que tu aies besoin d’aménagement. L’avantage aussi c’est que ça permet de t ’entraîner sans stress avec des professionnels bienveillants face à tes difficultés…Voilà. De ce que j’ai lu, ils ont aussi un véhicule réel, une jeep je crois, pour vraiment évaluer et former en situation réelle les personnes. Après je ne sais pas du tout s’il y a des délais d’attente longs, j’imagine que oui. On verra bien. C’est toi qui décideras de toute façon. \r\n\r\nJ’ai trouvé une vidéo plus récente qui date de l’année dernière et qui donne encore plus d’explications. Je te mettrai le lien après. Le centre voit entre 150 et 200 patients par an. Il est là pour faire une évaluation en amont de la visite chez le médecin régulateur qui donne l’autorisation de la reprise de la conduite. Il y a plusieurs personnes qui peuvent intervenir : le médecin, le neuropsy, l’orthoptiste et l’ergothérapeute. Ça a l’air vraiment bien, j’espère qu’ils ont de la place. Tiens je te donne le lien de la vidéo ",
    " Et Alain est-ce que tu as rendez-vous dans pas longtemps avec le docteur Séverin ? ",
    "Ok si tu veux bien je viendrai avec toi je t’emmènerai et comme ça on pourra discuter avec elle de ça. C’est à quelle heure ?"
};
    private string[] reformulatedSentences =
    {
        "Alain veut conduire à nouveau. L'orthophoniste conseille le centre Lay Saint-Christophe. Ce centre propose un programme de rééducation. Votre médecin doit envoyer un courrier au centre. Ce courrier explique la santé d'Alain. Nous en parlerons dimanche pour organiser cela.",
        "Alain veut reprendre la conduite. L'orthophoniste conseille le centre Lay Saint-Christophe. Ce centre propose une rééducation à la conduite. Il faut un courrier du médecin traitant pour obtenir un rendez-vous. Le médecin doit expliquer les difficultés de santé. On en parlera dimanche.",
        "L'orthophoniste a un programme. Je vous ai envoyé le lien par mail.",
        "L'orthophoniste a conseillé un programme. Je vous ai envoyé le lien par mail.",
        "Le centre propose un simulateur de conduite comme un jeu vidéo. Il évalue vos capacités. Il vérifie si vous avez besoin d'aménagements sur la voiture. Il y a des professionnels pour vous aider sans stress. Ils ont aussi une vraie voiture pour la pratique. Vous déciderez si vous voulez y aller. Le centre aide 200 patients par an. Des spécialistes évaluent votre conduite. Cela prépare votre visite chez le médecin pour conduire à nouveau. Je vous envoie une vidéo avec des explications.",
        "Le centre a un simulateur de conduite. C'est comme un jeu vidéo pour s'entraîner. Ils évaluent vos capacités pour conduire. Ils vérifient si la voiture a besoin de changements. Tu n'auras sûrement pas besoin de changements. Tu peux t'entraîner sans stress avec des professionnels. Le centre a aussi une vraie Jeep pour les essais. Il y a peut-être un temps d'attente. C'est toi qui décideras. Ils voient 200 patients par an. Ils préparent ta visite chez le médecin pour conduire. Le médecin, le neuropsy, l'orthoptiste et l'ergothérapeute travaillent ici. Regarde cette vidéo pour en savoir plus.",
        "Alain, tu as un rendez-vous bientôt avec le docteur Séverin ?",
        "Alain, as-tu bientôt rendez-vous avec le docteur Séverin ?",
        "Je viens avec toi.\r\nOn va lui parler.\r\nÀ quelle heure ?",
        "Je t'accompagne.\r\nNous parlerons avec elle.\r\nÀ quelle heure ?"
    };

    private int currentSentence = 0;
    private int n = 0;

    

    public void OnBackButtonClicked()
    {
        
    }

    public void OnReformulationButtonClicked()
{
    if (currentSentence == fullText.Length)
    {
        currentSentence = 0;
    }

    int reformulationIndex = currentSentence * 2 + n;

    if (reformulationIndex >= reformulatedSentences.Length)
    {
        reformulationText.text = "Aucune reformulation.";
        return;
    }

    reformulationText.text =
        reformulatedSentences[reformulationIndex];

    sayreformulationTextAloud(reformulationText.text);

    n++;

    if (n == 2)
    {
        n = 0;
        currentSentence++;
    }
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
    "Tu es un assistant de communication pour faciliter la compréhension pour les personnes aphasiques quand leur interlocuteur parle .\n\n" +
    "Contexte de la conversation :\n" +
    context +
    "\n\nPhrase à reformuler :\n" +
    phraseToReformulate +
    "\n\nConsignes obligatoires :\n" +
    "- n'ajouter pas une phrase de debut ou de fin du type \"Voici la reformulation\".\n" +
    "- quand il clique Plusieurs fois sur le bouton reformulation, il faut que la reformulation change à chaque fois.\n" +
    "- a chauque fois qu'il clique a nouveau sur le bouton reformulation pour la meme phrase, il faut que la reformulation devienne plus courte et plus simple que la précédente.\n" +
    "-commencer la reformulation par la phrase reformulée sans texte supplémentaire.\n" +
    "- donner qu'une seule reformulation par phrase.\n" +
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
      // VoiceManager.Instance.SpeakSelectedVoice(text);
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