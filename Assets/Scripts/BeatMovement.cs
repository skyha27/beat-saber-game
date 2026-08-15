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
        // Forward movement
        transform.position += Constants.BEAT_SPEED * Time.deltaTime * _velocity;

        if (transform.position.z <= -5f)
        {
            _velocity = new(0f, 1f, 0f);
        }

        if (transform.position.y < -10f)
        {
            Destroy(gameObject);
        }
    }
}
