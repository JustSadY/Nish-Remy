using UnityEngine;

[RequireComponent(typeof(Animator))]
public class CharacterAnimationFX : MonoBehaviour
{
    private static readonly int AnimSpeed = Animator.StringToHash("Speed");

    [Header("Footsteps & FX")]
    [SerializeField] private AudioSource audioFootsteps;
    [SerializeField] private Sprite footprintSprite;
    [SerializeField] private Transform leftFootTransform;
    [SerializeField] private Transform rightFootTransform;
    [SerializeField] private float footprintDuration = 5f;

    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public void UpdateSpeedAnimation(float speed)
    {
        if (_animator != null) _animator.SetFloat(AnimSpeed, speed);
    }

    // Animasyon Event tetikler
    private void OnFootstep(int isLeftFootInt)
    {
        if (audioFootsteps != null) audioFootsteps.Play();
        SpawnFootprint(isLeftFootInt == 1);
    }

    private void SpawnFootprint(bool isLeft)
    {
        if (footprintSprite == null) return;
        Transform activeFoot = isLeft ? leftFootTransform : rightFootTransform;
        Vector3 spawnPos = activeFoot != null ? activeFoot.position : transform.position;
        spawnPos.y += 0.01f;

        GameObject footprintObj = new GameObject("Footprint");
        SpriteRenderer sr = footprintObj.AddComponent<SpriteRenderer>();
        sr.sprite = footprintSprite;
        footprintObj.transform.position = spawnPos;
        footprintObj.transform.rotation = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);
        Destroy(footprintObj, footprintDuration);
    }
}