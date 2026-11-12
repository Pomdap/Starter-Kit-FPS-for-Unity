using System.Collections.Generic;
using UnityEngine;

public class Audio : MonoBehaviour
{
    private const int k_NumPlayers = 12;
    private const float k_Volume = 0.316f;

    private static Audio s_Instance;

    private readonly List<AudioSource> m_Available = new();
    private readonly List<AudioSource> m_AllSources = new();
    private readonly Queue<AudioClip> m_Queue = new();

    public static Audio Instance
    {
        get
        {
            if (s_Instance == null)
            {
                GameObject gameObject = new GameObject("Audio");
                s_Instance = gameObject.AddComponent<Audio>();
            }

            return s_Instance;
        }
    }

    private void Awake()
    {
        if (s_Instance != null && s_Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        s_Instance = this;
        DontDestroyOnLoad(gameObject);

        if (m_AllSources.Count == 0)
        {
            InitializePool();
        }
    }

    private void Update()
    {
        for (int i = 0; i < m_AllSources.Count; i++)
        {
            AudioSource source = m_AllSources[i];
            if (!source.isPlaying && !m_Available.Contains(source) && source.clip != null)
            {
                source.clip = null;
                m_Available.Add(source);
            }
        }

        while (m_Queue.Count > 0 && m_Available.Count > 0)
        {
            AudioSource source = m_Available[0];
            m_Available.RemoveAt(0);

            source.clip = m_Queue.Dequeue();
            source.pitch = Random.Range(0.9f, 1.1f);
            source.Play();
        }
    }

    private void InitializePool()
    {
        for (int i = 0; i < k_NumPlayers; i++)
        {
            GameObject child = new GameObject($"AudioSource_{i}");
            child.transform.SetParent(transform);

            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.volume = k_Volume;

            m_Available.Add(source);
            m_AllSources.Add(source);
        }
    }

    public void Play(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        m_Queue.Enqueue(clip);
    }

    public void Play(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
        {
            return;
        }

        Play(clips[Random.Range(0, clips.Length)]);
    }
}
