using UnityEngine;

public class FootstepAudioGain : MonoBehaviour
{
    private volatile float gain = 1f;

    public void SetGain(float value)
    {
        gain = Mathf.Clamp(value, 0f, 5f);
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        float currentGain = gain;

        for (int i = 0; i < data.Length; i++)
        {
            data[i] *= currentGain;
        }
    }
}