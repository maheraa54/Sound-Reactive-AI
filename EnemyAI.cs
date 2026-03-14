using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("Sound Reaction Settings")]
    public float soundDetectionRange = 15f;
    public float minLoudnessToReact = 0.3f;
    public float investigationDuration = 5f;
    public float runSpeed = 6f;

    [Header("Visualization")]
    public GameObject soundIndicatorPrefab;

    private NavMeshAgent agent;
    private Vector3 lastSoundPosition;
    private float investigationTimer;
    private bool isInvestigating;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = 0f; // لا يتحرك في البداية
    }

    void Update()
    {
        if (isInvestigating)
        {
            investigationTimer -= Time.deltaTime;

            if (investigationTimer <= 0 || ReachedSoundPosition())
            {
                StopInvestigation();
            }
        }
    }

    public void OnLoudSoundDetected(SoundData soundData)
    {
        if (ShouldReactToSound(soundData))
        {
            // هذا السطر يطبع شدة الصوت التي استجاب لها العدو
            Debug.Log("Enemy alerted by sound! Loudness: " + soundData.loudness);
            StartInvestigation(soundData.position);
        }
    }

    bool ShouldReactToSound(SoundData soundData)
    {
        float distanceToSound = Vector3.Distance(transform.position, soundData.position);
        return soundData.loudness >= minLoudnessToReact &&
               distanceToSound <= soundDetectionRange;
    }

    void StartInvestigation(Vector3 soundPosition)
    {
        lastSoundPosition = soundPosition;
        isInvestigating = true;
        investigationTimer = investigationDuration;
        agent.speed = runSpeed;
        agent.SetDestination(lastSoundPosition);

        // مؤشر مرئي للصوت
        if (soundIndicatorPrefab != null)
        {
            Instantiate(soundIndicatorPrefab, soundPosition, Quaternion.identity);
        }

        Debug.Log("Enemy alerted by sound!");
    }

    void StopInvestigation()
    {
        isInvestigating = false;
        agent.speed = 0f; // يتوقف عن الحركة
    }

    bool ReachedSoundPosition()
    {
        return !agent.pathPending &&
               agent.remainingDistance <= agent.stoppingDistance;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, soundDetectionRange);

        if (isInvestigating)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, lastSoundPosition);
        }
    }
}