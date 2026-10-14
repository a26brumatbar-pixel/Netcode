using Unity.Netcode;
using UnityEngine;

public class SpinningObstacle : NetworkBehaviour
{
    public float rotationSpeed = 90f;

    private void Update()
    {
        if (!IsServer) return;

        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }
}