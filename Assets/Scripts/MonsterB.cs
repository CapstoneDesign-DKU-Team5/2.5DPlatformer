using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class MonsterB : Monster
{
    private float moveTimeout = 4f;
    private float waitTime = 5f;
    private float patrolProbability = 0.9f;

    protected override IEnumerator IDLE()
    {
        float randomValue = Random.value;

        if (randomValue < patrolProbability)
        {
            Vector3 randomPoint = GetRandomPoint();

            navMeshAgent.SetDestination(randomPoint);
            Debug.Log(randomPoint);

            AnimatorStateInfo curAnimStateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (curAnimStateInfo.IsName("Walk") == false)
            {
                animator.Play("Walk", 0, 0);
            }

            float remainingDistance = monsterXOrZ ? randomPoint.x - transform.position.x : randomPoint.z - transform.position.z;
            spriteRenderer.flipX = remainingDistance < 0;
            remainingDistance = Mathf.Abs(remainingDistance);

            float timer = 0f;

            while (remainingDistance > navMeshAgent.stoppingDistance)
            {
                timer += Time.deltaTime;

                if (timer > moveTimeout || state != State.IDLE)
                {
                    //Debug.Log("time out");
                    //Debug.Log(state);
                    break;
                }

                yield return null;

                remainingDistance = monsterXOrZ ? randomPoint.x - transform.position.x : randomPoint.z - transform.position.z;
                remainingDistance = Mathf.Abs(remainingDistance);
            }
        }
        else if (randomValue >= patrolProbability/* && state == State.IDLE*/)
        {
            navMeshAgent.SetDestination(transform.position);
            Debug.Log("Á¦ÀÚ¸®");
            AnimatorStateInfo curAnimStateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (curAnimStateInfo.IsName("IdleNormal") == false)
            {
                animator.Play("IdleNormal", 0, 0);
            }
            yield return StartCoroutine(Wait2(waitTime));
        }
    }

    private Vector3 GetRandomPoint()
    {
        float minDistance = 3f;
        float maxDistance = 5f;

        float sign = Random.value < 0.5f ? -1f : 1f;

        float randomOffset = Random.Range(minDistance, maxDistance) * sign;

        Vector3 point;
        if (monsterXOrZ)
        {
            point = transform.position + Vector3.right * randomOffset;
        }
        else
        {
            point = transform.position + Vector3.forward * randomOffset;
        }
        return point;
    }

    protected IEnumerator Wait2(float t)
    {
        while (t > 0 && state == State.IDLE)
        {
            t -= Time.deltaTime;
            yield return null;
        }
    }
}
