using UnityEngine;

public class MonsterChaseRange : MonoBehaviour
{
    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            Monster monster = GetComponentInParent<Monster>();
            if (monster != null && monster.target == null)
            {
                monster.TriggerSetTarget(other.transform);
            }
        }
    }
}

