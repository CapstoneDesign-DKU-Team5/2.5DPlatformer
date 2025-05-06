using UnityEngine;

public class MonsterA : Monster
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
    }

    public override  void OnDamaged(Vector3 attackerPos) {
        Debug.Log("attacked A");
        base.OnDamaged(attackerPos);

    }
}
