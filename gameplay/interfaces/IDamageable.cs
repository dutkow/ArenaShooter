using System;

public interface IDamageable
{
    void ApplyDamage(int amount);

    public bool IsAlive();

}