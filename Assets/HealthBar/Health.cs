using UnityEngine;

public class Health : MonoBehaviour
{

	public int maxHealth = 10;
	public int currentHealth;

	public HealthBar healthBar;

    void Start()
    {
		currentHealth = maxHealth;
		healthBar.SetMaxHealth(maxHealth);
    }

    void Update()
    {
		// Take damage logic here
    }

	void TakeDamage(int damage)
	{
		currentHealth -= damage;

		healthBar.SetHealth(currentHealth);
	}
}
