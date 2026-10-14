
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Bumper : NetworkBehaviour
{
    public float pushForce = 10f;
    public float upwardForce = 1.5f;

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        PlayerController player = collision.gameObject.GetComponentInParent<PlayerController>();

        if (player == null) return;

        Rigidbody playerRb = player.GetComponent<Rigidbody>();

        if (playerRb == null) return;

        Vector3 direction = player.transform.position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            direction = collision.GetContact(0).normal;
            direction.y = 0f;
        }

        direction.Normalize();

        Vector3 velocity = playerRb.linearVelocity;
        velocity.x = 0f;
        velocity.z = 0f;
        playerRb.linearVelocity = velocity;

        Vector3 impulse = direction * pushForce + Vector3.up * upwardForce;

        playerRb.AddForce(impulse, ForceMode.Impulse);
    }
}
