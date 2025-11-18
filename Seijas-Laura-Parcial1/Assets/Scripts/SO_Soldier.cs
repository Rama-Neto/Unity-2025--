using UnityEngine;

[CreateAssetMenu(fileName = "SO_Soldier", menuName = "Scriptable Objects/SO_Soldier")]
public class SO_Soldier : ScriptableObject
{
    [Header("Speed")]
    public float chaseSpeed = 5f;
    
    [Header("Vision Settings")]
    public float visionRange = 10f;
    public float visionAngle = 60f;

}
