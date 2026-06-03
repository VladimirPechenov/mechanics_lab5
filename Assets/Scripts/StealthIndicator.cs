using UnityEngine;
using UnityEngine.UI;

public class StealthIndicator : MonoBehaviour
{
    [SerializeField] private Image indicatorImage;
    [SerializeField] private Text label;
    [SerializeField] private Color safeColor = new(0.16f, 0.75f, 0.28f, 0.95f);
    [SerializeField] private Color suspiciousColor = new(1f, 0.78f, 0.12f, 0.95f);
    [SerializeField] private Color detectedColor = new(0.9f, 0.1f, 0.08f, 0.95f);

    private void Awake()
    {
        if (indicatorImage == null)
        {
            indicatorImage = GetComponent<Image>();
        }
    }

    private void Update()
    {
        var state = GetHighestEnemyState();
        switch (state)
        {
            case EnemyStateMachine.State.Alert:
                ApplyState(detectedColor, "ALERT");
                break;
            case EnemyStateMachine.State.Suspicion:
                ApplyState(suspiciousColor, "SUSPICION");
                break;
            default:
                ApplyState(safeColor, "SAFE");
                break;
        }
    }

    private EnemyStateMachine.State GetHighestEnemyState()
    {
        var result = EnemyStateMachine.State.Patrol;
        var enemies = FindObjectsByType<EnemyStateMachine>(FindObjectsSortMode.None);

        foreach (var enemy in enemies)
        {
            if (enemy.CurrentState == EnemyStateMachine.State.Alert)
            {
                return EnemyStateMachine.State.Alert;
            }

            if (enemy.CurrentState == EnemyStateMachine.State.Suspicion)
            {
                result = EnemyStateMachine.State.Suspicion;
            }
        }

        return result;
    }

    private void ApplyState(Color color, string text)
    {
        if (indicatorImage != null)
        {
            indicatorImage.color = color;
        }

        if (label != null)
        {
            label.text = text;
        }
    }
}
