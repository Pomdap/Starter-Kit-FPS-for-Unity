using UnityEngine;

public class Enemy : MonoBehaviour, IDamageable
{
    [Header("References")]
    [SerializeField] private Transform m_Player;
    [SerializeField] private AnimatedSprite m_MuzzleA;
    [SerializeField] private AnimatedSprite m_MuzzleB;

    [Header("Properties")]
    [SerializeField] private float m_Health = 100f;
    [SerializeField] private float m_ShootInterval = 0.25f;
    [SerializeField] private float m_ShootDistance = 5f;
    [SerializeField] private float m_AttackDamage = 5f;

    [Header("Sounds")]
    [SerializeField] private AudioClip m_HurtAudio;
    [SerializeField] private AudioClip m_DestroyAudio;
    [SerializeField] private AudioClip m_AttackAudio;

    private float m_Time;
    private float m_ShootTimer;
    private Vector3 m_TargetPosition;
    private bool m_Destroyed;

    private void Start()
    {
        m_TargetPosition = transform.position;
        m_ShootTimer = m_ShootInterval;
    }

    private void Update()
    {
        float delta = Time.deltaTime;

        if (m_Player != null)
        {
            transform.LookAt(m_Player.position + new Vector3(0f, 0.5f, 0f), Vector3.up);
        }

        m_TargetPosition.y += Mathf.Cos(m_Time * 5f) * 1f * delta;

        m_Time += delta;

        transform.position = m_TargetPosition;

        m_ShootTimer -= delta;

        if (m_ShootTimer <= 0f)
        {
            m_ShootTimer += m_ShootInterval;
            Shoot();
        }
    }

    public void Damage(float amount)
    {
        Audio.Instance.Play(m_HurtAudio);

        m_Health -= amount;

        if (m_Health <= 0f && !m_Destroyed)
        {
            DestroyEnemy();
        }
    }

    private void DestroyEnemy()
    {
        Audio.Instance.Play(m_DestroyAudio);

        m_Destroyed = true;
        Destroy(gameObject);
    }

    private void Shoot()
    {
        if (!Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, m_ShootDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        if (!hit.collider.TryGetComponent(out IDamageable damageable))
        {
            return;
        }

        PlayMuzzle(m_MuzzleA);
        PlayMuzzle(m_MuzzleB);

        Audio.Instance.Play(m_AttackAudio);

        damageable.Damage(m_AttackDamage);
    }

    private static void PlayMuzzle(AnimatedSprite muzzle)
    {
        if (muzzle == null)
        {
            return;
        }

        muzzle.Frame = 0;
        muzzle.Play();
        muzzle.transform.localEulerAngles = new Vector3(0f, 0f, Random.Range(-45f, 45f));
    }
}
