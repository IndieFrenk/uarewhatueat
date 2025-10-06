using UnityEngine;

/// <summary>
/// Sistema di controllo First Person completo per Unity 6000 HDRP
/// Gestisce movimento, visuale, corsa, salto e interazione con la fisica
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    [Header("Movimento")]
    [Tooltip("Velocità di camminata normale")]
    [SerializeField] private float walkSpeed = 5f;
    
    [Tooltip("Velocità durante la corsa")]
    [SerializeField] private float runSpeed = 8f;
    
    [Tooltip("Velocità durante il crouch")]
    [SerializeField] private float crouchSpeed = 2.5f;
    
    [Tooltip("Tempo per raggiungere la velocità massima")]
    [SerializeField] private float accelerationTime = 0.1f;
    
    [Tooltip("Tempo per fermarsi completamente")]
    [SerializeField] private float decelerationTime = 0.1f;

    [Header("Salto e Gravità")]
    [Tooltip("Altezza del salto")]
    [SerializeField] private float jumpHeight = 2f;
    
    [Tooltip("Forza di gravità personalizzata")]
    [SerializeField] private float gravity = -20f;
    
    [Tooltip("Numero di salti consecutivi possibili (1 = singolo, 2 = doppio, ecc.)")]
    [SerializeField] private int maxJumps = 1;

    [Header("Controllo Visuale")]
    [Tooltip("Transform della camera (assegnare manualmente)")]
    [SerializeField] private Transform cameraTransform;
    
    [Tooltip("Sensibilità mouse orizzontale")]
    [SerializeField] private float mouseSensitivityX = 2f;
    
    [Tooltip("Sensibilità mouse verticale")]
    [SerializeField] private float mouseSensitivityY = 2f;
    
    [Tooltip("Limite angolo di rotazione verso l'alto")]
    [SerializeField] private float maxLookUpAngle = 80f;
    
    [Tooltip("Limite angolo di rotazione verso il basso")]
    [SerializeField] private float maxLookDownAngle = 80f;
    
    [Tooltip("Smoothing della rotazione camera")]
    [SerializeField] private float lookSmoothing = 10f;

    [Header("Crouch")]
    [Tooltip("Abilita il sistema di crouch")]
    [SerializeField] private bool enableCrouch = true;
    
    [Tooltip("Altezza del controller in piedi")]
    [SerializeField] private float standingHeight = 2f;
    
    [Tooltip("Altezza del controller accovacciato")]
    [SerializeField] private float crouchHeight = 1f;
    
    [Tooltip("Velocità transizione crouch")]
    [SerializeField] private float crouchSpeed_transition = 10f;

    [Header("Opzioni Avanzate")]
    [Tooltip("Movimento aereo permesso (% della velocità normale)")]
    [SerializeField] [Range(0f, 1f)] private float airControl = 0.3f;
    
    [Tooltip("Blocca il cursore all'avvio")]
    [SerializeField] private bool lockCursorOnStart = true;
    
    [Tooltip("Layer da considerare come terreno")]
    [SerializeField] private LayerMask groundMask = 1;
    
    [Tooltip("Distanza check terreno")]
    [SerializeField] private float groundCheckDistance = 0.3f;

    [Header("Effetti Visivi (Opzionale)")]
    [Tooltip("Head bob abilitato durante la camminata")]
    [SerializeField] private bool enableHeadBob = true;
    
    [Tooltip("Intensità head bob")]
    [SerializeField] private float headBobAmount = 0.05f;
    
    [Tooltip("Frequenza head bob")]
    [SerializeField] private float headBobFrequency = 10f;

    // Variabili private
    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 currentVelocity;
    private float xRotation = 0f;
    private bool isGrounded;
    private int currentJumps;
    private bool isCrouching;
    private float currentHeight;
    private Vector3 cameraStartPosition;
    private float headBobTimer;
    
    // Smoothing
    private Vector2 currentMouseDelta;
    private Vector2 currentMouseVelocity;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        currentHeight = standingHeight;
        controller.height = currentHeight;
        
        if (cameraTransform != null)
        {
            cameraStartPosition = cameraTransform.localPosition;
        }
        
        if (lockCursorOnStart)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        HandleGroundCheck();
        HandleMouseLook();
        HandleMovement();
        HandleJump();
        HandleCrouch();
        HandleHeadBob();
        
        // Sblocca cursore con ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        // Blocca cursore con click
        if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void HandleGroundCheck()
    {
        // Check se il personaggio è a terra
        Vector3 spherePosition = transform.position - new Vector3(0, controller.height / 2 - controller.radius + 0.1f, 0);
        isGrounded = Physics.CheckSphere(spherePosition, controller.radius, groundMask, QueryTriggerInteraction.Ignore);
        
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Piccola forza per mantenere il personaggio attaccato al terreno
            currentJumps = 0;
        }
    }

    private void HandleMouseLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        // Input mouse con smoothing
        Vector2 targetMouseDelta = new Vector2(
            Input.GetAxis("Mouse X") * mouseSensitivityX,
            Input.GetAxis("Mouse Y") * mouseSensitivityY
        );
        
        currentMouseDelta = Vector2.SmoothDamp(
            currentMouseDelta, 
            targetMouseDelta, 
            ref currentMouseVelocity, 
            1f / lookSmoothing
        );

        // Rotazione orizzontale del corpo
        transform.Rotate(Vector3.up * currentMouseDelta.x);

        // Rotazione verticale della camera
        xRotation -= currentMouseDelta.y;
        xRotation = Mathf.Clamp(xRotation, -maxLookUpAngle, maxLookDownAngle);

        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }

    private void HandleMovement()
    {
        // Input movimento
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        Vector3 move = transform.right * horizontal + transform.forward * vertical;
        move.Normalize();

        // Determina velocità target
        float targetSpeed = walkSpeed;
        if (Input.GetKey(KeyCode.LeftShift) && !isCrouching)
            targetSpeed = runSpeed;
        else if (isCrouching)
            targetSpeed = crouchSpeed;

        // Applica controllo aereo ridotto
        if (!isGrounded)
            targetSpeed *= airControl;

        // Accelerazione smooth
        Vector3 targetVelocity = move * targetSpeed;
        float acceleration = move.magnitude > 0 ? accelerationTime : decelerationTime;
        
        currentVelocity = Vector3.Lerp(
            currentVelocity, 
            targetVelocity, 
            Time.deltaTime / acceleration
        );

        // Applica movimento
        controller.Move(currentVelocity * Time.deltaTime);

        // Applica gravità
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && (isGrounded || currentJumps < maxJumps))
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            currentJumps++;
        }
    }

    private void HandleCrouch()
    {
        if (!enableCrouch) return;

        if (Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.LeftControl))
        {
            isCrouching = true;
        }
        else if (isCrouching)
        {
            // Check se c'è spazio per alzarsi
            if (CanStandUp())
            {
                isCrouching = false;
            }
        }

        // Smooth transition altezza
        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        currentHeight = Mathf.Lerp(currentHeight, targetHeight, Time.deltaTime * crouchSpeed_transition);
        controller.height = currentHeight;
        
        // Aggiusta centro del controller
        controller.center = new Vector3(0, currentHeight / 2, 0);
    }

    private bool CanStandUp()
    {
        // Raycast per verificare se c'è spazio sopra la testa
        float checkDistance = standingHeight - crouchHeight + 0.2f;
        Vector3 start = transform.position + Vector3.up * crouchHeight;
        
        return !Physics.Raycast(start, Vector3.up, checkDistance, groundMask, QueryTriggerInteraction.Ignore);
    }

    private void HandleHeadBob()
    {
        if (!enableHeadBob || cameraTransform == null || !isGrounded) 
        {
            // Reset camera position
            if (cameraTransform != null)
            {
                cameraTransform.localPosition = Vector3.Lerp(
                    cameraTransform.localPosition, 
                    cameraStartPosition, 
                    Time.deltaTime * 5f
                );
            }
            headBobTimer = 0;
            return;
        }

        // Head bob solo se ci si muove
        if (currentVelocity.magnitude > 0.1f)
        {
            headBobTimer += Time.deltaTime * headBobFrequency * currentVelocity.magnitude;
            
            float bobOffset = Mathf.Sin(headBobTimer) * headBobAmount;
            Vector3 targetPosition = cameraStartPosition + new Vector3(0, bobOffset, 0);
            
            cameraTransform.localPosition = targetPosition;
        }
    }

    // Metodi pubblici per modificare valori runtime
    public void SetMouseSensitivity(float sensitivity)
    {
        mouseSensitivityX = sensitivity;
        mouseSensitivityY = sensitivity;
    }

    public void SetWalkSpeed(float speed)
    {
        walkSpeed = speed;
    }

    public void SetRunSpeed(float speed)
    {
        runSpeed = speed;
    }

    // Gizmos per debug
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        
        // Visualizza ground check sphere
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 spherePosition = transform.position - new Vector3(0, controller.height / 2 - controller.radius + 0.1f, 0);
        Gizmos.DrawWireSphere(spherePosition, controller.radius);
    }
}