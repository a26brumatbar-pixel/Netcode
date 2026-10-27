using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class NetworkGrabber : NetworkBehaviour
{
    public float grabRange = 2f;
    public float throwForce = 8f;
    private Transform holdPoint;

    public GameObject grabPrompt;

    private NetworkGrabbable nearbyObject;
    private NetworkGrabbable heldObject;

    private readonly NetworkVariable<NetworkObjectReference>
        heldObjectReference = new NetworkVariable<NetworkObjectReference>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
    
    private readonly NetworkVariable<bool> isHoldingObject =
        new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );


    private void Update()
    {
        if (!IsOwner) return;

        UpdateNearbyObject();

        bool isHolding = isHoldingObject.Value;

        if (grabPrompt != null)
        {
            grabPrompt.SetActive(nearbyObject != null && !isHolding);
        }

        if (Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame)
        {
            return;
        }

                if (isHoldingObject.Value)
        {
            ThrowObjectServerRpc();
        }
        else if (nearbyObject != null && nearbyObject.NetworkObject.IsSpawned)
        {
            GrabObjectServerRpc(new NetworkObjectReference(nearbyObject.NetworkObject));
        }
    }

    
    private void UpdateNearbyObject()
    {
        nearbyObject = null;

        if (isHoldingObject.Value)
            return;

        Collider[] hits = Physics.OverlapSphere(transform.position, grabRange);

        float closestDistance = float.MaxValue;

        foreach (Collider hit in hits)
        {
            NetworkGrabbable grabbable = hit.GetComponentInParent<NetworkGrabbable>();

            if (grabbable == null || !grabbable.IsSpawned || grabbable.IsGrabbed.Value)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, grabbable.transform.position);

            if (distance > grabRange)
                continue;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                nearbyObject = grabbable;
            }
        }
    }


    private void CreateHoldPoint()
    {
        GameObject holdObject = new GameObject("HoldPoint");

        holdPoint = holdObject.transform;

        holdPoint.SetParent(transform, false);

        holdPoint.localPosition = new Vector3(0f, 0f, 1f);
        holdPoint.localRotation = Quaternion.identity;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        CreateHoldPoint();
    }

    [ServerRpc]
    private void GrabObjectServerRpc(NetworkObjectReference reference)
    {
        Debug.Log("Servidor: petición de agarrar recibida.");

        if (holdPoint == null) return;

        if (!reference.TryGet(out NetworkObject objectNetworkObject))
            return;

        NetworkGrabbable grabbable = objectNetworkObject.GetComponent<NetworkGrabbable>();

        if (grabbable == null || grabbable.IsGrabbed.Value)
            return;

        if (Vector3.Distance(transform.position, grabbable.transform.position) > grabRange)
        {
            return;
        }
        
        if (grabbable.TryGrab(NetworkObject, holdPoint))
        {
            heldObject = grabbable;
            heldObjectReference.Value =
                new NetworkObjectReference(objectNetworkObject);

            isHoldingObject.Value = true;

            Debug.Log("[AGARRE] El servidor ha guardado el objeto agarrado.");
        }
    }

    [ServerRpc]
    private void ThrowObjectServerRpc()
    {
        Debug.Log("[LANZAMIENTO] Petición recibida por el servidor.");

        if (heldObject == null)
        {
            Debug.LogError("[LANZAMIENTO] heldObject es null en el servidor.");
            return;
        }

        Vector3 direction = transform.forward;

        if (!heldObject.Throw(direction, throwForce))
        {
            Debug.LogError("[LANZAMIENTO] No se ha podido soltar el cubo.");
            return;
        }

        heldObject = null;
        heldObjectReference.Value = default;
        isHoldingObject.Value = false;

        Debug.Log("[LANZAMIENTO] Objeto soltado y referencia reiniciada.");
    }


    private void OnDisable()
    {
        if (grabPrompt != null)
            grabPrompt.SetActive(false);
    }
}
