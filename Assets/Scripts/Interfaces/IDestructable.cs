using UnityEngine;
public interface IDestructable
{
    int Health { get; set; }

    bool TryDestroy();

    void Damage(Collision collision = null);
    
    void Damage(Vector3 hitPoint);
}
