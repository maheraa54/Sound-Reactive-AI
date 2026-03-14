using UnityEngine;

[System.Serializable]
public class SoundData
{
    public Vector3 position;
    public float loudness;

    public SoundData(Vector3 pos, float loud)
    {
        position = pos;
        loudness = loud;
    }
}