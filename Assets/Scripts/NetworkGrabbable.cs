using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class NetworkGrabbable : NetworkBehaviour
{
    public NetworkVariable<bool> IsGrabbed = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        rb.isKinematic = !IsServer;
    }

    
    public bool TryGrab(NetworkObject playerNetworkObject, Transform holdPoint)
    {
        Debug.Log($"[AGARRE 1] TryGrab ejecutado. IsServer={IsServer}");

        if (!IsServer || IsGrabbed.Value)
        {
            Debug.LogWarning($"[AGARRE 2] Cancelado. IsServer={IsServer}, IsGrabbed={IsGrabbed.Value}");
            return false;
        }

        if (playerNetworkObject == null || !playerNetworkObject.IsSpawned)
        {
            Debug.LogError("[AGARRE 3] El NetworkObject del jugador es nulo o no está spawned.");
            return false;
        }

        if (holdPoint == null)
        {
            Debug.LogError("[AGARRE 4] HoldPoint es null.");
            return false;
        }

        Debug.Log($"[AGARRE 5] Jugador: {playerNetworkObject.name}. HoldPoint: {holdPoint.position}");

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        Debug.Log("[AGARRE 6] Rigidbody preparado. Intentando cambiar el padre.");

        
        Debug.Log(
            $"[ANTES] Cubo={NetworkObject.name}, " +
            $"Cubo spawned={NetworkObject.IsSpawned}, " +
            $"Jugador={playerNetworkObject.name}, " +
            $"Jugador spawned={playerNetworkObject.IsSpawned}"
        );

        bool parentSet = NetworkObject.TrySetParent(
            playerNetworkObject,
            false
        );

        Debug.Log($"[PARENTADO] TrySetParent devuelve: {parentSet}");

        Debug.Log(
            $"[DESPUÉS] Padre={(transform.parent != null ? transform.parent.name : "NULL")}, " + $"Padre correcto={transform.parent == playerNetworkObject.transform}"
        );

        if (!parentSet)
        {
            rb.isKinematic = false;
            Debug.LogError("[PARENTADO] Netcode rechazó el cambio de padre.");
            return false;
        }

        transform.localPosition = new Vector3(0f, 0f, 1f);
        transform.localRotation = Quaternion.identity;

        Debug.Log($"[POSICIÓN] Local={transform.localPosition}, " + $"Mundo={transform.position}"
        );

        IsGrabbed.Value = true;
        return true;
    }

    public bool Throw(Vector3 direction, float force)
    {
        if (!IsServer || !IsGrabbed.Value)
        {
            Debug.LogWarning("Throw cancelado: no es el servidor o el cubo no está agarrado.");
            return false;
        }

        if (!NetworkObject.TryRemoveParent())
        {
            Debug.LogError("No se ha podido quitar el padre del cubo.");
            return false;
        }

        IsGrabbed.Value = false;

        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.AddForce(direction.normalized * force, ForceMode.Impulse);

        Debug.Log("Cubo soltado correctamente.");
        return true;
    }
}
