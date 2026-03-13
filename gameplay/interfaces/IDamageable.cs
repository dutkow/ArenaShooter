using System;

public interface IDamageable
{
    public void ApplyDamage(int amount);



    public bool IsAlive();

}

public interface ILifeEntity
{
    public int GetHealth();
    public void SetHealth(int health);
    public int GetMaxHealth();
    public void SetMaxHealth(int maxHealth);

    public int GetArmor();
    public void SetArmor(int armor);


    public int GetMaxArmor();
    public void SetMaxArmor(int maxArmor);

}