using UnityEngine;

public class FastIKLook : MonoBehaviour
{
    /// <summary>
    /// Look at target
    /// </summary>
    public Transform Target;

    /// <summary>
    /// Initial direction
    /// </summary>
    protected Vector3 StartDirection;

    /// <summary>
    /// Initial Rotation
    /// </summary>
    protected Quaternion StartRotation;

    void Awake()
    {
        if (Target == null)
            return;

        StartDirection = Target.position - transform.position;
        StartRotation = transform.rotation;
    }

    void LateUpdate()
    {
        if (Target == null)
            return;

        transform.LookAt(Target);
        transform.rotation *= Quaternion.FromToRotation(Vector3.right, Vector3.forward);
    }
}
