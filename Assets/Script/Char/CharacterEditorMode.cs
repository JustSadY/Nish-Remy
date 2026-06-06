using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.InputSystem;

public class CharacterEditorMode : MonoBehaviour
{
    [SerializeField] private GameObject roadPrefab;
    [SerializeField] private NavMeshSurface navMeshSurface;
    [SerializeField, Range(4, 32)] private int samplesPerSegment = 12;
    [SerializeField, Range(0f, 1f)] private float simplifyTolerance = 0.15f;

    [Header("Editor Cursor Settings")]
    [SerializeField] private Texture2D editorCursorTexture; 
    [SerializeField] private Vector2 cursorHotspot = Vector2.zero; 

    private GameObject _currentRoad;
    private InputSystem_Actions _actions;
    private CharacterPathDrawer _drawer;
    private bool _isEditorActive;

    private void Awake()
    {
        _actions = new InputSystem_Actions();
        _drawer = GetComponent<CharacterPathDrawer>();
    }

    private void OnEnable() => _actions?.Enable();
    
    private void OnDisable()
    {
        _actions?.Disable();
        ResetCursor();
    }

    // Bu fonksiyon modlar arası geçişte imleci kesin olarak değiştirir
    public void SetEditorModeActive(bool isActive)
    {
        _isEditorActive = isActive;

        if (_isEditorActive && editorCursorTexture != null)
        {
            Cursor.SetCursor(editorCursorTexture, cursorHotspot, CursorMode.Auto);
        }
        else
        {
            ResetCursor();
        }
    }

    private void ResetCursor()
    {
        // İmleci sistemin varsayılan (Default) haline döndürür
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    public void HandleEditorTick()
    {
        if (!_isEditorActive) return;

        if (_actions.Player.Left.WasPressedThisFrame()) _drawer.ClearDrawing();
        if (_actions.Player.Left.IsPressed()) _drawer.HandleDrawing(); 
        if (_actions.Player.Left.WasReleasedThisFrame() && _drawer.GetDrawnPoints().Count > 1)
        {
            CreatePermanentRoad(_drawer.GetDrawnPoints());
            _drawer.ClearDrawing();
        }
    }

    private void CreatePermanentRoad(List<Vector3> points)
    {
        if (_currentRoad != null)
        {
            Destroy(_currentRoad);
            _currentRoad = null;
            StartCoroutine(RebuildNavMeshThenSpawn(points));
            return;
        }
        SpawnRoad(points);
    }

    private void SpawnRoad(List<Vector3> points)
    {
        List<Vector3> splinePoints = CatmullRomSpline.Build(points, samplesPerSegment, simplifyTolerance);
        _currentRoad = Instantiate(roadPrefab);
        _currentRoad.transform.position = new Vector3(0f, -0.1f, 0f);

        if (!_currentRoad.TryGetComponent(out NavMeshModifier modifier))
            modifier = _currentRoad.AddComponent<NavMeshModifier>();

        modifier.overrideArea = true;
        modifier.area = 0;

        if (_currentRoad.TryGetComponent(out RoadMeshBuilder meshBuilder))
            meshBuilder.Build(splinePoints);

        StartCoroutine(BuildNavMeshAsync());
    }

    private IEnumerator RebuildNavMeshThenSpawn(List<Vector3> points)
    {
        yield return null;
        if (navMeshSurface != null) yield return navMeshSurface.UpdateNavMesh(navMeshSurface.navMeshData);
        SpawnRoad(points);
    }

    private IEnumerator BuildNavMeshAsync()
    {
        if (navMeshSurface == null) yield break;
        yield return null;
        yield return navMeshSurface.UpdateNavMesh(navMeshSurface.navMeshData);
    }
}