using UnityEngine;

public class MonsterChaseRange : MonoBehaviour
{
 

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            Monster monster = GetComponentInParent<Monster>();
            if (monster != null && monster.target == null)
            {
                monster.SetTarget(other.transform);
            }
        }
    }
}

