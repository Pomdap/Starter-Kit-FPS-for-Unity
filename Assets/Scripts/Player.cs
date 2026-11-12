using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour, IDamageable
{
    private const float k_Gravity = 20f;
    private const float k_WeaponChangeDuration = 0.1f;

    private static readonly Vector3 k_ContainerOffset = new(1.2f, -1.1f, 2.75f);

    [Header("References")]
    [SerializeField] private Transform m_Camera;
    [SerializeField] private Transform m_Container;
    [SerializeField] private AnimatedSprite m_Muzzle;
    [SerializeField] private AnimatedSprite m_ImpactPrefab;
    [SerializeField] private Image m_Crosshair;
    [SerializeField] private AudioSource m_FootstepsAudio;
    [SerializeField] private AudioClip[] m_JumpAudio;
    [SerializeField] private AudioClip m_LandAudio;
    [SerializeField] private AudioClip m_WeaponChangeAudio;

    [Header("Properties")]
    [SerializeField] private float m_MovementSpeed = 5f;
    [SerializeField, Range(0, 100)] private int m_NumberOfJumps = 2;
    [SerializeField] private float m_JumpStrength = 8f;
    [SerializeField] private LayerMask m_ShootMask = Physics.DefaultRaycastLayers;

    [Header("Weapons")]
    [SerializeField] private Weapon[] m_Weapons;

    [Header("Controls")]
    [SerializeField] private float m_MouseSensitivity = 700f;
    [SerializeField] private float m_GamepadSensitivity = 0.075f;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference m_MoveAction;
    [SerializeField] private InputActionReference m_LookAction;
    [SerializeField] private InputActionReference m_LookGamepadAction;
    [SerializeField] private InputActionReference m_JumpAction;
    [SerializeField] private InputActionReference m_ShootAction;
    [SerializeField] private InputActionReference m_WeaponToggleAction;
    [SerializeField] private InputActionReference m_MouseCaptureAction;
    [SerializeField] private InputActionReference m_MouseCaptureExitAction;

    private CharacterController m_Controller;
    private CollisionFlags m_CollisionFlags;

    private Weapon m_Weapon;
    private int m_WeaponIndex;
    private Coroutine m_WeaponChangeRoutine;

    private bool m_MouseCaptured = true;

    private Vector3 m_Velocity;
    private Vector3 m_MovementVelocity;
    private Vector2 m_RotationTarget;
    private float m_CameraPitch;
    private float m_Yaw;

    private int m_Health = 100;
    private float m_Gravity;

    private bool m_PreviouslyFloored;

    private int m_JumpsRemaining;

    private float m_NextShotTime;

    public event Action<int> OnHealthUpdated;

    private void Awake()
    {
        m_Controller = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        m_MoveAction?.action.Enable();
        m_LookAction?.action.Enable();
        m_LookGamepadAction?.action.Enable();
        m_JumpAction?.action.Enable();
        m_ShootAction?.action.Enable();
        m_WeaponToggleAction?.action.Enable();
        m_MouseCaptureAction?.action.Enable();
        m_MouseCaptureExitAction?.action.Enable();
    }

    private void OnDisable()
    {
        m_MoveAction?.action.Disable();
        m_LookAction?.action.Disable();
        m_LookGamepadAction?.action.Disable();
        m_JumpAction?.action.Disable();
        m_ShootAction?.action.Disable();
        m_WeaponToggleAction?.action.Disable();
        m_MouseCaptureAction?.action.Disable();
        m_MouseCaptureExitAction?.action.Disable();
    }

    private void Start()
    {
        CaptureMouse(true);

        m_Yaw = transform.eulerAngles.y;
        m_RotationTarget = new Vector2(m_CameraPitch, m_Yaw);

        m_Weapon = m_Weapons[m_WeaponIndex];
        InitiateChangeWeapon(m_WeaponIndex);
    }

    private void Update()
    {
        float delta = Time.deltaTime;

        HandleControls(delta);
        HandleGravity(delta);

        m_MovementVelocity = transform.rotation * m_MovementVelocity;

        Vector3 appliedVelocity = Vector3.Lerp(m_Velocity, m_MovementVelocity, delta * 10f);
        appliedVelocity.y = -m_Gravity;

        m_Velocity = appliedVelocity;
        m_CollisionFlags = m_Controller.Move(appliedVelocity * delta);

        bool isGrounded = m_Controller.isGrounded;

        Vector3 localVelocity = Quaternion.Inverse(transform.rotation) * appliedVelocity;
        m_Container.localPosition = Vector3.Lerp(m_Container.localPosition, k_ContainerOffset - (localVelocity / 30f), delta * 10f);

        Vector3 velocity = m_Controller.velocity;

        if (m_FootstepsAudio != null)
        {
            if (isGrounded && (Mathf.Abs(velocity.x) > 1f || Mathf.Abs(velocity.z) > 1f))
            {
                m_FootstepsAudio.UnPause();
            }
            else
            {
                m_FootstepsAudio.Pause();
            }
        }

        Vector3 cameraPosition = m_Camera.localPosition;
        cameraPosition.y = Mathf.Lerp(cameraPosition.y, 0f, delta * 5f);

        if (isGrounded && m_Gravity > 1f && !m_PreviouslyFloored)
        {
            Audio.Instance.Play(m_LandAudio);
            cameraPosition.y = -0.1f;
        }

        m_Camera.localPosition = cameraPosition;

        m_PreviouslyFloored = isGrounded;

        if (transform.position.y < -10f)
        {
            ReloadScene();
        }
    }

    public void Damage(float amount)
    {
        m_Health -= Mathf.RoundToInt(amount);
        OnHealthUpdated?.Invoke(m_Health);

        if (m_Health < 0)
        {
            ReloadScene();
        }
    }

    private void HandleControls(float delta)
    {
        if (m_MouseCaptureAction != null && m_MouseCaptureAction.action.WasPressedThisFrame())
        {
            CaptureMouse(true);
        }

        if (m_MouseCaptureExitAction != null && m_MouseCaptureExitAction.action.WasPressedThisFrame())
        {
            CaptureMouse(false);
        }

        Vector2 mouse = m_LookAction != null
            ? m_LookAction.action.ReadValue<Vector2>()
            : Vector2.zero;

        if (m_MouseCaptured && mouse != Vector2.zero)
        {
            HandleRotation(mouse.x, mouse.y, false, delta);
        }

        Vector2 input = m_MoveAction != null
            ? m_MoveAction.action.ReadValue<Vector2>()
            : Vector2.zero;

        m_MovementVelocity = new Vector3(input.x, 0f, input.y).normalized * m_MovementSpeed;

        Vector2 rotationInput = m_LookGamepadAction != null
            ? m_LookGamepadAction.action.ReadValue<Vector2>()
            : Vector2.zero;

        if (rotationInput != Vector2.zero)
        {
            HandleRotation(rotationInput.x, rotationInput.y, true, delta);
        }

        ActionShoot();

        if (m_JumpAction != null && m_JumpAction.action.WasPressedThisFrame())
        {
            if (m_JumpsRemaining > 0)
            {
                ActionJump();
            }
        }

        ActionWeaponToggle();
    }

    private void HandleRotation(float xRot, float yRot, bool isController, float delta)
    {
        if (isController)
        {
            Vector2 stick = Vector2.ClampMagnitude(new Vector2(xRot, yRot), 1f);
            float amount = m_GamepadSensitivity * Mathf.Rad2Deg * delta * 60f;

            m_RotationTarget.x -= stick.y * amount;
            m_RotationTarget.y += stick.x * amount;
            m_RotationTarget.x = Mathf.Clamp(m_RotationTarget.x, -90f, 90f);

            m_CameraPitch = Mathf.LerpAngle(m_CameraPitch, m_RotationTarget.x, delta * 25f);
            m_Yaw = Mathf.LerpAngle(m_Yaw, m_RotationTarget.y, delta * 25f);
        }
        else
        {
            m_RotationTarget.x -= yRot / m_MouseSensitivity * Mathf.Rad2Deg;
            m_RotationTarget.y += xRot / m_MouseSensitivity * Mathf.Rad2Deg;
            m_RotationTarget.x = Mathf.Clamp(m_RotationTarget.x, -90f, 90f);

            m_CameraPitch = m_RotationTarget.x;
            m_Yaw = m_RotationTarget.y;
        }

        ApplyRotation();
    }

    private void ApplyRotation()
    {
        m_Camera.localRotation = Quaternion.Euler(m_CameraPitch, 0f, 0f);
        transform.rotation = Quaternion.Euler(0f, m_Yaw, 0f);
    }

    private void HandleGravity(float delta)
    {
        m_Gravity += k_Gravity * delta;

        if (m_Gravity < 0f && (m_CollisionFlags & CollisionFlags.Above) != 0)
        {
            m_Gravity = 0f;
        }

        if (m_Gravity > 0f && m_Controller.isGrounded)
        {
            m_JumpsRemaining = m_NumberOfJumps;
            m_Gravity = k_Gravity * delta;
        }
    }

    private void ActionJump()
    {
        Audio.Instance.Play(m_JumpAudio);

        m_Gravity = -m_JumpStrength;
        m_JumpsRemaining--;
    }

    private void ActionShoot()
    {
        if (m_ShootAction == null || !m_ShootAction.action.IsPressed())
        {
            return;
        }

        if (Time.time < m_NextShotTime)
        {
            return;
        }

        Audio.Instance.Play(m_Weapon.SoundShoot);

        m_Muzzle.Play();

        Transform muzzle = m_Muzzle.transform;
        muzzle.localEulerAngles = new Vector3(0f, 0f, Random.Range(-45f, 45f));
        muzzle.localScale = Vector3.one * Random.Range(0.40f, 0.75f);
        muzzle.localPosition = m_Container.localPosition - m_Weapon.MuzzlePosition;

        m_NextShotTime = Time.time + m_Weapon.Cooldown;

        for (int n = 0; n < m_Weapon.ShotCount; n++)
        {
            Vector3 target = new Vector3(
                Random.Range(-m_Weapon.Spread, m_Weapon.Spread),
                Random.Range(-m_Weapon.Spread, m_Weapon.Spread),
                m_Weapon.MaxDistance);

            Vector3 direction = m_Camera.TransformDirection(target);

            if (!Physics.Raycast(m_Camera.position, direction.normalized, out RaycastHit hit, direction.magnitude, m_ShootMask, QueryTriggerInteraction.Collide))
            {
                continue;
            }

            if (hit.collider.TryGetComponent(out IDamageable damageable))
            {
                damageable.Damage(m_Weapon.Damage);
            }

            Vector3 impactPosition = hit.point + (hit.normal / 10f);
            AnimatedSprite impact = Instantiate(m_ImpactPrefab, impactPosition, Quaternion.LookRotation(impactPosition - m_Camera.position));
            impact.Play();
        }

        Vector2 knockback = RandomVec2(m_Weapon.MinKnockback, m_Weapon.MaxKnockback) * Mathf.Rad2Deg;

        m_Container.localPosition -= new Vector3(0f, 0f, 0.25f);
        m_CameraPitch -= knockback.x;
        m_Yaw += knockback.y;
        m_RotationTarget.x -= knockback.x;
        m_RotationTarget.y += knockback.y;
        ApplyRotation();

        m_MovementVelocity += new Vector3(0f, 0f, -m_Weapon.Knockback);
    }

    private void ActionWeaponToggle()
    {
        if (m_WeaponToggleAction != null && m_WeaponToggleAction.action.WasPressedThisFrame())
        {
            m_WeaponIndex = (m_WeaponIndex + 1) % m_Weapons.Length;
            InitiateChangeWeapon(m_WeaponIndex);

            Audio.Instance.Play(m_WeaponChangeAudio);
        }
    }

    private void InitiateChangeWeapon(int index)
    {
        m_WeaponIndex = index;

        if (m_WeaponChangeRoutine != null)
        {
            StopCoroutine(m_WeaponChangeRoutine);
        }

        m_WeaponChangeRoutine = StartCoroutine(ChangeWeaponRoutine());
    }

    private IEnumerator ChangeWeaponRoutine()
    {
        Vector3 start = m_Container.localPosition;
        Vector3 target = k_ContainerOffset - new Vector3(0f, 1f, 0f);

        for (float time = 0f; time < k_WeaponChangeDuration; time += Time.deltaTime)
        {
            m_Container.localPosition = Vector3.Lerp(start, target, time / k_WeaponChangeDuration);
            yield return null;
        }

        m_Container.localPosition = target;

        ChangeWeapon();

        m_WeaponChangeRoutine = null;
    }

    private void ChangeWeapon()
    {
        m_Weapon = m_Weapons[m_WeaponIndex];

        for (int i = m_Container.childCount - 1; i >= 0; i--)
        {
            Destroy(m_Container.GetChild(i).gameObject);
        }

        if (m_Crosshair != null)
        {
            m_Crosshair.sprite = m_Weapon.Crosshair;
        }

        if (m_Weapon.Model == null)
        {
            Debug.LogWarning($"Weapon '{m_Weapon.name}' has no model assigned.", m_Weapon);
            return;
        }

        GameObject weaponModel = Instantiate(m_Weapon.Model, m_Container);

        weaponModel.transform.localPosition = m_Weapon.Position;
        weaponModel.transform.localEulerAngles = m_Weapon.Rotation;

        int layer = m_Container.gameObject.layer;

        foreach (Transform child in weaponModel.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.layer = layer;
        }

        foreach (Renderer meshRenderer in weaponModel.GetComponentsInChildren<Renderer>(true))
        {
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        }
    }

    private void CaptureMouse(bool captured)
    {
        m_MouseCaptured = captured;
        Cursor.lockState = captured ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !captured;
    }

    private static void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private static Vector2 RandomVec2(Vector2 min, Vector2 max)
    {
        float sign = Random.Range(0, 2) == 0 ? -1f : 1f;
        return new Vector2(Random.Range(min.x, max.x), Random.Range(min.y, max.y) * sign);
    }
}
