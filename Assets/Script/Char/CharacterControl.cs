using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(CharacterPathDrawer))]
[RequireComponent(typeof(CharacterMovement))]
[RequireComponent(typeof(CharacterAnimationFX))]
[RequireComponent(typeof(CharacterEditorMode))]
public class CharacterControl : MonoBehaviour
{
    [Header("Mode Settings")]
    [SerializeField] private bool isEditorMode;
    [SerializeField] private float cameraRotationSpeed = 200f;

    private CharacterPathDrawer _drawer;
    private CharacterMovement _movement;
    private CharacterAnimationFX _fx;
    private CharacterEditorMode _editorMode;
    private CinemachineOrbitalFollow _orbital;
    private InputSystem_Actions _actions;
    private bool _isStopped;

    private void Awake()
    {
        _actions = new InputSystem_Actions();
        _drawer = GetComponent<CharacterPathDrawer>();
        _movement = GetComponent<CharacterMovement>();
        _fx = GetComponent<CharacterAnimationFX>();
        _editorMode = GetComponent<CharacterEditorMode>();
        _orbital = FindAnyObjectByType<CinemachineOrbitalFollow>();
    }

    private void Start()
    {
        _drawer.OnPathCommitted += _movement.SetNewPath;

        // --- KRİTİK GÜNCELLEME ---
        // Oyun ilk açıldığında imleci editör modunun başlangıç durumuna göre ayarla
        _editorMode.SetEditorModeActive(isEditorMode);
    }

    private void OnDestroy()
    {
        _drawer.OnPathCommitted -= _movement.SetNewPath;
    }

    private void OnEnable() => _actions?.Enable();
    private void OnDisable() => _actions?.Disable();

    private void Update()
    {
        if (_isStopped) return;

        // Mod Değiştirme (E Tuşu)
        if (_actions.Player.E.WasPressedThisFrame())
        {
            isEditorMode = !isEditorMode;
            
            _movement.ClearPath();
            _drawer.ClearDrawing();
            _fx.UpdateSpeedAnimation(0f);

            // Mod değiştiğinde imleci güncelle
            _editorMode.SetEditorModeActive(isEditorMode);
        }

        HandleCameraRotation();

        if (isEditorMode)
        {
            _editorMode.HandleEditorTick();
        }
        else
        {
            _movement.HandleMovementTick();
            _drawer.HandleDrawing();
            _fx.UpdateSpeedAnimation(_movement.CurrentSpeed);
        }
    }

    public void StopCharacter()
    {
        _isStopped = true;
        _movement.StopMoving();
        _drawer.ClearDrawing();
        _fx.UpdateSpeedAnimation(0f);
        
        // Karakter durdurulduğunda imleci de normale çek
        _editorMode.SetEditorModeActive(false);
        
        enabled = false;
    }

    private void HandleCameraRotation()
    {
        if (_orbital == null || !_actions.Player.Right.IsPressed()) return;
        float delta = _actions.Player.Look.ReadValue<Vector2>().x * cameraRotationSpeed * Time.deltaTime;
        _orbital.HorizontalAxis.Value += delta;
    }
}