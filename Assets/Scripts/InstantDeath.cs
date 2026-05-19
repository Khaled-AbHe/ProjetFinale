using UnityEngine;

public class InstantDeath : MonoBehaviour
{
    public void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            HealthSystem health = collision.gameObject.GetComponent<HealthSystem>();
            health.TakeDamage(999999);
        } else if (collision.gameObject.CompareTag("Enemy"))
        {
            EnemyEntity enemyEntity = collision.gameObject.GetComponent<EnemyEntity>();
            enemyEntity.TakeDamage(999999);
        }
    }
}
