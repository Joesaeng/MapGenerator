using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnArea : MonoBehaviour
{
    public bool Check { get; set; }

    CellularAutomataMap map;
    private void Start()
    {
        map = GameObject.FindWithTag("Cellular").GetComponent<CellularAutomataMap>();
    }

    private void Update()
    {
        if(Check)
        {
            if (transform.position.x < 0 || transform.position.x >= map.width ||
                transform.position.y < 0 || transform.position.y >= map.height)
                map.RemoveArea(this.gameObject);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if(Check)
        {
            if(collision.gameObject.CompareTag("Wall"))
            {
                map.RemoveArea(this.gameObject);
            }
        }
    }
}
