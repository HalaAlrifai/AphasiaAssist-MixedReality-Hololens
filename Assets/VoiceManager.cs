using UnityEngine;

#if WINDOWS_UWP
using Windows.Media.SpeechSynthesis;
using Windows.Media.Playback;
using Windows.Media.Core;
using System;
#endif

public class VoiceManager : MonoBehaviour
{
    public string phrase =
        " Bonjour. Écoutez cette prononciation et indiquez celle que vous préférez.";
    public static VoiceManager Instance;
    private static bool isSpeaking = false;
    public string selectedVoice = "Hortense"; // Default voice
    public string selectedRate = "medium"; // Default rate
    public string selectedPitch = "medium"; // Default pitch
    public string selectedVolume = "100%"; // Default volume


#if WINDOWS_UWP
    private SpeechSynthesizer synthesizer;
    private MediaPlayer mediaPlayer;
#endif

    void Awake()
    {
        Instance = this;
#if WINDOWS_UWP
        synthesizer = new SpeechSynthesizer();
        mediaPlayer = new MediaPlayer();

        mediaPlayer.MediaEnded += (sender, args) =>
        {
            isSpeaking = false;
        };
#endif
    }

    // Voix féminine 1 : Hortense normale
    public void PlayVoice1()
    {
       

#if WINDOWS_UWP
        SetVoice("Hortense");
#endif
        selectedVoice = "Hortense";
        selectedRate = "medium";
        selectedPitch = "medium";
        selectedVolume = "100%";

        SpeakSelectedVoice(" Bonjour. Écoutez cette prononciation et indiquez celle que vous préférez.");
    }

    // Voix féminine 2 : Hortense lente
    public void PlayVoice2()
    {
      

#if WINDOWS_UWP
        SetVoice("Hortense");
#endif

        selectedVoice = "Hortense";
        selectedRate = "slow";
        selectedPitch = "medium";
        selectedVolume = "100%";

        SpeakSelectedVoice(" Bonjour. Écoutez cette prononciation et indiquez celle que vous préférez.."); ;
    }

    // Voix masculine 1 : Paul normal
    public void PlayVoice3()
    {
        

#if WINDOWS_UWP
        SetVoice("Paul");
#endif

        selectedVoice = "Paul";
        selectedRate = "medium";
        selectedPitch = "medium";
        selectedVolume = "100%";

        SpeakSelectedVoice(" Bonjour. Écoutez cette prononciation et indiquez celle que vous préférez.");
    }

    // Voix masculine 2 : Paul lent
    public void PlayVoice4()
    {
        

#if WINDOWS_UWP
        SetVoice("Paul");
#endif

        selectedVoice = "Paul";
        selectedRate = "slow";
        selectedPitch = "medium";
        selectedVolume = "100%";

        SpeakSelectedVoice(" Bonjour. Écoutez cette prononciation et indiquez celle que vous préférez.");
    }

    public void SpeakSelectedVoice(string phrase)
    {
        if (isSpeaking) return;

#if WINDOWS_UWP
        SetVoice(selectedVoice);
#endif

        Speak(phrase, selectedRate, selectedPitch, selectedVolume);
    }

#if WINDOWS_UWP
    private void SetVoice(string voiceName)
    {
        foreach (var voice in SpeechSynthesizer.AllVoices)
        {
            if (voice.DisplayName.Contains(voiceName))
            {
                synthesizer.Voice = voice;
                break;
            }
        }
    }

    private async void Speak(string phrase, string rate, string pitch, string volume)
    {
        isSpeaking = true;

        string ssml =
            "<speak version='1.0' xml:lang='fr-FR'>" +
            "<prosody rate='" + rate + "' pitch='" + pitch + "' volume='" + volume + "'>" +
            phrase +
            "</prosody></speak>";

        var stream = await synthesizer.SynthesizeSsmlToStreamAsync(ssml);
        mediaPlayer.Source = MediaSource.CreateFromStream(stream, stream.ContentType);
        mediaPlayer.Play();
    }
#else
    private void Speak(string phrase, string rate, string pitch, string volume)
    {
        Debug.Log($"Phrase: {phrase} | Voice: {selectedVoice} | rate={rate}");
    }
#endif
}