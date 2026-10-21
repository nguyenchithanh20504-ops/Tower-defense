
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Counter : MonoBehaviour
{
    public static List<GameObject> enemies = new List<GameObject>();
    public static List<GameObject> towers = new List<GameObject>();
    public static void clearCounter()
    {
        towers.Clear();
        enemies.Clear();
    }
}
