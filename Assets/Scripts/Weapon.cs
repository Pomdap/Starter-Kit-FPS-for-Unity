using UnityEngine;

[CreateAssetMenu(fileName = "Weapon", menuName = "Starter Kit FPS/Weapon")]
public class Weapon : ScriptableObject
{
    [Header("Model")]
    [SerializeField] private GameObject m_Model;
    [SerializeField] private Vector3 m_Position;
    [SerializeField] private Vector3 m_Rotation;
    [SerializeField] private Vector3 m_MuzzlePosition;

    [Header("Properties")]
    [SerializeField, Range(0.1f, 1f)] private float m_Cooldown = 0.1f;
    [SerializeField, Range(1, 20)] private int m_MaxDistance = 10;
    [SerializeField, Range(0f, 100f)] private float m_Damage = 25f;
    [SerializeField, Range(0f, 5f)] private float m_Spread = 0f;
    [SerializeField, Range(1, 5)] private int m_ShotCount = 1;
    [SerializeField, Range(0, 50)] private int m_Knockback = 20;

    [SerializeField] private Vector2 m_MinKnockback = new(0.001f, 0.001f);
    [SerializeField] private Vector2 m_MaxKnockback = new(0.0025f, 0.002f);

    [Header("Sounds")]
    [SerializeField] private AudioClip m_SoundShoot;

    [Header("Crosshair")]
    [SerializeField] private Sprite m_Crosshair;

    public GameObject Model => m_Model;
    public Vector3 Position => m_Position;
    public Vector3 Rotation => m_Rotation;
    public Vector3 MuzzlePosition => m_MuzzlePosition;
    public float Cooldown => m_Cooldown;
    public int MaxDistance => m_MaxDistance;
    public float Damage => m_Damage;
    public float Spread => m_Spread;
    public int ShotCount => m_ShotCount;
    public int Knockback => m_Knockback;
    public Vector2 MinKnockback => m_MinKnockback;
    public Vector2 MaxKnockback => m_MaxKnockback;
    public AudioClip SoundShoot => m_SoundShoot;
    public Sprite Crosshair => m_Crosshair;
}
