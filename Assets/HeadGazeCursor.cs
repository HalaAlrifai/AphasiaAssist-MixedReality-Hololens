using TMPro;
using UnityEngine;
using UnityEngine.Windows.Speech;

public class HeadGazeCursor : MonoBehaviour
{
    public Transform cursor;
    public TMP_Text debugText;
    public float maxDistance = 20f;

    void Update()
    {
        PhraseRecognitionSystem.Restart();
        Ray ray = new Ray(
            Camera.main.transform.position,
            Camera.main.transform.forward
        );

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, ~0))
        {
            cursor.position = hit.point;
            cursor.forward = hit.normal;

            if (debugText != null)
                debugText.text = "Head hit: " + hit.collider.name;
        }
        else
        {
            cursor.position = Camera.main.transform.position +
                              Camera.main.transform.forward * 2f;

            cursor.forward = Camera.main.transform.forward;

            if (debugText != null)
                debugText.text = "Head hit: nothing";
        }
    }
}