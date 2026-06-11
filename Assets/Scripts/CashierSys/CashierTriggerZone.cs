using UnityEngine;

/// <summary>
/// Attach ke GameObject isTrigger di 3D Env kasir (bukan CashierUI).
///
/// Alasan dipisah dari CashierController:
///   OnTriggerEnter/Exit hanya fire di GameObject yang punya Collider isTrigger.
///   CashierController ada di CashierUI (Canvas GO) — tidak bisa receive trigger.
///   Script ini jadi bridge: physics trigger → CashierController di Canvas.
///
/// Setup:
///   1. Attach script ini ke GameObject isTrigger Cashier 3D Env
///   2. Pastikan Collider di GameObject ini: Is Trigger = true
///   3. Pastikan Rigidbody ada di Player (atau di GO ini dengan IsKinematic = true)
///   4. Tag player HARUS "Player"
/// </summary>
public class CashierTriggerZone : MonoBehaviour
{
    private CashierController _cashierController;

    private void Awake()
    {
        _cashierController = FindFirstObjectByType<CashierController>();

        if (_cashierController == null)
            Debug.LogError("[CashierTrigger] CashierController tidak ditemukan di scene! " +
                           "Pastikan CashierController sudah attach ke CashierUI.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log("[CashierTrigger] Player MASUK zona kasir.");
        _cashierController?.OnPlayerEnterKasir();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log("[CashierTrigger] Player KELUAR zona kasir.");
        _cashierController?.OnPlayerExitKasir();
    }
}