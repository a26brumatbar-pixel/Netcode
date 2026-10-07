using System.Globalization;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    public float speed = 5f;
    public float jumpForce = 5f;
    public float groundCheckDistance = 1f;

    private Vector2 inputVector;
    private bool jumpPress;

    private readonly NetworkVariable<Color> playerColor = new NetworkVariable<Color>(
        Color.white,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>(
        "",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private Rigidbody rb;
    private AudioSource audioSource;

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    private void Update()
    {
        if (!IsOwner) return; //Solo jugador local controla su personaje

        float h = 0f;
        float v = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) h -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) h += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) v -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) v += 1f;
        }

        bool jump = false;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.spaceKey.isPressed)
            {
                jump = true;
            }

            if (Keyboard.current.cKey.wasPressedThisFrame)
            {
                ChangeColorServerRpc();
            }
        }

        Vector2 input = new Vector2(h, v).normalized;

        SubmitInputServerRpc(input, jump);
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        Vector3 moveDirection = new Vector3(inputVector.x, 0f, inputVector.y);

        Vector3 targetVelocity = moveDirection * speed;
        rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);

        if (jumpPress)
        {
            if (IsGrounded())
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                PlayJumpSoundClientRpc();
            }

            jumpPress = false;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        playerColor.OnValueChanged += OnPlayerColorChanged;
        playerName.OnValueChanged += OnPlayerNameChanged;

        if (IsServer)
        {
            rb.isKinematic = false;

            playerName.Value = $"Jugador {OwnerClientId + 1}";
            playerColor.Value = Random.ColorHSV();
        }
        else
        {
            rb.isKinematic = true;
        }
    }

    private void OnPlayerColorChanged(Color previousColor, Color newColor)
    {
        Renderer renderer = GetComponentInChildren<Renderer>();

        if (renderer != null)
        {
            renderer.material.color = newColor;
        }
    }

    private void OnPlayerNameChanged(FixedString32Bytes previousName, FixedString32Bytes newName)
    {
        Debug.Log($"El nombre del jugador ha cambiado a: {newName}");
    }

    [ServerRpc]
    private void SubmitInputServerRpc(Vector2 input, bool jump)
    {
        inputVector = input;
        jumpPress = jump;
    }

    [ServerRpc]
    private void ChangeColorServerRpc()
    {
        playerColor.Value = Random.ColorHSV();
    }

    [ClientRpc]
    private void PlayJumpSoundClientRpc()
    {
        audioSource.Play();
    }

    public override void OnNetworkDespawn()
    {
        playerColor.OnValueChanged -= OnPlayerColorChanged;
        playerName.OnValueChanged -= OnPlayerNameChanged;

        base.OnNetworkDespawn();
    }
}
