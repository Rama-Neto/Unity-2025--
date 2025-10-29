using UnityEngine;

public class Enemy : MonoBehaviour
{
   

    //Variables de vida, persecución y cono de visión, expuestas en Inspector
    public int enemyLife = 50;
    public float chaseSpeed = 5f;
    [Header("Vision Settings")]
    public float visionRange = 10f;
    public float visionAngle = 60f;

    private Transform player; //referencia al transform del player, para persecución
    private Vector3 spawnPoint; //punto de respawn específico del enemigo
    private string state = "Normal"; //Estado de arranque del enemigo
    private bool isDead = false; //booleano para chequear si el enemigo está vivo o muerto

    private float staminaTimer = 0f; //timer para drenaje y regeneración de estamina del player, según si está dentro o fuera de cono de visión

    private void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
        spawnPoint = transform.position;
        SetState("Normal");
    }

    //Input F3 para respawn de enemigo si el booleano isDead es true
    private void Update()
    {
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

        if (distance <= visionRange && angleToPlayer <= visionAngle / 2f)
        {
            ChasePlayer();
        }
        else
        {
            SetState("Normal");
            RegenerateStamina();
        }

    }

    //Lógica de persecusión a player, contiene drenaje de estamina del player (1 por segundo mientras esté en cono de visión)
    void ChasePlayer()
    {
        SetState("Chase");
        Vector3 dir = (player.position - transform.position).normalized;
        transform.position += dir * chaseSpeed * Time.deltaTime;

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
        enemyLife = 50;
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
        }
    }

    //Gizmos dibujados en scene view para poder ver el cono de visión
    private void OnDrawGizmos()
    {
        // Draw cone even in edit mode
        Gizmos.color = Color.yellow;

        // Draw vision range sphere
        Gizmos.DrawWireSphere(transform.position, visionRange);

        // Cone boundaries
        Quaternion leftRot = Quaternion.Euler(0, -visionAngle / 2f, 0);
        Vector3 leftDir = leftRot * transform.forward * visionRange;
        Gizmos.DrawLine(transform.position, transform.position + leftDir);

        Quaternion rightRot = Quaternion.Euler(0, visionAngle / 2f, 0);
        Vector3 rightDir = rightRot * transform.forward * visionRange;
        Gizmos.DrawLine(transform.position, transform.position + rightDir);

        // Forward direction
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * visionRange);
    }


}





