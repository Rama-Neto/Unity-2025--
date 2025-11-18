using UnityEngine;
using TMPro;

public class Enemy : MonoBehaviour
{
    //Referencia a SO
    [Header("References")]
    [SerializeField] private SO_Soldier SO_Soldier;

    public int enemyLife = 70;

    //Referencias a TMPro y cámara transform para texto de estados
    [SerializeField] private TextMeshPro stateText;
    [SerializeField] private Transform cameraTransform;

    //Colores de los estados, ajustables en Inspector
    [SerializeField] private Color normalColor = Color.green;
    [SerializeField] private Color chaseColor = new Color(1f, 0.5f, 0f);  // orange
    [SerializeField] private Color damagedColor = Color.red;
    [SerializeField] private Color deadColor = Color.gray;

    private Transform player; //referencia al transform del player, para persecución
    private Vector3 spawnPoint; //punto de respawn específico del enemigo
    private string state = "Normal"; //Estado de arranque del enemigo
    private bool isDead = false; //booleano para chequear si el enemigo está vivo o muerto

    private float staminaTimer = 0f; //timer para drenaje y regeneración de estamina del player, según si está dentro o fuera de cono de visión

    private float damagedCooldown = 0f;


    private void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
        spawnPoint = transform.position;
        SetState("Normal");
        stateText.color = normalColor;
    }

    //Input F3 para respawn de enemigo si el booleano isDead es true
    private void Update()
    {
        if (damagedCooldown > 0f)
        {
            damagedCooldown -= Time.deltaTime;
            return; // DO NOT overwrite state during damage flash
        }

        //Texto de estado siempre mira a cámara
        stateText.transform.LookAt(
        stateText.transform.position + cameraTransform.forward
        );


        if (isDead)
        {
            if (Input.GetKeyDown(KeyCode.F3))
            {
                Respawn();
                Debug.Log("Enemy respawning");
            }
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);
        Vector3 dirToPlayer = (player.position - transform.position).normalized;

        //Se persigue al jugador cuando está dentro del rango y ángulo de visión (cono), de lo contrario el estado sigue siendo Normal y el player puede regenerar estamina
        float angleToPlayer = Vector3.Angle(transform.forward, dirToPlayer);

        if (distance <= SO_Soldier.visionRange && angleToPlayer <= SO_Soldier.visionAngle / 2f)
        {
            if (HasLineOfSight())
            {
                ChasePlayer();
            }
            else
            {
                SetState("Normal");
                RegenerateStamina();
            }
            
        }
        else
        {
            SetState("Normal");
            RegenerateStamina();
        }

    }

    //Chequea si hay estructuras bloqueando la visión al player
    private bool HasLineOfSight()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        Debug.DrawRay(transform.position, direction * SO_Soldier.visionRange, Color.red);

        // Raycast toward the player
        RaycastHit hit;

        if (Physics.Raycast(transform.position, direction, out hit, SO_Soldier.visionRange))
        {
            // If the hit object is the player, LOS is clear
            if (hit.collider.CompareTag("Player"))
                return true;
        }

        return false; // Something else is blocking the view
    }



    //Lógica de persecusión a player, contiene drenaje de estamina del player (1 por segundo mientras esté en cono de visión)
    void ChasePlayer()
    {
        SetState("Chase");
        Vector3 dir = (player.position - transform.position).normalized;
        transform.position += dir * SO_Soldier.chaseSpeed * Time.deltaTime;

        // Drain stamina at 1 point per second
        staminaTimer += Time.deltaTime;
        if (staminaTimer >= 1f)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null && pc.playerStamina > 0)
            {
                pc.playerStamina = Mathf.Max(0, pc.playerStamina - 1);
            }
            staminaTimer = 0f;
            Debug.Log("Player Stamina: " + pc.playerStamina);
        }
    }

    void RegenerateStamina()
    {
        //Player regenera estamina, 1 por segundo
        staminaTimer += Time.deltaTime;
        if (staminaTimer >= 1f)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null && pc.playerStamina < 10)
            {
                pc.playerStamina = Mathf.Min(10, pc.playerStamina + 1);
            }
            staminaTimer = 0f;
            Debug.Log("Player Stamina: " + pc.playerStamina);
        }
    }

    //Lógica de daño que recibe el enemigo
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        enemyLife -= damage;
        SetState("Damaged");

        SetState("Damaged");
        damagedCooldown = 0.3f; // Freeze state for 0.3 seconds

        if (enemyLife <= 0)
        {
            Die();
        }
    }

    //Lógica de muerte del enemigo, se apagan sus componentes de renderización y colisión
    public void Die()
    {
        SetState("Dead");
        isDead = true;

        // Disable visuals and collisions
        GetComponent<Renderer>().enabled = false;
        GetComponent<Collider>().enabled = false;
    }

    //Lógica de respawn del enemigo, se vuelven a mostrar sus componentes de renderización y colisión
    void Respawn()
    {
        transform.position = spawnPoint;
        enemyLife = 70;
        isDead = false;

        // Re-enable visuals and collisions
        GetComponent<Renderer>().enabled = true;
        GetComponent<Collider>().enabled = true;

        SetState("Normal");
    }

    //Muestra el estado del enemigo en Consola
    void SetState(string newState)
    {
        if (state != newState)
        {
            state = newState;
            Debug.Log("Enemy State: " + state);

            if (stateText != null)
                stateText.text = newState;

           //Cambio de color de texto según estado
            switch (newState)
            {
                case "Normal":
                    stateText.color = normalColor; // green
                    break;

                case "Chase":
                    stateText.color = chaseColor; // orange
                    break;

                case "Damaged":
                    stateText.color = damagedColor; // red
                    break;

                case "Dead":
                    stateText.color = deadColor; // dark grey
                    break;
            }
        }
    }

    //Gizmos dibujados en scene view para poder ver el cono de visión
    private void OnDrawGizmos()
    {
        // Draw cone even in edit mode
        Gizmos.color = Color.yellow;

        // Draw vision range sphere
        Gizmos.DrawWireSphere(transform.position, SO_Soldier.visionRange);

        // Cone boundaries
        Quaternion leftRot = Quaternion.Euler(0, -SO_Soldier.visionAngle / 2f, 0);
        Vector3 leftDir = leftRot * transform.forward * SO_Soldier.visionRange;
        Gizmos.DrawLine(transform.position, transform.position + leftDir);

        Quaternion rightRot = Quaternion.Euler(0, SO_Soldier.visionAngle / 2f, 0);
        Vector3 rightDir = rightRot * transform.forward * SO_Soldier.visionRange;
        Gizmos.DrawLine(transform.position, transform.position + rightDir);

        // Forward direction
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * SO_Soldier.visionRange);
    }


}





