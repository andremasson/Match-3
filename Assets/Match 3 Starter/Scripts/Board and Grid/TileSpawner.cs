using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileSpawner : MonoBehaviour
{
    public GameObject tile;
    private Vector2 offset;
    float lastUpdate = 0.0f;
    float updateSpeed = .1f;

    private void Start()
    {
        offset = tile.GetComponent<SpriteRenderer>().bounds.size;
    }

    private void FixedUpdate()
    {
        if (lastUpdate > updateSpeed)
        {
            lastUpdate = 0.0f;
            //CheckEmptyCell();
        }
        else
        {
            lastUpdate += Time.deltaTime;
        }
    }

    private GameObject GetAdjacent(Vector2 castDir)
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, castDir);
        if (hit.collider != null)
        {
            return hit.collider.gameObject;
        }
        return null;
    }

    private float GetAdjacentDistance(Vector2 castDir)
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, castDir);
        if (hit.collider != null)
        {
            return hit.distance;
        }
        return float.NaN;
    }

    public void CheckEmptyCell()
    {
        float dist = GetAdjacentDistance(Vector2.down);
        if (dist >= offset.y)
        {
            SpawnTile();
        }
    }

    public GameObject SpawnAndReturnTile()
    {
        GameObject newTile = Instantiate(tile, transform.localPosition, tile.transform.rotation, BoardManager.instance.transform);
        newTile.GetComponent<SpriteRenderer>().sprite = BoardManager.instance.GetNewSprite();
        return newTile;
    }

    public void SpawnTile()
    {
        GameObject newTile = SpawnAndReturnTile();
        //newTile.GetComponent<Tile>().Plunge();
    }
}
