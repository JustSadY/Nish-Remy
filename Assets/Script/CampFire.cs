using UnityEngine;

public class CampFire : MonoBehaviour
{
    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (other.TryGetComponent(out Flare flare))
        {
            flare.ResetLight();
        }
    }
}