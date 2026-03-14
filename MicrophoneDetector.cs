using UnityEngine;

public class MicrophoneDetector : MonoBehaviour
{
    [Header("Microphone Settings")]
    [Tooltip("Base microphone sensitivity")]
    [Range(0.5f, 10f)] public float inputGain = 3f;

    [Tooltip("Dynamic boost that auto-adjusts to quiet sounds")]
    [Range(1f, 20f)] public float maxAutoBoost = 8f;

    [Tooltip("Minimum volume threshold to trigger detection")]
    [Range(0f, 0.5f)] public float voiceThreshold = 0.03f;

    [Tooltip("Noise cancellation strength")]
    [Range(0f, 0.5f)] public float noiseReduction = 0.15f;

    [Header("Timing Settings")]
    [Tooltip("How often to check microphone input (seconds)")]
    [Range(0.02f, 0.3f)] public float checkInterval = 0.1f;

    [Tooltip("Number of audio samples to analyze")]
    public int sampleWindow = 1024;

    [Header("Enemy Detection")]
    public EnemyAI enemyAI;
    public float maxDetectionRange = 20f;

    [Header("Advanced")]
    [Tooltip("Smoothing effect for volume changes")]
    [Range(0.1f, 0.95f)] public float smoothingFactor = 0.8f;

    [Tooltip("Enable automatic input normalization")]
    public bool autoNormalize = true;

    // Private variables
    private AudioClip microphoneInput;
    private bool isInitialized;
    private float[] samples;
    private float lastCheckTime;
    private float smoothLoudness;
    private string selectedDevice;
    private float dynamicBoost = 1f;
    private float peakVolume = 0f;

    void Start()
    {
        samples = new float[sampleWindow];
        InitializeMicrophone();
    }

    void InitializeMicrophone()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphone devices found!");
            enabled = false;
            return;
        }

        selectedDevice = Microphone.devices[0];
        microphoneInput = Microphone.Start(selectedDevice, true, 10, 44100);
        isInitialized = true;

        Debug.Log($"Microphone initialized: {selectedDevice}\n" +
                 $"Base Gain: {inputGain} | Max Boost: {maxAutoBoost}\n" +
                 $"Threshold: {voiceThreshold} | Noise Reduction: {noiseReduction}");
    }

    void Update()
    {
        if (!isInitialized || Time.time - lastCheckTime < checkInterval)
            return;

        float rawLoudness = GetFilteredLoudness();
        smoothLoudness = Mathf.Lerp(smoothLoudness, rawLoudness, 1f - smoothingFactor);

        // Debug visualization
        peakVolume = Mathf.Max(peakVolume, smoothLoudness);
        if (Time.time - lastCheckTime > 1f)
        {
            peakVolume = 0f;
        }

        if (smoothLoudness > voiceThreshold)
        {
            AlertEnemy(smoothLoudness);
        }

        lastCheckTime = Time.time;
    }

    float GetFilteredLoudness()
    {
        if (!isInitialized) return 0f;

        int position = Microphone.GetPosition(selectedDevice);
        if (position < sampleWindow) return 0f;

        microphoneInput.GetData(samples, position - sampleWindow);

        float peak = 0f;
        float sum = 0f;

        // Analyze samples
        for (int i = 0; i < sampleWindow; i++)
        {
            float sample = samples[i];
            peak = Mathf.Max(peak, Mathf.Abs(sample));
            sum += Mathf.Abs(sample);
        }

        // Dynamic boost calculation
        float average = sum / sampleWindow;
        float boostFactor = Mathf.Clamp(1f / (peak + 0.0001f), 1f, maxAutoBoost);
        dynamicBoost = autoNormalize ? boostFactor : 1f;

        // Apply amplification and noise reduction
        float amplifiedPeak = peak * inputGain * dynamicBoost;
        float adjustedNoise = noiseReduction * (autoNormalize ? peak : 1f);
        float finalVolume = Mathf.Clamp01(amplifiedPeak - adjustedNoise);

        Debug.Log($"Input: {peak:F4} | Boost: x{dynamicBoost:F1} | Output: {finalVolume:F4}");

        return finalVolume;
    }

    void AlertEnemy(float loudness)
    {
        if (enemyAI != null && Vector3.Distance(transform.position, enemyAI.transform.position) <= maxDetectionRange)
        {
            SoundData data = new SoundData(transform.position, loudness);
            enemyAI.OnLoudSoundDetected(data);
        }
    }

    void OnDestroy()
    {
        if (isInitialized && Microphone.IsRecording(selectedDevice))
        {
            Microphone.End(selectedDevice);
        }
    }

    // Visual debug in Game view
    void OnGUI()
    {
        GUI.color = Color.green;
        GUI.Label(new Rect(10, 10, 300, 20), $"Current Volume: {smoothLoudness:F4}");
        GUI.Label(new Rect(10, 30, 300, 20), $"Peak Volume: {peakVolume:F4}");
        GUI.Label(new Rect(10, 50, 300, 20), $"Active Boost: x{dynamicBoost:F1}");
        GUI.Label(new Rect(10, 70, 300, 20), $"Threshold: {voiceThreshold:F4}");
    }
}