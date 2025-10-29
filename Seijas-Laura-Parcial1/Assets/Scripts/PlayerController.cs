using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SO_PlayerStats PlayerStatsSO;

    public int playerLife = 100;
    public int playerStamina = 10;

    private float nextFireTime = 0f;
    private Rigidbody rb;
    private Camera mainCamera;
    private int ignoreLayerMask;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        mainCamera = Camera.main;
        if (mainCamera == null)
            Debug.LogWarning("PlayerController: Camera.main is null. Make sure a camera is tagged MainCamera.");

        //se genera máscara para que raycast de disparo ignore a la layer del player, para que no choque contra su propio collider
        int playerLayer = gameObject.layer;
        ignoreLayerMask = ~(1 << playerLayer); 
        Debug.Log($"PlayerController started. Player layer: {LayerMask.LayerToName(playerLayer)} (index {playerLayer}).");
    }

    void FixedUpdate()
    {
        //movimiento del player y la cámara 
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 inputDir = new Vector3(horizontal, 0, vertical).normalized;

        Vector3 camForward = mainCamera.transform.forward;
        Vector3 camRight = mainCamera.transform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = camForward * vertical + camRight * horizontal;
        Vector3 targetPos = rb.position + moveDir * PlayerStatsSO.moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(targetPos);

        if (moveDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            rb.rotation = Quaternion.RotateTowards(rb.rotation, targetRot, PlayerStatsSO.rotationSpeed * Time.fixedDeltaTime);
        }
    }

    void Update()
    {
        //Input de disparo con click derecho del mouse
        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            Debug.Log("Input detected: left mouse button pressed.");
            ShootFromCenter();
            nextFireTime = Time.time + PlayerStatsSO.fireRate;
        }

        //clamp para mantener valores de estamina enteros
        playerStamina = Mathf.Clamp(playerStamina, 0, 10);
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

        Debug.Log($"Shooting ray. Origin: {ray.origin}, Dir: {ray.direction}, Range: {PlayerStatsSO.gunRange}");

        //raycast ignora layer y collider de player
        if (Physics.Raycast(ray, out hit, PlayerStatsSO.gunRange, ignoreLayerMask))
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
                Debug.Log($"Hit enemy component on {enemy.gameObject.name}. Applying {PlayerStatsSO.gunDamage} damage.");
                enemy.TakeDamage(PlayerStatsSO.gunDamage);
            }
            else
            {
                Debug.Log("Hit object is not an Enemy.");
            }
        }
        else
        {
            Debug.DrawRay(ray.origin, ray.direction * PlayerStatsSO.gunRange, Color.red, 1f);
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
            Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * PlayerStatsSO.gunRange);
        }
    }
}


