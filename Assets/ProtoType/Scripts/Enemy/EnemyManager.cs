using System.Collections.Generic;
using UnityEngine;


public static class EnemyManager
{
    private static List<Enemy> enemies = new List<Enemy>();

    public static void Register(Enemy enemy)
    {
        if (!enemies.Contains(enemy))
            enemies.Add(enemy);
    }

    public static void Unregister(Enemy enemy)
    {
        if (enemies.Contains(enemy))
            enemies.Remove(enemy);
    }

    public static List<Enemy> GetAllEnemies()
    {
        return enemies;
    }
}
