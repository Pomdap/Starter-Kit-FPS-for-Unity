using System;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class AnimatedSprite : MonoBehaviour
{
    [SerializeField] private Sprite[] m_Frames;
    [SerializeField] private float m_Speed = 30f;
    [SerializeField] private bool m_Loop = false;
    [SerializeField] private int m_Frame = 0;

    private SpriteRenderer m_Renderer;
    private float m_Time;
    private bool m_Playing;

    public event Action OnAnimationFinished;

    public int Frame
    {
        get => m_Frame;
        set
        {
            m_Frame = Mathf.Clamp(value, 0, Mathf.Max(0, m_Frames.Length - 1));
            m_Time = m_Frame;
            ApplyFrame();
        }
    }

    private void Awake()
    {
        m_Renderer = GetComponent<SpriteRenderer>();
        ApplyFrame();
    }

    private void Update()
    {
        if (!m_Playing || m_Frames.Length == 0)
        {
            return;
        }

        m_Time += Time.deltaTime * m_Speed;
        int frame = Mathf.FloorToInt(m_Time);

        if (frame >= m_Frames.Length)
        {
            if (m_Loop)
            {
                m_Time %= m_Frames.Length;
                frame = Mathf.FloorToInt(m_Time);
            }
            else
            {
                m_Frame = m_Frames.Length - 1;
                m_Playing = false;
                ApplyFrame();
                OnAnimationFinished?.Invoke();
                return;
            }
        }

        if (frame != m_Frame)
        {
            m_Frame = frame;
            ApplyFrame();
        }
    }

    public void Play()
    {
        if (!m_Playing && m_Frame >= m_Frames.Length - 1)
        {
            m_Frame = 0;
            m_Time = 0f;
        }

        m_Playing = true;
        ApplyFrame();
    }

    private void ApplyFrame()
    {
        if (m_Renderer == null)
        {
            return;
        }

        m_Renderer.sprite = m_Frames.Length > 0 ? m_Frames[m_Frame] : null;
    }
}
