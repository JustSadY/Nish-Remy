using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { private set; get; }
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject commingSoonPanel;

    private InputSystem_Actions _actions;
    private bool _isGameOver = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this.gameObject);

        _actions = new InputSystem_Actions();
    }

    private void Update()
    {
        if (!_isGameOver && _actions.Player.Esc.WasPressedThisFrame())
        {
            FlipFlopMenuPanel();
        }
    }

    private void OnEnable()
    {
        _actions?.Enable();
    }

    private void OnDisable()
    {
        _actions?.Disable();
    }

    public void FlipFlopMenuPanel()
    {
        menuPanel.SetActive(!menuPanel.activeSelf);
    }

    public void EndGame()
    {
        if (_isGameOver) return;

        _isGameOver = true;
        menuPanel.SetActive(true);

        CharacterControl[] players = FindObjectsByType<CharacterControl>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            player.StopCharacter();
        }
    }

    public void ComingSoon()
    {
        commingSoonPanel.SetActive(true);
        Invoke(nameof(LoadFirstScene), 3f);
    }

    private void LoadFirstScene()
    {
        SceneManager.LoadScene(0);
    }
}