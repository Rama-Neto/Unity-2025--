using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    //variables de movimiento, privadas
    float moveSpeed = 5f;
    float rotationSpeed = 720f;

    //variables de vida y estamina, expuestas en Inspector
    [Header("Player Stats")]
    public int playerLife = 100;
    public int playerLifeMax = 0;
    public int playerStamina = 10;

    //variables de disparo, expuestas en Inspector
    [Header("Gun Settings")]
    public float gunRange = 20f;
    public int gunDamage = 20;
    public float fireRate = 0.5f;
    public int gunMagazineMax = 15;

    private float nextFireTime = 0f;
    private Rigidbody rb;
    [SerializeField] private Camera mainCamera;
    private int ignoreLayerMask;

    //Variables para crouching
    private CapsuleCollider capsule;
    private float originalHeight;
    private Vector3 originalCenter;

    private float crouchHeight;
    private Vector3 crouchCenter;

    private bool isCrouched = false;
    private Vector3 originalScale;
    private Vector3 crouchScale;

    //variables UI
    public Slider healthBar;
    public TMP_Text healthText;
    public TMP_Text ammoCounter;

    void Start()
    {
        playerLifeMax = playerLife;
        
        rb = GetComponent<Rigidbody>();
        mainCamera = Camera.main;
        if (mainCamera == null)
            Debug.LogWarning("PlayerController: Camera.main is null. Make sure a camera is tagged MainCamera.");

        //se genera máscara para que raycast de disparo ignore a la layer del player, para que no choque contra su propio collider
        int playerLayer = gameObject.layer;
        ignoreLayerMask = ~(1 << playerLayer); 
        Debug.Log($"PlayerController started. Player layer: {LayerMask.LayerToName(playerLayer)} (index {playerLayer}).");

        //Inicialización de valores para crouching
        capsule = GetComponent<CapsuleCollider>();

        originalHeight = capsule.height;
        originalCenter = capsule.center;

        crouchHeight = originalHeight * 0.5f;
        crouchCenter = new Vector3(
            originalCenter.x,
            originalCenter.y - (originalHeight - crouchHeight) / 2f,
            originalCenter.z
        );

        originalScale = transform.localScale;
        crouchScale = new Vector3(
            originalScale.x,
            originalScale.y * 0.5f,   // half-height visually
            originalScale.z
        );


    }

    void UpdateMovement()
    {
        // --- Read movement input ---
        float horizontal = 0f;
        float vertical = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) vertical += 1f;
            if (Keyboard.current.sKey.isPressed) vertical -= 1f;
            if (Keyboard.current.dKey.isPressed) horizontal += 1f;
            if (Keyboard.current.aKey.isPressed) horizontal -= 1f;
        }

        bool hasInput = (horizontal != 0f || vertical != 0f);

        Vector3 moveDir = Vector3.zero;
        rb.angularVelocity = Vector3.zero;


        if (hasInput)
        {
            Vector3 camForward = mainCamera.transform.forward;
            Vector3 camRight = mainCamera.transform.right;

            camForward.y = 0f;
            camRight.y = 0f;

            camForward.Normalize();
            camRight.Normalize();

            moveDir = camForward * vertical + camRight * horizontal;

            // normalize only when non-zero
            if (moveDir.sqrMagnitude > 0f)
                moveDir.Normalize();
        }

        // Apply movement
        rb.linearVelocity = moveDir * moveSpeed;

        // Apply rotation only when there is real input
        /*if (hasInput && moveDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            rb.MoveRotation(Quaternion.RotateTowards(
                rb.rotation,
                targetRot,
                rotationSpeed * Time.deltaTime
            ));
        }*/
    }


    void Update()
    {
        UpdateMovement();

        //Mostrar y actualizar valores de UI
        healthText.text = playerLife + " / " + playerLifeMax;
        healthBar.value = (float)playerLife / (float)playerLifeMax;
        ammoCounter.text = gunMagazineMax.ToString();

        //Logica de disparo con click derecho del mouse
        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            if (gunMagazineMax > 0)
            {
                Debug.Log("Input detected: left mouse button pressed.");
                ShootFromCenter();
                nextFireTime = Time.time + fireRate;
                gunMagazineMax -= 1;
            }
            else if (gunMagazineMax <= 0)
            {
                nextFireTime = Time.time + fireRate;
                Debug.Log("Press R to recharge gun");
            }
        }

        //Recarga de arma con R
        if(Input.GetKeyDown(KeyCode.R) && gunMagazineMax < 15)
        {
            gunMagazineMax = 15;
            Debug.Log("Gun recharged");
        }

        //clamp para mantener valores de estamina enteros
        playerStamina = Mathf.Clamp(playerStamina, 0, 10);

        //Crouching con C o left ctrl
        if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.LeftControl))
        {
            if (!isCrouched)
            {
                isCrouched = true;

                moveSpeed = 5f * 0.75f; // reduce speed

                //Se achica collider 50%, se evita que el collider atraviese el piso
                capsule.height = crouchHeight;
                capsule.center = crouchCenter;

                //Se achica el mesh del player para que visualmente parezca que se agacha
                transform.localScale = crouchScale;
            }
            else
            {
                isCrouched = false;

                moveSpeed = 5f;

                capsule.height = originalHeight;
                capsule.center = originalCenter;

                transform.localScale = originalScale;
            }
        }



    }

    void ShootFromCenter()
    {
        //chequeo de cámara
        if (mainCamera == null)
        {
            Debug.LogWarning("Shoot aborted: mainCamera is null.");
            return;
        }

        //raycast que sale siempre del centro de la pantalla
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        Debug.Log($"Shooting ray. Origin: {ray.origin}, Dir: {ray.direction}, Range: {gunRange}");

        //raycast ignora layer y collider de player
        if (Physics.Raycast(ray, out hit, gunRange, ignoreLayerMask))
        {
            Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.green, 1f);
            Debug.Log($"Ray hit: {hit.collider.name} (tag: {hit.collider.tag}, layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}) at distance {hit.distance}");

            //mostrar marca en target disparado (para debug)
            Debug.DrawLine(hit.point + Vector3.up * 0.01f, hit.point + Vector3.up * 0.01f + Vector3.up * 0.1f, Color.yellow, 1f);

            //chequear si el objeto disparado es o no el enemigo (debug)
            Enemy enemy = hit.collider.GetComponent<Enemy>();
            if (enemy == null)
                enemy = hit.collider.GetComponentInParent<Enemy>();

            if (enemy != null)
            {
                Debug.Log($"Hit enemy component on {enemy.gameObject.name}. Applying {gunDamage} damage.");
                enemy.TakeDamage(gunDamage);
            }
            else
            {
                Debug.Log("Hit object is not an Enemy.");
            }
        }
        else
        {
            Debug.DrawRay(ray.origin, ray.direction * gunRange, Color.red, 1f);
            Debug.Log("Raycast did not hit anything.");
        }
    }

    //se muestra línea de raycast en scene view
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying && Camera.main != null)
        {
           
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * gunRange);
        }
    }
}


