using UnityEngine;

public class BeatManager : MonoBehaviour
{
    // Audio playback
    AudioSource audioData;
    public int beatIndex = 0;

    // JSON utility
    public TextAsset jsonFile;
    public Beat[] beats;
    public GameObject CubePrefab;

    void Start()
    {
        // Play audio
        audioData = GetComponent<AudioSource>();
        audioData.Play();

        // Parse JSON
        beats = JsonUtility.FromJson<BeatMap>(jsonFile.text).Beats;
    }

    void Update()
    {
        if (beats.Length > 0 && beatIndex < beats.Length) {
            float currentTime = audioData.time;
            Beat currentBeat = beats[beatIndex];

            float triggerTime = currentBeat.Timestamp - Constants.BEAT_SPWAN_INVERSE_DELAY;
            if (currentTime >= triggerTime) {
                SpawnBeat(currentBeat.QuadrantIndex);
                beatIndex++;
            }
        }
    }

    void SpawnBeat(int index)
    {
        float startIndex = index % 2;
        switch (startIndex)
        {
            case 0:
                Instantiate(CubePrefab, new Vector3(0f, 0f, 10f), Quaternion.identity);
                break;
            case 1:
                Instantiate(CubePrefab, new Vector3(4f, 0f, 10f), Quaternion.identity);
                break;
        }
    }
}

// Models for JSON to deserialize to
[System.Serializable]
public class BeatMap
{
    public Beat[] Beats;
}

[System.Serializable]
public class Beat
{
    public int QuadrantIndex;
    public float Timestamp;
    public bool Color; // red = true (left quadrant), blue = false (right quadrant); 8 quads total
}
