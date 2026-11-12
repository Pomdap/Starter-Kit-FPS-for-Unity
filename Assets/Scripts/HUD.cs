using TMPro;
using UnityEngine;

public class HUD : MonoBehaviour
{
    [SerializeField]
    private Player m_Player;

    [SerializeField]
    private TextMeshProUGUI m_HealthText;

    private void OnEnable()
    {
        m_Player.OnHealthUpdated += OnHealthUpdated;
    }

    private void OnDisable()
    {
        m_Player.OnHealthUpdated -= OnHealthUpdated;
    }

    private void OnHealthUpdated(int health)
    {
        m_HealthText.text = $"{health}%";
    }
}
