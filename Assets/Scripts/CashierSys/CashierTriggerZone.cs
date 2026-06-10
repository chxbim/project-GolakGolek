// Attach ke GameObject isTrigger Cashier 3D Env
// Tidak perlu file terpisah, bisa 1 file CashierTriggerZone.cs
using UnityEngine;

public class CashierTriggerZone : MonoBehaviour
{
    private CashierController _cashierController;

    private void Awake()
    {
        _cashierController = FindFirstObjectByType<CashierController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            _cashierController?.OnPlayerEnterKasir();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            _cashierController?.OnPlayerExitKasir();
    }
}