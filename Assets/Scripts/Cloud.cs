using UnityEngine;

public class Cloud : MonoBehaviour
{
    private float m_Time = 0f;
    private float m_RandomVelocity;
    private float m_RandomTime;

    private Vector3 m_StartPosition;

    private void Start()
    {
        m_StartPosition = transform.position;

        m_RandomVelocity = Random.Range(0.1f, 2.0f);
        m_RandomTime = Random.Range(0.1f, 2.0f);
    }

    private void Update()
    {
        float yOffset = Mathf.Cos(m_Time * m_RandomTime) * m_RandomVelocity;
        transform.position = m_StartPosition + new Vector3(0f, yOffset, 0f);

        m_Time += Time.deltaTime;
    }
}
