using UnityEngine;

public class BeatMovement : MonoBehaviour
{
    private Vector3 _velocity = new(0f, 0f, 1f);
    private Quaternion target = Quaternion.Euler(0f, 0f, 180f);
    void Start()
    {
        
    }

    void Update()
    {
        // Initial rotation
        if (Quaternion.Angle(transform.rotation, target) > 0.1f)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, 200f * Time.deltaTime);
        }

        // Forward movement
        transform.position += Constants.BEAT_SPEED * Time.deltaTime * _velocity;
    }
}
