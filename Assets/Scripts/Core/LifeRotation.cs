using UnityEngine;

public class LifeRotation : MonoBehaviour
{
    [SerializeField]private float rotationSpeed;

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(Vector3.forward*rotationSpeed*Time.deltaTime);
    }
}
