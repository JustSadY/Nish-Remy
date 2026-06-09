using System;
using UnityEngine;

public class CommingSoon : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Player")) return;
        if (GameManager.Instance != null) GameManager.Instance.ComingSoon();
    }
}