using UnityEngine;

public class PictogrammeVoiceManager : MonoBehaviour
{
    public void DireSoif()
    {
        VoiceManager.Instance.SpeakSelectedVoice("J'ai soif.");
    }

    public void DireFaim()
    {
        VoiceManager.Instance.SpeakSelectedVoice("J'ai faim.");
    }
    public void DireToilettes()
    {
        VoiceManager.Instance.SpeakSelectedVoice("Je vais aux toilettes.");
    }
    public void DireMal()
    {
        VoiceManager.Instance.SpeakSelectedVoice("J'ai mal.");
    }
}