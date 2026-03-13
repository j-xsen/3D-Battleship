using JetBrains.Annotations;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEditor.Search;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;

public class logic : MonoBehaviour
{
    //make private for security
    private struct ship
    {
        private int health_points;
        private int direction;
        private int[] coords;
    }
    public struct fleet
    {
        private ship[] places;
        public float health;

    }/*
    private ship Placement()
    {
        
    }
    public fleet Ship_Input()
    {
        Placement();
    }*/
    public fleet player;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        fleet player = new fleet();
        player.health = 20;
        
    }

    // Update is called once per frame
    void Update()
    {
    }
}
