using UnityEngine;

[CreateAssetMenu(fileName = "SO_PlayerStats", menuName = "Scriptable Objects/SO_PlayerStats")]
public class SO_PlayerStats : ScriptableObject
{
    [Header("Movement")]
    public float moveSpeed;
    public float rotationSpeed;

    [Header("Gun Settings")]
    public float gunRange;
    public int gunDamage;
    public float fireRate;
}
