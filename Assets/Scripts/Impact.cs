using UnityEngine;

[RequireComponent(typeof(AnimatedSprite))]
public class Impact : MonoBehaviour
{
    private AnimatedSprite m_AnimatedSprite;

    private void Awake()
    {
        m_AnimatedSprite = GetComponent<AnimatedSprite>();
    }

    private void OnEnable()
    {
        m_AnimatedSprite.OnAnimationFinished += OnAnimationFinished;
    }

    private void OnDisable()
    {
        m_AnimatedSprite.OnAnimationFinished -= OnAnimationFinished;
    }

    private void OnAnimationFinished()
    {
        Destroy(gameObject);
    }
}
