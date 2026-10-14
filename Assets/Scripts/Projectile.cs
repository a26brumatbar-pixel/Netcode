using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : NetworkBehaviour
{
    private Rigidbody rb;
    private bool hasHit;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        rb.isKinematic = !IsServer;

        if (IsServer)
        {
            Debug.Log("Proyectil creado en el servidor.");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        HandleHit(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        HandleHit(other);
    }

    private void HandleHit(Collider other)
    {
        if (hasHit) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();

        if (player != null && player.OwnerClientId == OwnerClientId)
        {
            return;
        }

        hasHit = true;

        if (player != null)
        {
            player.SetRandomColorFromServer();
        }

        if (NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }
}
